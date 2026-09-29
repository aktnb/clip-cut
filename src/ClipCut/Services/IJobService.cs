using System.Threading;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public interface IJobService
{
    ValueTask EnqueueAsync(CutJob job, CancellationToken cancellationToken = default);
}