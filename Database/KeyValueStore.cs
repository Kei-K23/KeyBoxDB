using System.Collections.Concurrent;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class KeyValueStore
    {
        private readonly ConcurrentDictionary<string, Record> _store;
        private readonly StorageEngine _storageEngine;
        private readonly ReaderWriterLockSlim _look = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public KeyValueStore(string databasePath)
        {
            _storageEngine = new StorageEngine(databasePath);
            _store = _storageEngine.Load();

            // Start the background checking thread
            Task.Run(() => CleanupExpiredKey(_cancellationTokenSource.Token));
        }

        public void Add(string key, string value, TimeSpan? ttl = null)
        {
            if (_store.ContainsKey(key))
            {
                throw new InvalidOperationException($"Key: '{key}' already exists.");
            }

            _store[key] = new Record { Key = key, Value = value, ExpirationDate = ttl.HasValue ? DateTime.UtcNow.Add(ttl.Value) : null };
            // Save the value
            Persist();
        }

        public string Get(string key)
        {
            _look.EnterReadLock();
            try
            {
                if (_store.TryGetValue(key, out var record))
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

            Persist();
        }

        public void Delete(string key)
        {
            if (!_store.TryRemove(key, out _))
            {
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }

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
    }
}