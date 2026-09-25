using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using ClipCut.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ClipCut.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] public partial string? InputFilePath { get; set; } = string.Empty;
    [ObservableProperty] public partial string? OutputFilePath { get; set; } = string.Empty;
    [ObservableProperty] public partial string? StartTime { get; set; }
    [ObservableProperty] public partial string? EndTime { get; set; }
    [ObservableProperty] public partial string? ValidationError { get; set; }
    public ObservableCollection<CutJob> Jobs { get; } = [];

    [RelayCommand]
    private void AddJob()
    {
        ValidationError = null;

        try
        {
            var start = ParseTime(StartTime);
            var end = ParseTime(EndTime);

            Jobs.Add(new CutJob(InputFilePath!, OutputFilePath!, start, end));
        }
        catch (ArgumentException ex)
        {
            ValidationError = ex.Message;
        }
        catch (FileNotFoundException ex)
        {
            ValidationError = ex.Message;
        }
        catch (DirectoryNotFoundException ex)
        {
            ValidationError = ex.Message;
        }
    }

    [RelayCommand]
    private void RemoveJob(CutJob job)
    {
        Jobs.Remove(job);
    }

    private async Task ExecuteJobAsync(CutJob job)
    {
        job.Status = JobStatus.Processing;

        try
        {
            job.Status = JobStatus.Completed;
        }
        catch
        {
            job.Status = JobStatus.Failed;
        }
    }

    private static TimeSpan? ParseTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (TimeSpan.TryParseExact(value, @"hh¥:mm¥:ss¥.fff", CultureInfo.InvariantCulture, out var result))
            return result;

        throw new ArgumentException($"Invalid time format: {value}");
    }
}