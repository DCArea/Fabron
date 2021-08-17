﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Orleans;
using Orleans.Concurrency;
using Orleans.Runtime;

namespace FabronService.Grains
{
    public class Resource
    {
        public string? Uri { get; set; }
        public string? Id { get; set; }
    }

    public interface IResourceGrain : IGrainWithStringKey
    {
        [ReadOnly]
        Task<string?> GetId();
        Task<string> GetOrCreateId();
    }

    public class ResourceGrain : Grain, IResourceGrain
    {
        private readonly ILogger<ResourceGrain> _logger;
        private readonly IPersistentState<Resource> _state;

        public ResourceGrain(
            ILogger<ResourceGrain> logger,
            [PersistentState("ResourceIds")] IPersistentState<Resource> state)
        {
            _logger = logger;
            _state = state;
        }

        public Task<string?> GetId()
        {
            return Task.FromResult(_state.State.Id);
        }

        public async Task<string> GetOrCreateId()
        {
            _state.State = new Resource
            {
                Uri = this.GetPrimaryKeyString(),
                Id = Guid.NewGuid().ToString()
            };
            await _state.WriteStateAsync();
            return _state.State.Id;
        }
    }

}