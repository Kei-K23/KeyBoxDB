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
        private readonly ReaderWriterLockSlim _look = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

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

        public void Add(string key, string value, TimeSpan? ttl = null)
        {
            if (_store.ContainsKey(key))
            {
                throw new InvalidOperationException($"Key: '{key}' already exists.");
            }

            var record = new Record { Key = key, Value = value, ExpirationDate = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null };
            // Save the value
            _store[key] = record;
            // Save the index
            _index[key] = record.Timestamp.Ticks;
            Persist();
        }

        public string Get(string key)
        {
            _look.EnterReadLock();
            try
            {
                if (_index.ContainsKey(key) && _store.TryGetValue(key, out var record))
                {
                    if (record.IsExpired())
                    {
                        _look.ExitReadLock();
                        // Delete the key
                        Delete(key);
                        _look.EnterReadLock();
                        throw new KeyNotFoundException($"Key '{key}' has expired.");
                    }

                    return record.Value;
                }
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }
            finally
            {
                _look.ExitReadLock();
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
            if (!_store.TryRemove(key, out _))
            {
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }
            _index.TryRemove(key, out _);

            Persist();
        }

        public IEnumerable<Record> GetAll()
        {
            _look.EnterReadLock();
            try
            {
                return _store.Values.ToList();
            }
            finally
            {
                _look.ExitReadLock();
            }
        }

        // Method to stop the background checking thread by sending cancel signal
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _look.Dispose(); // Release all resources
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
            _look.EnterWriteLock();
            try
            {
                _storageEngine.Save(_store);
            }
            finally
            {
                _look.ExitWriteLock();

            }
        }

        private void BuildIndex()
        {
            _look.EnterWriteLock();
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
                _look.ExitWriteLock();
            }
        }
    }
}