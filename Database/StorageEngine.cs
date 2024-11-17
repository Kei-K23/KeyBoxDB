using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using KeyBoxDB.Models;

namespace KeyBoxDB.Database
{
    public class StorageEngine(string filePath)
    {
        private readonly string _filePath = filePath;

        public void Save(ConcurrentDictionary<string, Record> data)
        {
            try
            {
                using var fileStream = File.Create(_filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Compress);
                using var writer = new StreamWriter(gzipStream);

                // Convert JSON object into JSON string
                var jsonData = JsonSerializer.Serialize(data);
                // Write to file
                writer.Write(jsonData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving database: {ex.Message}");
            }
        }

        public ConcurrentDictionary<string, Record> Load()
        {
            if (!File.Exists(_filePath) || new FileInfo(_filePath).Length == 0)
                return new ConcurrentDictionary<string, Record>();

            try
            {
                using var fileStream = File.OpenRead(_filePath);
                using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
                using var reader = new StreamReader(gzipStream);

                var jsonData = reader.ReadToEnd();

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