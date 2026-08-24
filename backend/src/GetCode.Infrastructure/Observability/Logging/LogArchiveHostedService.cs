using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GetCode.Infrastructure.Observability.Logging;

internal sealed partial class LogArchiveHostedService(
    IOptions<LogStorageOptions> options,
    ILogger<LogArchiveHostedService> logger,
    LogServiceIdentity identity) : BackgroundService
{
    private readonly LogStorageOptions _options = options.Value;

    [GeneratedRegex(@"-(?<date>\d{8})(?<chunk>_\d+)?\.jsonl$", RegexOptions.CultureInvariant)]
    private static partial Regex RolledLogRegex();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(5, _options.ArchiveIntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveCompletedDaysAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "log.archive.failed for {Service}", identity.ServiceName);
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    /// <summary>
    /// Archives every closed UTC-day JSONL file into
    /// {root}/{YYYY}/{MM}/{service}/{yyyy-MM-dd}-{instance}[-chunk].jsonl.gz.
    /// Idempotent: a completed archive is never written twice; a crashed run
    /// (gzip present but source not yet removed) self-heals by verifying the
    /// archived byte count before discarding the source.
    /// </summary>
    internal async Task ArchiveCompletedDaysAsync(CancellationToken cancellationToken)
    {
        var activeDirectory = Path.Combine(_options.RootPath, "active", identity.ServiceName, identity.InstanceId);
        if (!Directory.Exists(activeDirectory))
        {
            return;
        }

        // Services run with TZ=UTC; the rolling filename date is treated as the UTC day it covers.
        var currentUtcDay = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var sourcePath in Directory.EnumerateFiles(activeDirectory, "*.jsonl", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var match = RolledLogRegex().Match(Path.GetFileName(sourcePath));
            if (!match.Success || !DateOnly.TryParseExact(match.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) || day >= currentUtcDay)
            {
                continue;
            }

            var chunk = match.Groups["chunk"].Success ? match.Groups["chunk"].Value.Replace('_', '-') : string.Empty;
            var archiveDirectory = Path.Combine(_options.RootPath, day.Year.ToString("0000"), day.Month.ToString("00"), identity.ServiceName);
            Directory.CreateDirectory(archiveDirectory);
            var destination = Path.Combine(archiveDirectory, $"{day:yyyy-MM-dd}-{identity.InstanceId}{chunk}.jsonl.gz");

            if (File.Exists(destination))
            {
                // Previous run may have crashed between gzip creation and source deletion.
                if (await ArchivedLengthMatchesAsync(destination, sourcePath, cancellationToken))
                {
                    File.Delete(sourcePath);
                    logger.LogInformation("log.archive.recovered_after_crash {Source} {Destination}", sourcePath, destination);
                }
                else
                {
                    logger.LogWarning("log.archive.destination_exists {Destination}", destination);
                }

                continue;
            }

            var tempDestination = destination + $".tmp-{Guid.NewGuid():N}";
            try
            {
                await using (var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                await using (var target = new FileStream(tempDestination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                await using (var gzip = new GZipStream(target, CompressionLevel.SmallestSize, leaveOpen: false))
                {
                    await source.CopyToAsync(gzip, cancellationToken);
                }

                File.Move(tempDestination, destination);
                File.Delete(sourcePath);
                logger.LogInformation("log.archive.completed {Source} {Destination}", sourcePath, destination);
            }
            finally
            {
                if (File.Exists(tempDestination))
                {
                    File.Delete(tempDestination);
                }
            }
        }
    }

    private static async Task<bool> ArchivedLengthMatchesAsync(string gzPath, string sourcePath, CancellationToken cancellationToken)
    {
        try
        {
            long archivedBytes;
            await using (var gzStream = new FileStream(gzPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            await using (var gzip = new GZipStream(gzStream, CompressionMode.Decompress))
            {
                archivedBytes = 0;
                var buffer = new byte[81920];
                int read;
                while ((read = await gzip.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
                {
                    archivedBytes += read;
                }
            }

            var sourceInfo = new FileInfo(sourcePath);
            return sourceInfo.Exists && sourceInfo.Length == archivedBytes;
        }
        catch (IOException)
        {
            return false;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}

public sealed record LogServiceIdentity(string ServiceName, string InstanceId);
