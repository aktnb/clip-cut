using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ClipCut.ViewModels;

namespace ClipCut.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BrowseButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "動画を選択",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("動画ファイル")
                    {
                        Patterns = ["*.mp4", "*.mkv", "*.mov", "*.webm"]
                    }
                ]
            });

        if (files.Count == 0)
            return;

        if (DataContext is MainWindowViewModel vm) vm.InputFilePath = files[0].Path.LocalPath;
    }
}
