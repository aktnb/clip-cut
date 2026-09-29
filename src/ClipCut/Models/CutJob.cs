using System;
using System.IO;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipCut.Models;

public partial class CutJob : ObservableObject
{
    public CancellationTokenSource CancellationTokenSource { get; } = new();

    public JobStatus Status
    {
        get => field == JobStatus.Processing && CancellationTokenSource.IsCancellationRequested
            ? JobStatus.Cancelling
            : field;
        set
        {
            if (!SetProperty(ref field, value))
                return;

            OnPropertyChanged(nameof(RemoveButtonText));
            OnPropertyChanged(nameof(CanRemove));
        }
    } = JobStatus.Pending;

    public string RemoveButtonText => Status switch
    {
        JobStatus.Processing => "Cancel",
        JobStatus.Cancelling => "Cancelling…",
        _ => "Remove"
    };

    public bool CanRemove => Status != JobStatus.Cancelling;

    public string InputFilePath { get; init; }
    public string OutputFilePath { get; init; }
    public TimeSpan? Start { get; init; }
    public TimeSpan? End { get; init; }

    [ObservableProperty] public partial string? ErrorMessage { get; set; }

    public CutJob(
        string inputFilePath,
        string outputFilePath,
        TimeSpan? start,
        TimeSpan? end
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputFilePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputFilePath);

        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("Input file does not exist.", inputFilePath);

        var outputDirectory = Path.GetDirectoryName(outputFilePath);

        if (string.IsNullOrWhiteSpace(outputDirectory) ||
            !Directory.Exists(outputDirectory))
            throw new DirectoryNotFoundException($"Output directory does not exist: {outputDirectory}");

        if (Path.GetFullPath(inputFilePath) == Path.GetFullPath(outputFilePath))
            throw new ArgumentException("Input and output file paths must be different.", nameof(outputFilePath));

        if (start is not null && start < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(start));

        if (start is not null && end is not null && end <= start)
            throw new ArgumentException("End time must be greater than start time.", nameof(end));

        InputFilePath = inputFilePath;
        OutputFilePath = outputFilePath;
        Start = start;
        End = end;
    }

    public void Cancel()
    {
        CancellationTokenSource.Cancel();
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(RemoveButtonText));
        OnPropertyChanged(nameof(CanRemove));
    }
}
