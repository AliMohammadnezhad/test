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
