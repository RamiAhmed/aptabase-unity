using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Data;

namespace AptabaseSDK.Services
{
    public interface IDispatcher
    {
        void Enqueue(AptabaseEvent data);

        Task Flush(CancellationToken cancellationToken);
    }
}