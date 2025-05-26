using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using JLAClient.ViewModels;
using JLA_Client.Views;
using JLAClient.Services;
using JLAClient.Models;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace JLA_Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
    //reference to main view model.
    private readonly MainWindowViewModel _mainViewModel = new();

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
            desktop.ShutdownRequested += DesktopOnShutdownRequested;
        }
        //Acquire Configuration from file
        JLAClientConfiguration configuration = await ConfigurationFileService.RetrieveStartupConfiguration();
        //Instantiate my services for my objects
        ListObjectsFileService<DisplayableJobListing> listingsFileService = new();
        listingsFileService.SetFilePath(configuration.ListingsSourcePath);
        ListObjectsFileService<ScheduleRule> rulesFileService = new();
        rulesFileService.SetFilePath(configuration.RulesSourcePath);
        // get the listings to load
        var listingsLoaded = await listingsFileService.LoadFromFileAsync();

        if (listingsLoaded is not null)
        {
            _mainViewModel.AppendListings(listingsLoaded);
        }
        //Get effective last save time (before it's overwritten) for passing to RabbitMQ service initialization
        DateTime? lastTimeListingsSaved = listingsFileService.GetLastTimeListingsSavedToFile();
        //get existing automatic update rules
        var scheduledRules = await rulesFileService.LoadFromFileAsync() ?? null;
        if(scheduledRules is not null)
        {
            _mainViewModel.AppendRules(scheduledRules);
        }
        //Initial periodic item save in case of computer crash
        _ = Task.Run(() => RecurringSaveToFile(TimeSpan.FromMilliseconds(configuration.AutosaveFrequencyInMilliseconds)));
        //Initialize the RabbitMQ system and pass it the rules so it knows what to automatically send out
        await MyRabbitMQ.Initialize(_mainViewModel.AppendListings, scheduledRules, lastTimeListingsSaved, configuration);
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
        //A call to the listings and rules services goes here, as well as a pull from the mainviewmodel
    }

    private async Task RecurringSaveToFile(TimeSpan interval, CancellationToken cancellationToken = default)
    {
        using PeriodicTimer timer = new(interval);
        while (true)
        {
            await SaveToFile();
            await timer.WaitForNextTickAsync(cancellationToken);
        }
    }
}