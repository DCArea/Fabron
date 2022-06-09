using Fabron.Models;

namespace Fabron.Store;

public interface ITimedEventStore : IStateStore2<TimedEvent>
{ }
public interface ICronEventStore : IStateStore2<CronEvent>
{ }
public interface IPeriodicEventStore : IStateStore2<PeriodicEvent>
{ }

public class InMemoryTimedEventStore : InMemoryStateStore2<TimedEvent>, ITimedEventStore
{
    protected override string GetStateKey(TimedEvent state)
        => state.Metadata.Key;
}

public class InMemoryCronEventStore : InMemoryStateStore2<CronEvent>, ICronEventStore
{
    protected override string GetStateKey(CronEvent state)
        => state.Metadata.Key;
}

public class InMemoryPeriodicEventStore : InMemoryStateStore2<PeriodicEvent>, IPeriodicEventStore
{
    protected override string GetStateKey(PeriodicEvent state)
        => state.Metadata.Key;
}

