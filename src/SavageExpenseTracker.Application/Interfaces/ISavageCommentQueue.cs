using System.Threading;
using System.Threading.Tasks;

namespace SavageExpenseTracker.Application.Interfaces
{
    public interface ISavageCommentQueue
    {
        void Enqueue(long expenseId);
        ValueTask<long> DequeueAsync(CancellationToken cancellationToken);
    }
}
