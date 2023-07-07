﻿namespace Fabron;

public interface ITimerManager<TTimer>
{
    /// <summary>
    /// Start the ticker of a timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Start(string key);

    /// <summary>
    /// Stop the ticker of a timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Stop(string key);

    /// <summary>
    /// Set extensions for a timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <param name="extensions">Extension fields</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task SetExt(
        string key,
        Dictionary<string, string?> extensions);


    /// <summary>
    /// Delete a timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task Delete(string key);

    /// <summary>
    /// Get a timer
    /// </summary>
    /// <param name="key">Timer key</param>
    /// <returns>The timer, null if not exists</returns>
    public ValueTask<TTimer?> Get(string key);
}
