# راه‌حل: Archivability & Supportability
# پروژه: Dotnet10ArchivabilitySupportabilityAssessment (.NET 10 Console App)

## ساختار پروژه

```
ArchivabilityDemo/
├── ArchivabilityDemo.csproj
├── Program.cs                        ← Composition Root
├── DataManager.cs                    ← منطق اصلی (بایگانی + لاگ)
├── Models/
│   └── DataItem.cs
├── Archiving/
│   ├── IArchiveProvider.cs           ← قرارداد قابل‌توسعه
│   └── JsonFileArchiveProvider.cs    ← پیاده‌سازی JSON
└── Logging/
    ├── IAppLogger.cs                 ← قرارداد قابل‌توسعه
    ├── CompositeLogger.cs            ← fan-out به چند لاگر
    ├── ConsoleLogger.cs              ← خروجی کنسول
    └── FileLogger.cs                 ← خروجی فایل
```

---

## Models/DataItem.cs

```csharp
namespace Dotnet10ArchivabilitySupportabilityAssessment.Models;

public class DataItem
{
    public int Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

---

## Logging/IAppLogger.cs

```csharp
namespace Dotnet10ArchivabilitySupportabilityAssessment.Logging;

public enum LogLevel { Info, Warning, Error }

/// قرارداد لاگینگ — هر خروجی جدید (فایل، Seq، Elasticsearch و …)
/// فقط این اینترفیس را implement می‌کند.
public interface IAppLogger
{
    void Log(LogLevel level, string message, Exception? ex = null);

    void Info(string message)    => Log(LogLevel.Info,    message);
    void Warning(string message) => Log(LogLevel.Warning, message);
    void Error(string message, Exception? ex = null) => Log(LogLevel.Error, message, ex);
}
```

---

## Logging/ConsoleLogger.cs

```csharp
namespace Dotnet10ArchivabilitySupportabilityAssessment.Logging;

public sealed class ConsoleLogger : IAppLogger
{
    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        var (color, prefix) = level switch
        {
            LogLevel.Info    => (ConsoleColor.Cyan,   "INFO "),
            LogLevel.Warning => (ConsoleColor.Yellow, "WARN "),
            LogLevel.Error   => (ConsoleColor.Red,    "ERROR"),
            _                => (ConsoleColor.White,  "LOG  ")
        };

        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{prefix}] {message}");
        if (ex is not null)
            Console.WriteLine($"         Exception: {ex.Message}");
        Console.ForegroundColor = original;
    }
}
```

---

## Logging/FileLogger.cs

```csharp
namespace Dotnet10ArchivabilitySupportabilityAssessment.Logging;

public sealed class FileLogger : IAppLogger, IDisposable
{
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    public FileLogger(string logPath)
    {
        _writer = new StreamWriter(logPath, append: true) { AutoFlush = true };
    }

    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] [{level,-7}] {message}";
        if (ex is not null)
            line += $" | Exception: {ex.Message}";

        lock (_lock)
            _writer.WriteLine(line);
    }

    public void Dispose() => _writer.Dispose();
}
```

---

## Logging/CompositeLogger.cs

```csharp
namespace Dotnet10ArchivabilitySupportabilityAssessment.Logging;

/// لاگر مرکب — پیام را به همه لاگرهای ثبت‌شده ارسال می‌کند.
/// افزودن خروجی جدید: فقط یک IAppLogger جدید بسازید و به اینجا اضافه کنید.
public sealed class CompositeLogger : IAppLogger
{
    private readonly IReadOnlyList<IAppLogger> _loggers;

    public CompositeLogger(params IAppLogger[] loggers)
    {
        _loggers = loggers;
    }

    public void Log(LogLevel level, string message, Exception? ex = null)
    {
        foreach (var logger in _loggers)
            logger.Log(level, message, ex);
    }
}
```

---

## Archiving/IArchiveProvider.cs

```csharp
using Dotnet10ArchivabilitySupportabilityAssessment.Models;

namespace Dotnet10ArchivabilitySupportabilityAssessment.Archiving;

/// قرارداد بایگانی — هر پیاده‌سازی جدید (JSON، DB، S3 و …) فقط این را implement می‌کند.
public interface IArchiveProvider
{
    Task ArchiveAsync(IEnumerable<DataItem> items, CancellationToken ct = default);
}
```

---

## Archiving/JsonFileArchiveProvider.cs

```csharp
using System.Text.Json;
using Dotnet10ArchivabilitySupportabilityAssessment.Models;

namespace Dotnet10ArchivabilitySupportabilityAssessment.Archiving;

public sealed class JsonFileArchiveProvider : IArchiveProvider
{
    private readonly string _archivePath;
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public JsonFileArchiveProvider(string archivePath)
    {
        _archivePath = archivePath;
    }

    public async Task ArchiveAsync(IEnumerable<DataItem> items, CancellationToken ct = default)
    {
        // خواندن آرشیو موجود (اگر وجود داشت)
        List<DataItem> existing = [];
        if (File.Exists(_archivePath))
        {
            await using var readStream = File.OpenRead(_archivePath);
            existing = await JsonSerializer.DeserializeAsync<List<DataItem>>(
                           readStream, _jsonOptions, ct) ?? [];
        }

        existing.AddRange(items);

        await using var writeStream = File.Create(_archivePath);
        await JsonSerializer.SerializeAsync(writeStream, existing, _jsonOptions, ct);
    }
}
```

---

## DataManager.cs

```csharp
using Dotnet10ArchivabilitySupportabilityAssessment.Archiving;
using Dotnet10ArchivabilitySupportabilityAssessment.Logging;
using Dotnet10ArchivabilitySupportabilityAssessment.Models;

namespace Dotnet10ArchivabilitySupportabilityAssessment;

/// منطق اصلی: بایگانی داده‌های منقضی و ثبت تمام رویدادها.
/// وابستگی‌ها از طریق Constructor Injection تزریق می‌شوند.
public sealed class DataManager
{
    private readonly IArchiveProvider _archiver;
    private readonly IAppLogger _logger;

    public DataManager(IArchiveProvider archiver, IAppLogger logger)
    {
        _archiver = archiver;
        _logger   = logger;
    }

    public async Task RunAsync(List<DataItem> dataStore, int daysToKeep,
                               CancellationToken ct = default)
    {
        _logger.Info($"شروع پردازش — تعداد آیتم‌ها: {dataStore.Count}");

        var now     = DateTime.UtcNow;
        var expired = dataStore
            .Where(i => (now - i.CreatedAt).TotalDays > daysToKeep)
            .ToList();

        if (expired.Count == 0)
        {
            _logger.Info("هیچ آیتم منقضی‌ای برای بایگانی وجود ندارد.");
            return;
        }

        _logger.Info($"آیتم‌های منقضی: {expired.Count} — در حال بایگانی...");

        try
        {
            await _archiver.ArchiveAsync(expired, ct);
            _logger.Info($"بایگانی موفق — {expired.Count} آیتم ذخیره شد.");
        }
        catch (Exception ex)
        {
            _logger.Error("خطا در بایگانی داده‌ها.", ex);
            throw; // حذف فقط پس از بایگانی موفق انجام می‌شود
        }

        foreach (var item in expired)
        {
            dataStore.Remove(item);
            _logger.Info($"آیتم حذف شد — Id={item.Id}, Value={item.Value}");
        }

        _logger.Info($"پایان پردازش — آیتم‌های باقی‌مانده: {dataStore.Count}");
    }
}
```

---

## Program.cs  (Composition Root)

```csharp
using Dotnet10ArchivabilitySupportabilityAssessment;
using Dotnet10ArchivabilitySupportabilityAssessment.Archiving;
using Dotnet10ArchivabilitySupportabilityAssessment.Logging;
using Dotnet10ArchivabilitySupportabilityAssessment.Models;

var archivePath = Path.Combine(AppContext.BaseDirectory, "archive.json");
var logPath     = Path.Combine(AppContext.BaseDirectory, "app.log");

using var fileLogger = new FileLogger(logPath);

IAppLogger logger = new CompositeLogger(
    new ConsoleLogger(),
    fileLogger
);

IArchiveProvider archiver = new JsonFileArchiveProvider(archivePath);
var manager = new DataManager(archiver, logger);

var dataStore = new List<DataItem>
{
    new() { Id = 1, Value = "Alpha", CreatedAt = DateTime.UtcNow.AddDays(-10) },
    new() { Id = 2, Value = "Beta",  CreatedAt = DateTime.UtcNow.AddDays(-1)  },
    new() { Id = 3, Value = "Gamma", CreatedAt = DateTime.UtcNow              },
};

Console.WriteLine("─── داده‌های موجود ───────────────────────────");
foreach (var item in dataStore)
    Console.WriteLine($"  {item.Id}: {item.Value} ({item.CreatedAt:yyyy-MM-dd HH:mm:ss})");

Console.WriteLine("\nتعداد روز نگهداری داده را وارد کنید:");
var input = Console.ReadLine();

if (!int.TryParse(input, out int daysToKeep) || daysToKeep < 0)
{
    logger.Error("ورودی نامعتبر است. برنامه خاتمه می‌یابد.");
    return;
}

try
{
    await manager.RunAsync(dataStore, daysToKeep);
}
catch
{
    Console.WriteLine("[خطا] عملیات ناموفق بود. جزئیات در فایل لاگ موجود است.");
    return;
}

Console.WriteLine("\n─── داده‌های باقی‌مانده ───────────────────────");
if (dataStore.Count == 0)
    Console.WriteLine("  (هیچ آیتمی باقی نمانده)");
else
    foreach (var item in dataStore)
        Console.WriteLine($"  {item.Id}: {item.Value} ({item.CreatedAt:yyyy-MM-dd HH:mm:ss})");

Console.WriteLine($"\nآرشیو: {archivePath}");
Console.WriteLine($"لاگ:   {logPath}");
```
