# KeyBoxDB 📦: Lightweight Key-Value Storage

**KeyBoxDB** 📦 is a lightweight, in-memory, and file-persisted key-value storage database for .NET applications. It supports basic CRUD operations, time-to-live (TTL) for keys, batch operations with transactions, and automatic cleanup of expired keys.

## Features

- **CRUD Operations**: Add, retrieve, update, and delete key-value pairs.
- **Persistent Storage**: Data is stored in a compressed file and automatically reloaded on startup.
- **TTL (Time-to-Live)**: Automatically expire keys after a specified duration.
- **Transactions**: Support for batch operations with commit and rollback.
- **Concurrency**: Thread-safe operations with efficient locking mechanisms.
- **Background Cleanup**: Automatic removal of expired keys.
- **In-Memory Indexing**: Fast lookups using in-memory indexing.

---

## Getting Started

### Prerequisites

- **.NET 6.0 or later**
- Add a reference to `System.Collections.Concurrent` and `System.IO.Compression`.

---

### Installation

Clone the repository and add the `KeyBoxDB` project to your solution. Alternatively, include the source files directly in your project.

---

### Usage

#### Initializing the Key-Value Store

```csharp
using KeyBoxDB.Database;

// Initialize with a path for persistent storage
var kvStore = new KeyValueStore("path/to/database.db");
```

---

#### Basic Operations

##### Add a Key

```csharp
kvStore.Add("exampleKey", "exampleValue", TimeSpan.FromMinutes(5)); // Optional TTL
```

##### Get a Key

```csharp
try
{
    var value = kvStore.Get("exampleKey");
    Console.WriteLine($"Value: {value}");
}
catch (KeyNotFoundException ex)
{
    Console.WriteLine(ex.Message);
}
```

##### Update a Key

```csharp
kvStore.Update("exampleKey", "newValue", TimeSpan.FromHours(1));
```

##### Delete a Key

```csharp
kvStore.Delete("exampleKey");
```

##### Get All Keys

```csharp
foreach (var record in kvStore.GetAll())
{
    Console.WriteLine($"{record.Key}: {record.Value}");
}
```

---

#### Transactions

##### Begin a Transaction

```csharp
kvStore.BeginTransaction();
```

##### Perform Batch Operations

```csharp
kvStore.Add("key1", "value1");
kvStore.Update("key2", "newValue2");
kvStore.Delete("key3");
```

##### Commit the Transaction

```csharp
kvStore.CommitTransaction();
```

##### Rollback the Transaction

```csharp
kvStore.RollbackTransaction();
```

---

#### Graceful Shutdown

Always dispose of the store to stop background tasks and release resources.

```csharp
kvStore.Dispose();
```

---

## Internals

### Record Structure

Each key-value pair is stored as a `Record`:

```csharp
public class Record
{
    public string Key { get; set; }
    public string Value { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public DateTime? ExpirationDate { get; set; }

    public bool IsExpired() => ExpirationDate.HasValue && ExpirationDate.Value <= DateTime.UtcNow;
}
```

---

### Persistent Storage

The database uses a compressed JSON file to persist the data:

- **Save**: Data is serialized to JSON and compressed using GZip.
- **Load**: Data is decompressed and deserialized on startup.

---

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature`).
3. Commit your changes (`git commit -m 'Add feature'`).
4. Push the branch (`git push origin feature/your-feature`).
5. Open a pull request.

---

## License

This project is licensed under the [MIT License](LICENSE).
