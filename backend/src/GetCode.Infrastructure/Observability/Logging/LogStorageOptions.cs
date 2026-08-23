namespace GetCode.Infrastructure.Observability.Logging;

public sealed class LogStorageOptions
{
    public const string SectionName = "LogStorage";

    public string RootPath { get; init; } = "./logs";
    public int ArchiveIntervalMinutes { get; init; } = 60;

    /// <summary>
    /// 0 means disabled. Manual month-folder deletion remains supported and is the default policy.
    /// </summary>
    public int AutomaticRetentionMonths { get; init; }
}
