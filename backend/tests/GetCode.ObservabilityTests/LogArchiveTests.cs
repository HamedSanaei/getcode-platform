using System.IO.Compression;
using System.Text;
using GetCode.Infrastructure.Observability.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GetCode.ObservabilityTests;

/// <summary>
/// M00-007 verification: the archive operation is idempotent, crash-safe and
/// lays out closed UTC-day files under {root}/{YYYY}/{MM}/{service}/ while the
/// active day file is untouched. Deleting a month folder is safe because
/// nothing else references it (asserted here by re-running an archive after
/// deleting a month directory).
/// </summary>
public sealed class LogArchiveTests : IDisposable
{
    private const string ServiceName = "getcode-test";
    private const string InstanceId = "inst-1";

    private readonly string _root = Path.Combine(Path.GetTempPath(), "gc-archive-" + Guid.NewGuid().ToString("N"));

    private LogArchiveHostedService CreateService() =>
        new(
            Options.Create(new LogStorageOptions { RootPath = _root }),
            NullLogger<LogArchiveHostedService>.Instance,
            new LogServiceIdentity(ServiceName, InstanceId));

    private string ActiveDir => Path.Combine(_root, "active", ServiceName, InstanceId);

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private static string DayFileName(DateOnly day) => $"{ServiceName}-{day:yyyyMMdd}.jsonl";

    private async Task WriteFileAsync(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, content, Encoding.UTF8);
    }

    [Fact]
    public async Task Closed_days_are_gzipped_into_month_folders_and_active_day_is_untouched()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterdayContent = "{\"line\":1}\n{\"line\":2}\n";
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), yesterdayContent);
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(today)), "active\n");

        var service = CreateService();
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var expectedArchive = Path.Combine(_root, yesterday.Year.ToString("0000"), yesterday.Month.ToString("00"), ServiceName, $"{yesterday:yyyy-MM-dd}-{InstanceId}.jsonl.gz");
        Assert.True(File.Exists(expectedArchive));

        string decompressed;
        await using (var fileStream = File.OpenRead(expectedArchive))
        await using (var gz = new GZipStream(fileStream, CompressionMode.Decompress))
        using (var reader = new StreamReader(gz, Encoding.UTF8))
        {
            decompressed = await reader.ReadToEndAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(yesterdayContent, decompressed);
        Assert.False(File.Exists(Path.Combine(ActiveDir, DayFileName(yesterday))));
        Assert.True(File.Exists(Path.Combine(ActiveDir, DayFileName(today))), "active day file must never be archived");
    }

    [Fact]
    public async Task Re_running_the_archive_is_a_no_op()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), "content\n");

        var service = CreateService();
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var archiveDirectory = Path.Combine(_root, yesterday.Year.ToString("0000"), yesterday.Month.ToString("00"), ServiceName);
        var before = Directory.EnumerateFiles(archiveDirectory).ToList();

        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var after = Directory.EnumerateFiles(archiveDirectory).ToList();
        Assert.Equal(before.OrderBy(x => x), after.OrderBy(x => x));
        Assert.Single(after);
    }

    [Fact]
    public async Task Crashed_archive_with_matching_gzip_is_self_healed()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        var content = "identical-content\n";
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), content);

        var service = CreateService();
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var archiveDirectory = Path.Combine(_root, yesterday.Year.ToString("0000"), yesterday.Month.ToString("00"), ServiceName);
        var gzPath = Path.Combine(archiveDirectory, $"{yesterday:yyyy-MM-dd}-{InstanceId}.jsonl.gz");
        var beforeBytes = await File.ReadAllBytesAsync(gzPath, TestContext.Current.CancellationToken);

        // Simulate a crash after gzip creation but before source deletion.
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), content);

        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        Assert.False(File.Exists(Path.Combine(ActiveDir, DayFileName(yesterday))), "matching leftover source must be removed");
        var afterBytes = await File.ReadAllBytesAsync(gzPath, TestContext.Current.CancellationToken);
        Assert.Equal(beforeBytes, afterBytes);
    }

    [Fact]
    public async Task Destination_conflict_with_different_content_is_preserved_for_investigation()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), "original\n");

        var service = CreateService();
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var archiveDirectory = Path.Combine(_root, yesterday.Year.ToString("0000"), yesterday.Month.ToString("00"), ServiceName);
        var gzPath = Path.Combine(archiveDirectory, $"{yesterday:yyyy-MM-dd}-{InstanceId}.jsonl.gz");
        var beforeBytes = await File.ReadAllBytesAsync(gzPath, TestContext.Current.CancellationToken);

        // A different source with an existing destination must not be destroyed.
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(yesterday)), "different-length!\n");

        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(ActiveDir, DayFileName(yesterday))), "conflicting source is kept for investigation");
        var afterBytes = await File.ReadAllBytesAsync(gzPath, TestContext.Current.CancellationToken);
        Assert.Equal(beforeBytes, afterBytes);
    }

    [Fact]
    public async Task Month_folder_deletion_is_safe_and_archives_resume()
    {
        var twoMonthsAgo = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-2);
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(twoMonthsAgo)), "old\n");

        var service = CreateService();
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        var monthDirectory = Path.Combine(_root, twoMonthsAgo.Year.ToString("0000"), twoMonthsAgo.Month.ToString("00"));
        Assert.True(Directory.Exists(monthDirectory));

        // The documented retention policy: delete the month folder.
        Directory.Delete(monthDirectory, recursive: true);

        // Nothing recreates or caches it; a later archive run works and can rebuild entries.
        await WriteFileAsync(Path.Combine(ActiveDir, DayFileName(twoMonthsAgo)), "old-again\n");
        await service.ArchiveCompletedDaysAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(monthDirectory, ServiceName, $"{twoMonthsAgo:yyyy-MM-dd}-{InstanceId}.jsonl.gz")));
    }
}
