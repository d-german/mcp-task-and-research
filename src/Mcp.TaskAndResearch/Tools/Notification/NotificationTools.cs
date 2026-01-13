using System.ComponentModel;
using Mcp.TaskAndResearch.Services;
using ModelContextProtocol.Server;

namespace Mcp.TaskAndResearch.Tools.Notification;

/// &lt;summary&gt;
/// MCP tools for audio notifications.
/// &lt;/summary&gt;
public static class NotificationTools
{
    [McpServerTool(Name = "play_beep")]
    [Description("Play an audible beep to notify the user. Call this when you've completed your work and are waiting for user input. Requires ENABLE_COMPLETION_BEEP=true environment variable.")]
    public static string PlayBeep(AudioNotificationService notificationService)
    {
        if (!notificationService.IsEnabled)
        {
            return "Beep notification is disabled. Set ENABLE_COMPLETION_BEEP=true to enable.";
        }

        var played = notificationService.PlayCompletionBeep();

        return played
            ? "✓ Beep played successfully."
            : "Beep could not be played (not supported on this platform or no audio device).";
    }
}
