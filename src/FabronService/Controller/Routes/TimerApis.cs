using System.Security.Claims;
using CommunityToolkit.Diagnostics;
using Fabron;
using Fabron.Models;
using Fabron.Stores;
using FabronService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FabronService.Controller.Routes;

public enum TimerType
{
    Generic, Periodic, Cron
}

internal static class TimerApis<TFabronTimer>
    where TFabronTimer : IDistributedTimer
{
    private static readonly string type = GetSchedulerType();

    public static RouteGroupBuilder MapTimerApis<TTimer, TDto>(IEndpointRouteBuilder route, string path, string name)
        where TTimer : IDistributedTimer
    {
        var clientApis = route.MapGroup(path)
            .WithGroupName(name)
            .AddEndpointFilter((ctx, next) =>
            {
                var clientId = ctx.HttpContext.User.GetClientId();
                if (clientId == null)
                {
                    return ValueTask.FromResult((object?)Results.Problem("client not found", statusCode: 404));
                }
                ctx.Arguments.Add(clientId);
                return next(ctx);
            })
            .ProducesProblem(401)
            .ProducesProblem(403)
            .ProducesProblem(404)
            ;

        clientApis.MapGet("{key}", async (
            string key,
            string clientId,
            ITimerManager<TFabronTimer> fabron) =>
            {
                var timer = await fabron.Get($"{clientId}/{key}");
                return timer is null ? Results.Problem($"Timer {key} not found", statusCode: 404) : Results.Ok(timer);
            })
            .Produces<CronTimer>();

        clientApis.MapDelete("{key}", async (
            string clientId,
            string key,
            ITimerManager<TFabronTimer> fabron) =>
            {
                var fullKey = $"{clientId}/{key}";
                await fabron.Delete(fullKey);
                return Results.NoContent();
            })
            .Produces(204);

        clientApis.MapPut("{key}/ticker", async (
            string key,
            string clientId,
            ITimerManager<TFabronTimer> fabron) =>
            {
                await fabron.Start($"{clientId}/{key}");
                return Results.NoContent();
            })
            .Produces(204);

        clientApis.MapDelete("{key}/ticker", async (
            string clientId,
            string key,
            ITimerManager<TFabronTimer> fabron) =>
            {
                await fabron.Stop($"{clientId}/{key}");
                return Results.NoContent();
            })
            .Produces(204);

        clientApis.MapPut("{key}/tick", async (
            string clientId,
            string key,
            ITimerManager<TFabronTimer> fabron) =>
            {
                var fullKey = $"{clientId}/{key}";
                await fabron.Tick(fullKey);
                return Results.NoContent();
            })
            .Produces(204);

        clientApis.MapGet("", async (
            string clientId,
            string? key,
            int? page,
            int? pageSize,
            IQueryable<TimerStateEntry<TTimer>> store) =>
            {
                var pList = await GetListInternal(store, clientId, key, page, pageSize);
                return Results.Ok(pList);
            })
            .Produces<PaginatedList<TTimer>>(200);

        //clientApis.MapGet("{key}/fires", GetFires);

        return clientApis;
    }

    private static async Task<PaginatedList<TTimer>> GetListInternal<TTimer>(
        IQueryable<TimerStateEntry<TTimer>> set,
        string clientId,
        string? key,
        int? skip,
        int? take)
    {
        skip ??= 1;
        take ??= 20;

        var q = set
            .Where(e => e.Key.StartsWith(clientId + '/'));
        if (key is { Length: > 0 })
        {
            q = q.Where(e => e.Key.Contains(key));
        }

        var items = await q
            .OrderByDescending(e => EF.Property<DateTime>(e, "created_at"))
            .Select(e => e.Data)
            .CountAndPagingAsync(skip.Value, take.Value, default);

        return items;
    }

    private static string GetSchedulerType()
    {
        var timerType = typeof(TFabronTimer);
        return typeof(TFabronTimer) switch
        {
            Type a when a == typeof(GenericTimer) => "generic",
            Type b when b == typeof(PeriodicTimer) => "periodic",
            Type c when c == typeof(CronTimer) => "cron",
            _ => ThrowHelper.ThrowArgumentOutOfRangeException<string>(nameof(timerType))
        };
    }
}
