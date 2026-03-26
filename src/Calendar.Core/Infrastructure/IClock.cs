namespace Calendar.Core.Infrastructure;

public interface IClock
{
    DateTimeOffset Now { get; }
}
