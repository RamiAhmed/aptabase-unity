using System.Threading;
using System.Threading.Tasks;

namespace AptabaseSDK
{
    public interface IDispatcher
    {
        void Enqueue(Event data);

        Task Flush(CancellationToken cancellationToken);
    }
}