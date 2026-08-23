namespace GetCode.Application.Common;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
