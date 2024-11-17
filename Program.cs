using KeyBoxDB.Database;

class Program
{
    // Main entry point of the program
    static void Main()
    {
        string databasePath = "database.json";
        var keyValueStore = new KeyValueStore(databasePath);

        Console.WriteLine("Welcome to the KeyBoxDB!");
        while (true)
        {
            Console.WriteLine("\nOptions: [add] [get] [update] [delete] [list] [exit]");
            Console.Write("Enter command: ");

            var command = Console.ReadLine()?.ToLower();

            try
            {
                switch (command)
                {
                    case "add":
                        Console.Write("Key: ");
                        var addKey = Console.ReadLine()?.Trim();

                        Console.Write("Value: ");
                        var addValue = Console.ReadLine()?.Trim();

                        keyValueStore.Add(addKey!, addValue!);
                        Console.WriteLine("Record added successfully.");
                        break;
                    case "update":
                        Console.Write("Key: ");
                        var updateKey = Console.ReadLine()?.Trim();

                        Console.Write("New Value: ");
                        var updateValue = Console.ReadLine()?.Trim();

                        keyValueStore.Update(updateKey!, updateValue!);
                        Console.WriteLine("Record updated successfully.");
                        break;
                    case "delete":
                        Console.Write("Key: ");
                        var deleteKey = Console.ReadLine()?.Trim();

                        keyValueStore.Delete(deleteKey!);
                        Console.WriteLine("Record deleted successfully.");
                        break;
                    case "get":
                        Console.Write("Key: ");
                        var getKey = Console.ReadLine()?.Trim();

                        var getValue = keyValueStore.Get(getKey!);
                        Console.WriteLine($"Value: {getValue}");
                        break;
                    case "list":
                        Console.WriteLine("All Records:");
                        foreach (var record in keyValueStore.GetAll())
                        {
                            Console.WriteLine(record);
                        }
                        break;
                    case "exit":
                        Console.WriteLine("Bye...");
                        return;
                    default:
                        Console.WriteLine("Invalid command! Try again.");
                        break;
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine($"Error: {ex.Message}");
            }

        }
    }
}