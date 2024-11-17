namespace KeyBoxDB.Models
{
    public class Record
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"{Key}:{Value} (Last Updated {Timestamp})";
        }
    }
}