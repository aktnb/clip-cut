using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public class JobService : IJobService, IAsyncDisposable
{
    private readonly Channel<CutJob> _channel;
    private readonly IFfmpegService _ffmpegService;

    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _workerTask;

    public JobService(IFfmpegService ffmpegService)
    {
        _ffmpegService = ffmpegService;

        _channel = Channel.CreateUnbounded<CutJob>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _workerTask = ProcessJobsAsync(_cancellationTokenSource.Token);
    }

    private async Task ProcessJobsAsync(CancellationToken cancellationToken)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(cancellationToken))
            await ProcessJobAsync(job, cancellationToken);
    }

    private async Task ProcessJobAsync(
        CutJob job,
        CancellationToken cancellationToken)
    {
        try
        {
            job.Status = JobStatus.Processing;
            job.ErrorMessage = null;

            await _ffmpegService.CutAsync(job, cancellationToken);

            job.Status = JobStatus.Completed;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            job.Status = JobStatus.Cancelled;
        }
        catch (Exception ex)
        {
            job.Status = JobStatus.Failed;
            job.ErrorMessage = ex.Message;
        }
    }

    public ValueTask EnqueueAsync(CutJob job, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();

        await _cancellationTokenSource.CancelAsync();

        try
        {
            await _workerTask;
        }
        catch (OperationCanceledException)
        {
        }

        _cancellationTokenSource.Dispose();
    }
}