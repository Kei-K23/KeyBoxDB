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
            if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
                return new ConcurrentDictionary<string, Record>();

            try
            {
                var jsonData = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<ConcurrentDictionary<string, Record>>(jsonData)
                       ?? new ConcurrentDictionary<string, Record>();
            }
            catch (JsonException)
            {
                Console.WriteLine("Error: Invalid JSON data in the storage file. Starting with a fresh database.");
                return new ConcurrentDictionary<string, Record>();
            }
        }

    }
}