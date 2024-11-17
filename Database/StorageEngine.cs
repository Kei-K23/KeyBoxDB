using System.Collections.Concurrent;
using System.Text.Json;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class StorageEngine(string filePath)
    {
        private readonly string _filePath = filePath;

        public void Save(ConcurrentDictionary<string, Record> data)
        {
            // Convert JSON object into JSON string
            var jsonData = JsonSerializer.Serialize(data);
            // Write to file
            File.WriteAllText(_filePath, jsonData);
        }

        public ConcurrentDictionary<string, Record> Load()
        {
            var emptyData = new ConcurrentDictionary<string, Record>();
            // Not found database file, then return new empty record
            if (!File.Exists(_filePath))
            {
                return emptyData;
            }

            // Read JSON string data from database file and convert to JSON object
            var jsonStrData = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<ConcurrentDictionary<string, Record>>(jsonStrData) ?? emptyData;
        }
    }
}