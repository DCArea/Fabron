namespace FabronService.Controller.Routes;

public record PaginatedList<T>(int Count, List<T> Items);
