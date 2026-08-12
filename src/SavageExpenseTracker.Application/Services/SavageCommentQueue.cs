using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using SavageExpenseTracker.Application.Interfaces;

namespace SavageExpenseTracker.Application.Services
{
    public class SavageCommentQueue : ISavageCommentQueue
    {
        private readonly Channel<long> _queue;

        public SavageCommentQueue()
        {
            var options = new BoundedChannelOptions(10000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true
            };
            _queue = Channel.CreateBounded<long>(options);
        }

        public void Enqueue(long expenseId)
        {
            if (!_queue.Writer.TryWrite(expenseId))
            {
                throw new InvalidOperationException($"Queue is full, dropping expense {expenseId}");
            }
        }

        public ValueTask<long> DequeueAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
