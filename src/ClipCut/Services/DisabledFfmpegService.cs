using System;
using System.Threading;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public sealed class DisabledFfmpegService : IFfmpegService
{
    public async Task CutAsync(CutJob cutJob, CancellationToken cancellationToken = default)
    {
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        throw new InvalidOperationException("FFmpeg process execution is disabled in Debug builds.");
    }
}
