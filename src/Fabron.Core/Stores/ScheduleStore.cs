using Fabron.Models;

namespace Fabron.Store;

public interface ITimedEventStore : IStateStore<TimedEvent>
{ }
public interface ICronEventStore : IStateStore<CronEvent>
{ }
public interface IPeriodicEventStore : IStateStore<PeriodicEvent>
{ }

public class InMemoryTimedEventStore : InMemoryStateStore<TimedEvent>, ITimedEventStore
{
    protected override string GetStateKey(TimedEvent state)
        => state.Metadata.Key;
}

public class InMemoryCronEventStore : InMemoryStateStore<CronEvent>, ICronEventStore
{
    protected override string GetStateKey(CronEvent state)
        => state.Metadata.Key;
}

public class InMemoryPeriodicEventStore : InMemoryStateStore<PeriodicEvent>, IPeriodicEventStore
{
    protected override string GetStateKey(PeriodicEvent state)
        => state.Metadata.Key;
}

