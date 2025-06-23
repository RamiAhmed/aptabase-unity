using System.Threading;
using System.Threading.Tasks;

namespace AptabaseSDK.Services
{
    public interface IDispatcher
    {
        void Enqueue(Data.Event data);

        Task Flush(CancellationToken cancellationToken);
    }
}