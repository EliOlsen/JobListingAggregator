using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using JLA_Client.ViewModels;
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
        //Initial periodic item save in case of computer crash
        _ = Task.Run(() => RecurringSaveToFile(TimeSpan.FromMilliseconds(configuration.AutosaveFrequencyInMilliseconds)));
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
        //A call to the listings and rules services goes here
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