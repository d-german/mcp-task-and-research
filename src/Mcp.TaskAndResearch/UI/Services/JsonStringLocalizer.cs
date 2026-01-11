using Microsoft.Extensions.Localization;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Mcp.TaskAndResearch.UI.Services;

/// <summary>
/// Custom JSON-based string localizer for Blazor Server.
/// Loads locale strings from wwwroot/locales/{culture}.json files.
/// </summary>
public sealed class JsonStringLocalizer : IStringLocalizer
{
    private readonly ConcurrentDictionary<string, Dictionary<string, object>> _cache = new();
    private readonly string _basePath;
    private string _currentCulture = "en";

    public JsonStringLocalizer(IWebHostEnvironment environment)
    {
        _basePath = Path.Combine(environment.WebRootPath, "locales");
    }

    public LocalizedString this[string name]
    {
        get
        {
            var value = GetString(name);
            return new LocalizedString(name, value ?? name, value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var value = GetString(name);
            var formatted = value is not null ? string.Format(value, arguments) : name;
            return new LocalizedString(name, formatted, value is null);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var strings = LoadStrings(_currentCulture);
        return FlattenDictionary(strings, "")
            .Select(kv => new LocalizedString(kv.Key, kv.Value, false));
    }

    public void SetCulture(string culture)
    {
        _currentCulture = culture;
    }

    private string? GetString(string key)
    {
        var strings = LoadStrings(_currentCulture);
        return GetNestedValue(strings, key);
    }

    private Dictionary<string, object> LoadStrings(string culture)
    {
        return _cache.GetOrAdd(culture, c =>
        {
            var filePath = Path.Combine(_basePath, $"{c}.json");
            if (!File.Exists(filePath))
            {
                // Fallback to English
                filePath = Path.Combine(_basePath, "en.json");
                if (!File.Exists(filePath))
                {
                    return new Dictionary<string, object>();
                }
            }

            try
            {
                var json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        });
    }

    private static string? GetNestedValue(Dictionary<string, object> dict, string key)
    {
        var parts = key.Split('.');
        object? current = dict;

        foreach (var part in parts)
        {
            if (current is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(part, out var prop))
                {
                    current = prop;
                }
                else
                {
                    return null;
                }
            }
            else if (current is Dictionary<string, object> d && d.TryGetValue(part, out var value))
            {
                current = value;
            }
            else
            {
                return null;
            }
        }

        return current switch
        {
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString(),
            string s => s,
            _ => current?.ToString()
        };
    }

    private static IEnumerable<KeyValuePair<string, string>> FlattenDictionary(
        Dictionary<string, object> dict, string prefix)
    {
        foreach (var kv in dict)
        {
            var key = string.IsNullOrEmpty(prefix) ? kv.Key : $"{prefix}.{kv.Key}";

            if (kv.Value is JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Object)
                {
                    var nested = element.Deserialize<Dictionary<string, object>>() ?? new();
                    foreach (var nested_kv in FlattenDictionary(nested, key))
                    {
                        yield return nested_kv;
                    }
                }
                else if (element.ValueKind == JsonValueKind.String)
                {
                    yield return new KeyValuePair<string, string>(key, element.GetString() ?? "");
                }
            }
            else if (kv.Value is Dictionary<string, object> nested)
            {
                foreach (var nested_kv in FlattenDictionary(nested, key))
                {
                    yield return nested_kv;
                }
            }
            else
            {
                yield return new KeyValuePair<string, string>(key, kv.Value?.ToString() ?? "");
            }
        }
    }
}

/// <summary>
/// Generic string localizer wrapper for type-safe injection.
/// </summary>
/// <typeparam name="T">The type to associate with this localizer.</typeparam>
public sealed class JsonStringLocalizer<T> : IStringLocalizer<T>
{
    private readonly JsonStringLocalizer _localizer;

    public JsonStringLocalizer(JsonStringLocalizer localizer)
    {
        _localizer = localizer;
    }

    public LocalizedString this[string name] => _localizer[name];
    public LocalizedString this[string name, params object[] arguments] => _localizer[name, arguments];

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        _localizer.GetAllStrings(includeParentCultures);
}
