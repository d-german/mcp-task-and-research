namespace Mcp.TaskAndResearch.Services;

/// &lt;summary&gt;
/// Service for playing audio notifications to alert the user.
/// Controlled by the ENABLE_COMPLETION_BEEP environment variable.
/// &lt;/summary&gt;
public sealed class AudioNotificationService
{
    private const string EnableBeepKey = "ENABLE_COMPLETION_BEEP";
    private const string FrequencyKey = "BEEP_FREQUENCY";
    private const string DurationKey = "BEEP_DURATION";
    private const int DefaultFrequency = 2500;
    private const int DefaultDuration = 1000;

    /// <summary>
    /// Gets whether audio notifications are enabled via environment variable.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the configured beep frequency in Hz.
    /// </summary>
    public int Frequency { get; }

    /// <summary>
    /// Gets the configured beep duration in milliseconds.
    /// </summary>
    public int Duration { get; }

    public AudioNotificationService()
    {
        var setting = Environment.GetEnvironmentVariable(EnableBeepKey);
        IsEnabled = string.Equals(setting, "true", StringComparison.OrdinalIgnoreCase);

        Frequency = ParseIntEnvironmentVariable(FrequencyKey, DefaultFrequency, 37, 32767);
        Duration = ParseIntEnvironmentVariable(DurationKey, DefaultDuration, 100, 5000);
    }

    /// <summary>
    /// Plays a completion beep if notifications are enabled.
    /// </summary>
    /// <returns>True if beep was played, false if disabled.</returns>
    public bool PlayCompletionBeep()
    {
        return PlayBeep(Frequency, Duration);
    }

    /// <summary>
    /// Plays a beep with custom frequency and duration if notifications are enabled.
    /// </summary>
    /// <param name="frequency">Frequency in Hz (37-32767).</param>
    /// <param name="duration">Duration in milliseconds.</param>
    /// <returns>True if beep was played, false if disabled.</returns>
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

    private static int ParseIntEnvironmentVariable(string key, int defaultValue, int min, int max)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var parsed) && parsed >= min && parsed <= max)
        {
            return parsed;
        }

        return defaultValue;
    }
}
