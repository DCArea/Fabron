using System;
using System.Collections.Generic;
using Orleans;

namespace Fabron.Models;

[GenerateSerializer]
public class ObjectMetadata
{
    [Id(0)]
    public string Name { get; set; } = default!;

    [Id(1)]
    public string Namespace { get; set; } = default!;

    [Id(2)]
    public string UID { get; set; } = default!;

    [Id(3)]
    public DateTimeOffset? CreationTimestamp { get; set; }

    [Id(4)]
    public DateTimeOffset? DeletionTimestamp { get; set; }

    [Id(5)]
    public Dictionary<string, string>? Labels { get; set; }

    [Id(6)]
    public Dictionary<string, string>? Annotations { get; set; }

    [Id(7)]
    public OwnerReference? Owner { get; set; }
}

[GenerateSerializer]
public class OwnerReference
{
    [Id(0)]
    public string Kind { get; set; } = default!;

    [Id(1)]
    public string Name { get; set; } = default!;
}

[GenerateSerializer]
public class CommandSpec
{

    [Id(0)]
    public string Name { get; init; } = default!;

    [Id(1)]
    public string Data { get; init; } = default!;
}
