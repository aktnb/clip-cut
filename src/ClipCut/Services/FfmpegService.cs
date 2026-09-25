using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public class FfmpegService(string ffmpegPath) : IFfmpegService
{
    public async Task CutAsync(CutJob job, CancellationToken cancellationToken = default)
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
            startInfo.ArgumentList.Add(job.Start.Value.ToString(@"hh¥:mm¥:ss¥.fff"));
        }

        if (job.End is not null)
        {
            startInfo.ArgumentList.Add("-to");
            startInfo.ArgumentList.Add(job.End.Value.ToString(@"hh¥:mm¥:ss¥.fff"));
        }

        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(job.InputFilePath);

        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("copy");

        startInfo.ArgumentList.Add(job.OutputFilePath);

        using var process = new Process();
        process.StartInfo = startInfo;

        process.Start();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);

            throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}: {error}");
        }
    }
}