using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClipCut.Models;

public partial class CutJob : ObservableObject
{
    public string InputFilePath { get; init; }
    public string OutputFilePath { get; init; }
    public TimeSpan? Start { get; init; }
    public TimeSpan? End { get; init; }

    [ObservableProperty] public partial JobStatus Status { get; set; } = JobStatus.Pending;
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
}