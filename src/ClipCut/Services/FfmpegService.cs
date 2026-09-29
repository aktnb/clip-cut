using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public class FfmpegService(string ffmpegPath) : IFfmpegService
{
    public async Task CutAsync(CutJob job, CancellationToken cancellationToken = default)
    {
        var startInfo = BuildStartInfo(job);

        Console.WriteLine(BuildCommand(startInfo));

        cancellationToken.ThrowIfCancellationRequested();

        using var process = new Process
        {
            StartInfo = startInfo
        };

        if (!process.Start())
            throw new InvalidOperationException("Failed to start FFmpeg.");

        var standardOutputTask = process.StandardOutput.ReadToEndAsync();
        var standardErrorTask = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // The process exited between cancellation and Kill.
            }

            await process.WaitForExitAsync();
            await Task.WhenAll(standardOutputTask, standardErrorTask);
            throw;
        }

        await Task.WhenAll(standardOutputTask, standardErrorTask);

        if (process.ExitCode != 0)
        {
            var errorOutput = standardErrorTask.Result.Trim();
            var message = errorOutput.Length == 0
                ? $"FFmpeg exited with code {process.ExitCode}."
                : $"FFmpeg exited with code {process.ExitCode}: {errorOutput}";

            throw new InvalidOperationException(message);
        }
    }

    public ProcessStartInfo BuildStartInfo(CutJob job)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ffmpegPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        if (job.Start is not null)
        {
            startInfo.ArgumentList.Add("-ss");
            startInfo.ArgumentList.Add(job.Start.Value.ToString(@"hh\:mm\:ss\.fff"));
        }

        if (job.End is not null)
        {
            startInfo.ArgumentList.Add("-to");
            startInfo.ArgumentList.Add(job.End.Value.ToString(@"hh\:mm\:ss\.fff"));
        }

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(job.InputFilePath);

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("copy");

        startInfo.ArgumentList.Add(job.OutputFilePath);

        return startInfo;
    }

    private string BuildCommand(ProcessStartInfo startInfo)
    {
        return string.Join(
            " ",
            new[] { startInfo.FileName }
                .Concat(startInfo.ArgumentList.Select(Escape)));
    }

    private static string Escape(string arg)
    {
        return arg.Contains(' ')
            ? $"\"{arg}\""
            : arg;
    }
}
