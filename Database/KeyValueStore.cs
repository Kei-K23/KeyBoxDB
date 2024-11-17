using System.Collections.Concurrent;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class KeyValueStore
    {
        private readonly ConcurrentDictionary<string, Record> _store;
        private readonly StorageEngine _storageEngine;

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
            if (_store.TryGetValue(key, out var record))
            {
                return record.Value;
            }
            throw new KeyNotFoundException($"Key: '{key}' not found.");
        }

        public void Update(string key, string newValue)
        {
            if (!_store.ContainsKey(key))
            {
                throw new KeyNotFoundException($"Key: '{key}' not found.");
            }

            _store[key].Value = newValue;
            _store[key].Timestamp = DateTime.UtcNow;

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
            return _store.Values;
        }

        private void Persist()
        {
            _storageEngine.Save(_store);
        }
    }
}