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
        {
            if (job.CancellationTokenSource.IsCancellationRequested)
            {
                job.Status = JobStatus.Cancelled;
                continue;
            }

            await ProcessJobAsync(job, cancellationToken);
        }
    }

    private async Task ProcessJobAsync(
        CutJob job,
        CancellationToken cancellationToken)
    {
        using var jobCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            job.CancellationTokenSource.Token);

        job.Status = JobStatus.Processing;
        job.ErrorMessage = null;

        try
        {
            await _ffmpegService.CutAsync(job, jobCancellationTokenSource.Token);

            job.Status = jobCancellationTokenSource.IsCancellationRequested
                ? JobStatus.Cancelled
                : JobStatus.Completed;
        }
        catch (OperationCanceledException)
            when (jobCancellationTokenSource.IsCancellationRequested)
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

    public void RemoveJob(CutJob job)
    {
        job.Cancel();
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
