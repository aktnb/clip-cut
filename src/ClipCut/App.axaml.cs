using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ClipCut.Services;
using ClipCut.ViewModels;
using ClipCut.Views;
using Microsoft.Extensions.DependencyInjection;

namespace ClipCut;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

#if DEBUG
        services.AddSingleton<IFfmpegService, DisabledFfmpegService>();
#else
        services.AddSingleton<IFfmpegService>(_ => new FfmpegService("ffmpeg"));
#endif
        services.AddSingleton<IJobService, JobService>();
        services.AddTransient<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = Services.GetRequiredService<MainWindowViewModel>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
