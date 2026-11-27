using Microsoft.JSInterop;

namespace ClassMate.Services;
public sealed class UserClock(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? module;
    private TimeZoneInfo zone = TimeZoneInfo.Utc;

    public bool IsInitialized { get; private set; }

    public TimeZoneInfo Zone => zone;
    public DateTime Today => LocalTime(DateTimeOffset.UtcNow, zone).Date;
    public static DateTime LocalTime(DateTimeOffset instant, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(instant, zone).DateTime;

    /// Reads the browser's time zone. Safe to call more than once
    public async Task InitializeAsync()
    {
        if (IsInitialized)
            return;

        try
        {
            module = await js.InvokeAsync<IJSObjectReference>("import", "./js/userClock.js");
            var id = await module.InvokeAsync<string?>("getTimeZone");

            if (!string.IsNullOrWhiteSpace(id))
                zone = TimeZoneInfo.FindSystemTimeZoneById(id);

            IsInitialized = true;
        }
        catch (Exception ex) when (ex is JSException
                                      or JSDisconnectedException
                                      or InvalidOperationException
                                      or TimeZoneNotFoundException
                                      or InvalidTimeZoneException)
        {
            // Keep the UTC fallback
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null)
            return;

        try
        {
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; nothing to release
        }
    }
}