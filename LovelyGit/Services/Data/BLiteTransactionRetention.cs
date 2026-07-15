using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using BLite.Core.Storage;
using BLite.Core.Transactions;

namespace ExpressThat.LovelyGit.Services.Data;

// BLite 4.4.2 registers every transaction but its Transaction.CommitAsync and
// RollbackAsync paths bypass the overload that unregisters it. Keep this narrow
// compatibility boundary until the dependency releases an equivalent fix.
internal static class BLiteTransactionRetention
{
    public static IDisposable Track(ITransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        return transaction is Transaction concrete
            ? new Registration(concrete)
            : NullRegistration.Instance;
    }

    private sealed class Registration(Transaction transaction) : IDisposable
    {
        private Transaction? _transaction = transaction;

        public void Dispose()
        {
            var current = Interlocked.Exchange(ref _transaction, null);
            if (current is null)
            {
                return;
            }

            var storage = GetStorage(current);
            try
            {
                current.Dispose();
            }
            finally
            {
                GetActiveTransactions(storage).TryRemove(current.TransactionId, out _);
            }
        }
    }

    private sealed class NullRegistration : IDisposable
    {
        public static readonly NullRegistration Instance = new();

        public void Dispose()
        {
        }
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_storage")]
    private static extern ref StorageEngine GetStorage(Transaction transaction);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_activeTransactions")]
    private static extern ref ConcurrentDictionary<ulong, Transaction> GetActiveTransactions(
        StorageEngine storage);
}
