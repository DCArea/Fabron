// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Threading.Tasks;

using FabronService.Grains;

using Orleans;

namespace FabronService.Services
{

    public class ResourceLocator: IResourceLocator
    {
        private readonly IClusterClient _client;

        public ResourceLocator(IClusterClient client)
        {
            _client = client;
        }

        public async Task<string> GetOrCreateResourceId(string resourceUri)
        {
            var id = await _client.GetGrain<IResourceGrain>(resourceUri).GetOrCreateId();
            return id;
        }

        public async Task<string?> GetResourceId(string resourceUri)
        {
            var id = await _client.GetGrain<IResourceGrain>(resourceUri).GetId();
            return id;
        }
    }

    public interface IResourceLocator
    {
        Task<string> GetOrCreateResourceId(string resourceUri);
        Task<string?> GetResourceId(string resourceUri);
    }
}
