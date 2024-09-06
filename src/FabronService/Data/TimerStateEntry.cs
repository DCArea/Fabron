namespace FabronService.Data;

public record TimerStateEntry<TState>(string Key, TState Data, string ETag);
