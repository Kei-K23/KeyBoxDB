using System.Collections.Concurrent;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class KeyValueStore
    {
        private readonly ConcurrentDictionary<string, Record> _store;
        private readonly StorageEngine _storageEngine;
        private readonly ReaderWriterLockSlim _look = new ReaderWriterLockSlim();

        public KeyValueStore(string databasePath)
        {
            _storageEngine = new StorageEngine(databasePath);
            _store = _storageEngine.Load();
        }

        public void Add(string key, string value)
        {
            if (_store.ContainsKey(key))
            {
                throw new InvalidOperationException($"Key: '{key}' already exists.");
            }

            _store[key] = new Record { Key = key, Value = value };
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
                    return record.Value;
                }
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }
            finally
            {
                _look.ExitReadLock();
            }
        }

        public void Update(string key, string newValue)
        {
            if (!_store.TryGetValue(key, out Record? value))
            {
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }

            value.Value = newValue;
            value.Timestamp = DateTime.UtcNow;

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