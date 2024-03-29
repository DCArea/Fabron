﻿namespace Fabron.Models;

[GenerateSerializer]
[Immutable]
public record CronTimer(
    ScheduleMetadata Metadata,
    CronTimerSpec Spec,
    string? Data
    ) : DistributedTimer<CronTimerSpec>(Metadata, Spec, Data, new());

[GenerateSerializer]
[Immutable]
public record CronTimerSpec
(
    [property: Id(0)]
    string Schedule,

    [property: Id(1)]
    DateTimeOffset? NotBefore = null,

    [property: Id(2)]
    DateTimeOffset? NotAfter = null
) : ISchedulerSpec;
