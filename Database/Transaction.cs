using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class Transaction
    {
        private readonly Dictionary<string, Record> _pendingStore;
        private readonly HashSet<string> _pendingDelete;

        public Transaction()
        {
            _pendingStore = [];
            _pendingDelete = [];
        }

        public void Add(string key, Record record)
        {
            if (_pendingDelete.Contains(key))
            {
                throw new InvalidOperationException($"Cannot add key '{key}' that is marked for deletion in this transaction.");
            }

            _pendingStore[key] = record;
        }

        public void Update(string key, Record record)
        {
            if (!_pendingDelete.Contains(key) && !_pendingStore.ContainsKey(key))
            {
                throw new KeyNotFoundException($"Key '{key}' not found in transaction or main store.");
            }

            _pendingStore[key] = record;
        }

        public void Delete(string key)
        {
            if (_pendingStore.ContainsKey(key))
            {
                _pendingStore.Remove(key);
            }

            _pendingDelete.Add(key);
        }

        public IReadOnlyDictionary<string, Record> GetPendingStore() => _pendingStore;
        public IReadOnlyCollection<string> GetPendingDelete() => _pendingDelete;
    }
}