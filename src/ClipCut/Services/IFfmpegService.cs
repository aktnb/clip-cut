using System.Threading;
using System.Threading.Tasks;
using ClipCut.Models;

namespace ClipCut.Services;

public interface IFfmpegService
{
    Task CutAsync(
        CutJob cutJob,
        CancellationToken cancellationToken = default);
}