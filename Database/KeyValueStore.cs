using System.Collections.Concurrent;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class KeyValueStore
    {
        private readonly ConcurrentDictionary<string, Record> _store;
        // Using Concurrent Directory as in-memory indexing for quick lookup data
        private readonly ConcurrentDictionary<string, long> _index;
        private readonly StorageEngine _storageEngine;
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Transaction _currentTransaction;
        private bool _isInTransaction;

        public KeyValueStore(string databasePath)
        {
            _storageEngine = new StorageEngine(databasePath);
            _store = _storageEngine.Load();
            _index = new ConcurrentDictionary<string, long>();

            // Start in-memory indexing store
            BuildIndex();

            // Start the background checking thread
            Task.Run(() => CleanupExpiredKey(_cancellationTokenSource.Token));
        }


        public void BeginTransaction()
        {
            _lock.EnterWriteLock();
            try
            {
                if (_isInTransaction)
                    throw new InvalidOperationException("A transaction is already in progress.");

                _currentTransaction = new Transaction();
                _isInTransaction = true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void CommitTransaction()
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_isInTransaction)
                    throw new InvalidOperationException("No transaction in progress.");

                // Apply pending changes
                foreach (var (key, record) in _currentTransaction.GetPendingStore())
                {
                    _store[key] = record;
                    _index[key] = record.Timestamp.Ticks;
                }

                foreach (var key in _currentTransaction.GetPendingDelete())
                {
                    _store.TryRemove(key, out _);
                    _index.TryRemove(key, out _);
                }

                // Persist changes to disk
                Persist();

                // Clear transaction state
                _currentTransaction = null;
                _isInTransaction = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void RollbackTransaction()
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_isInTransaction)
                    throw new InvalidOperationException("No transaction in progress.");

                // Discard pending changes
                _currentTransaction = null;
                _isInTransaction = false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }


        public void Add(string key, string value, TimeSpan? ttl = null)
        {
            if (_isInTransaction)
            {
                var record = new Record
                {
                    Key = key,
                    Value = value,
                    ExpirationDate = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
                };
                _currentTransaction.Add(key, record);
                return;
            }

            if (_store.ContainsKey(key))
                throw new InvalidOperationException($"Key '{key}' already exists.");

            var newRecord = new Record
            {
                Key = key,
                Value = value,
                ExpirationDate = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null
            };

            _store[key] = newRecord;
            _index[key] = newRecord.Timestamp.Ticks;
            Persist();
        }

        public string Get(string key)
        {
            _lock.EnterReadLock();
            try
            {
                if (_index.ContainsKey(key) && _store.TryGetValue(key, out var record))
                {
                    if (record.IsExpired())
                    {
                        _lock.ExitReadLock();
                        // Delete the key
                        Delete(key);
                        _lock.EnterReadLock();
                        throw new KeyNotFoundException($"Key '{key}' has expired.");
                    }

                    return record.Value;
                }
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Update(string key, string newValue, TimeSpan? ttl = null)
        {

            if (!_store.TryGetValue(key, out Record? value))
            {
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }

            if (value.IsExpired())
            {
                // Delete the key
                Delete(key);
                throw new KeyNotFoundException($"Key '{key}' has expired.");
            }

            value.Value = newValue;
            value.Timestamp = DateTime.UtcNow;
            value.ExpirationDate = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null;

            _index[key] = value.Timestamp.Ticks;
            Persist();
        }

        public void Delete(string key)
        {
            if (_isInTransaction)
            {
                _currentTransaction.Delete(key);
                return;
            }

            if (!_store.TryRemove(key, out _))
                throw new KeyNotFoundException($"Key '{key}' not found.");

            _index.TryRemove(key, out _);
            Persist();
        }

        public IEnumerable<Record> GetAll()
        {
            _lock.EnterReadLock();
            try
            {
                return _store.Values.ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        // Method to stop the background checking thread by sending cancel signal
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _lock.Dispose(); // Release all resources
        }

        private void CleanupExpiredKey(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var key in _store.Keys.ToList())
                {
                    // If key is expired, remove that record
                    if (_store[key].IsExpired())
                    {
                        _store.TryRemove(key, out _);
                        _index.TryRemove(key, out _);
                    }
                }
                Persist();

                // Run this loop every 5 seconds. Background checking
                Thread.Sleep(5000); // 5 seconds
            }
        }

        private void Persist()
        {
            _lock.EnterWriteLock();
            try
            {
                _storageEngine.Save(_store);
            }
            finally
            {
                _lock.ExitWriteLock();

            }
        }

        private void BuildIndex()
        {
            _lock.EnterWriteLock();
            try
            {
                // Fill in-memory data from store
                foreach (var (key, record) in _store)
                {
                    if (!record.IsExpired())
                    {
                        _index[key] = record.Timestamp.Ticks;
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}