namespace Mcp.TaskAndResearch.Services;

/// &lt;summary&gt;
/// Service for playing audio notifications to alert the user.
/// Controlled by the ENABLE_COMPLETION_BEEP environment variable.
/// &lt;/summary&gt;
public sealed class AudioNotificationService
{
    private const string EnableBeepKey = "ENABLE_COMPLETION_BEEP";
    private const int DefaultFrequency = 2500;
    private const int DefaultDuration = 500;

    /// &lt;summary&gt;
    /// Gets whether audio notifications are enabled via environment variable.
    /// &lt;/summary&gt;
    public bool IsEnabled { get; }

    public AudioNotificationService()
    {
        var setting = Environment.GetEnvironmentVariable(EnableBeepKey);
        IsEnabled = string.Equals(setting, "true", StringComparison.OrdinalIgnoreCase);
    }

    /// &lt;summary&gt;
    /// Plays a completion beep if notifications are enabled.
    /// &lt;/summary&gt;
    /// &lt;returns&gt;True if beep was played, false if disabled.&lt;/returns&gt;
    public bool PlayCompletionBeep()
    {
        return PlayBeep(DefaultFrequency, DefaultDuration);
    }

    /// &lt;summary&gt;
    /// Plays a beep with custom frequency and duration if notifications are enabled.
    /// &lt;/summary&gt;
    /// &lt;param name="frequency"&gt;Frequency in Hz (37-32767).&lt;/param&gt;
    /// &lt;param name="duration"&gt;Duration in milliseconds.&lt;/param&gt;
    /// &lt;returns&gt;True if beep was played, false if disabled.&lt;/returns&gt;
    public bool PlayBeep(int frequency, int duration)
    {
        if (!IsEnabled)
        {
            return false;
        }

        if (!OperatingSystem.IsWindows())
        {
            // Console.Beep(frequency, duration) only works on Windows
            return false;
        }

        try
        {
            Console.Beep(frequency, duration);
            return true;
        }
        catch
        {
            // Silently fail if beep cannot be played (e.g., no audio device)
            return false;
        }
    }
}
