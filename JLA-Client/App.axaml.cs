using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Data.Core.Plugins;
using JLAClient.ViewModels;
using JLAClient.Views;
using JLAClient.Services;
using JLAClient.Models;
using JLAClient.Interfaces;
namespace JLAClient;
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    //reference to main view model and our two ListObjectsFileServices
    private readonly MainWindowViewModel _mainViewModel = new();
    private readonly ListObjectsFileService<DisplayableJobListing> _listingsFileService = new();
    private readonly ListObjectsFileService<ScheduleRule> _rulesFileService = new();
    private IJobRequestService? _jobRequestService;
    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainViewModel,
            };
            desktop.ShutdownRequested += DesktopOnShutdownRequested; //I've got some things to do before I allow the program to close, so register here
        }
        //Acquire Configuration from file
        JLAClientConfiguration configuration = await ConfigurationFileService.RetrieveStartupConfiguration();
        //Instantiate my filepaths for my services
        _listingsFileService.SetFilePath(configuration.ListingsSourcePath);
        _rulesFileService.SetFilePath(configuration.RulesSourcePath);
        // get the job listings to load
        var listingsLoaded = await _listingsFileService.LoadFromFileAsync();
        if (listingsLoaded is not null) _mainViewModel.AppendListings(listingsLoaded, false);
        //Get effective last save time (before it's overwritten) for passing to RabbitMQ service initialization
        DateTime? lastTimeListingsSaved = _listingsFileService.GetLastTimeListingsSavedToFile();
        //get existing automatic update rules
        var scheduledRules = await _rulesFileService.LoadFromFileAsync() ?? null;
        if (scheduledRules is not null) _mainViewModel.AppendRules(scheduledRules, false);
        //Initial periodic item save in case of computer crash
        _ = Task.Run(() => RecurringSaveToFile(TimeSpan.FromMilliseconds(configuration.AutosaveFrequencyInMilliseconds)));
        //Initialize the JobRequestService based on config value
        if (configuration.UseRabbitMQ) _jobRequestService = new MyRabbitMQ();
        else _jobRequestService = new HTTPRequestService();
        //Initialize the job request system and pass it the rules so it knows what to automatically send out
        await _jobRequestService.Initialize(_mainViewModel.AppendListings, scheduledRules, lastTimeListingsSaved, configuration);
        //Done with initialization
        base.OnFrameworkInitializationCompleted();
    }
    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
   private bool _canClose; // This flag is used to check if window is allowed to close
    private async void DesktopOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        e.Cancel = !_canClose; // cancel closing event first time
        if (!_canClose)
        {
            //Save job listings and rules to file
            await SaveToFile();
            // Set _canClose to true and Close this Window again
            _canClose = true;
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.Shutdown();
            }
        }
    }
    private async Task SaveToFile()
    {
        //Save job listings and rules, which consists of retrieving the current sets and passing them to their associated services.
        var listingsToSave = _mainViewModel.Listings.Select(listing => listing.GetDisplayableJobListing());
        var rulesToSave = _mainViewModel.Rules.Select(rule => rule.GetScheduleRule());
        await _listingsFileService.SaveToFileAsync(listingsToSave);
        await _rulesFileService.SaveToFileAsync(rulesToSave);
    }
    private async Task RecurringSaveToFile(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        //A wrapper for saving to file that uses a timer to trigger it every so often
        using PeriodicTimer timer = new(interval);
        while (true)
        {
            await SaveToFile();
            await timer.WaitForNextTickAsync(cancellationToken);
        }
    }
}