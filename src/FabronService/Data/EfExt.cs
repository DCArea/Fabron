using FabronService.Controller.Routes;
using Microsoft.EntityFrameworkCore;

namespace FabronService.Data;

public static class EfExt
{
    public static async Task<PaginatedList<T>> CountAndPagingAsync<T>(this IQueryable<T> source, int skip, int take, CancellationToken token = default)
    {
        var count = await source.CountAsync(token);
        var items = await source.Skip(skip).Take(take).ToListAsync(token);
        return new PaginatedList<T>(count, items);
    }
}
