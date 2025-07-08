using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace AptabaseSDK.Viewer
{
    public class AptabaseAnalyticsImporter
    {
        private static readonly Regex CsvSplitRegex = new(
            @",(?=(?:[^""]*""[^""]*"")*[^""]*$)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly Type[] _analyticsEventTypes;
        private readonly PropertyInfo[] _aptabaseEventProperties;
        private readonly Dictionary<string, PropertyInfo> _aptabaseEventPropertyNames;

        public AptabaseAnalyticsImporter()
        {
            _analyticsEventTypes = TypeCache.GetTypesDerivedFrom<object>()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .ToArray();

            _aptabaseEventProperties = typeof(AptabaseAnalyticsEvent).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<JsonPropertyAttribute>() != null)
                .ToArray();

            _aptabaseEventPropertyNames = _aptabaseEventProperties
                .ToDictionary(p => p.GetCustomAttribute<JsonPropertyAttribute>().PropertyName, p => p);
        }

        public async Task<string[]> ImportFrom(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("Path is null or empty");
                return Array.Empty<string>();
            }

            using var actionTimer = new ActionTimer(nameof(ImportFrom));

            try
            {
                // TODO: allow cancelling in between reading lines
                var contents = await File.ReadAllLinesAsync(path);
                if (contents != null && !contents.All(string.IsNullOrWhiteSpace))
                {
                    Debug.Log($"Successfully read {contents.Length} lines from CSV file at \"{path}\"");
                    return contents;
                }

                Debug.LogWarning($"CSV file at \"{path}\" is empty");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to read CSV file at \"{path}\"");
                Debug.LogException(ex);
            }

            return null;
        }

        public void ParseFile(string[] contents, int maxLinesToRead, out AptabaseParsedAnalyticsEvent[] analyticsEvents)
        {
            if (contents == null || contents.Length == 0)
                throw new ArgumentNullException(nameof(contents), "Contents cannot be null or empty.");

            using var actionTimer = new ActionTimer(nameof(ParseFile));
            analyticsEvents = Array.Empty<AptabaseParsedAnalyticsEvent>();

            try
            {
                var rawEvents = ParseAnalyticsEvents(contents, maxLinesToRead).ToArray();
                if (rawEvents.Length == 0)
                {
                    Debug.LogWarning("No analytics events found in the file.");
                    return;
                }

                var events = MatchTypes(rawEvents);
                if (events.Count == 0)
                {
                    Debug.LogWarning("No matching analytics events found.");
                    return;
                }

                if (rawEvents.Length != events.Count)
                    Debug.LogWarning($"Parsed {rawEvents.Length} raw events, " +
                                     $"but matched {events.Count} analytics events ({rawEvents.Length - events.Count} unmatched). " +
                                     "This may be caused by outdated analytics events data, which will be skipped.");
                else
                    Debug.Log($"Parsed {rawEvents.Length} raw events and {events.Count} analytics events from CSV.");

                analyticsEvents = events.ToArray();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private IEnumerable<AptabaseAnalyticsEvent> ParseAnalyticsEvents(string[] contents, int maxLines)
        {
            var headersLine = contents.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(headersLine))
            {
                Debug.LogWarning("CSV file does not contain headers or is empty.");
                yield break;
            }

            var headers = new List<string>();
            if (!TryParseLine(headersLine, headers))
                yield break;

            var max = maxLines > 0
                ? Mathf.Min(maxLines + 1, contents.Length)
                : contents.Length;

            var parts = new List<string>();
            for (var i = 1; i < max; i++)
            {
                var line = contents[i];
                if (string.IsNullOrEmpty(line))
                {
                    Debug.LogWarning($"Line at {i} is null or empty, skipping.");
                    continue;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Parsing Analytics Events",
                        $"Parsing line: {line}",
                        (float)i / max))
                    yield break;

                if (!TryParseLine(line, parts))
                    continue;

                var evt = new AptabaseAnalyticsEvent();
                for (var j = 0; j < parts.Count; j++)
                {
                    if (!_aptabaseEventPropertyNames.TryGetValue(headers[j], out var property))
                    {
                        Debug.LogWarning($"No property found for key '{headers[j]}' in {nameof(AptabaseAnalyticsEvent)}, \npart: {parts[j]}.");
                        continue;
                    }

                    property.SetValue(evt, parts[j]);
                }

                yield return evt;
            }
        }

        private bool TryParseLine(string line, List<string> parts)
        {
            try
            {
                parts.Clear();
                parts.AddRange(
                    CsvSplitRegex.Split(line.Trim())
                        .Select(l => l.Replace(@"""""", @""""))
                );

                if (parts.Count == _aptabaseEventProperties.Length)
                    return true;

                Debug.LogWarning(
                    $"Line '{line}' does not have the right amount of parts ({parts.Count} " +
                    $"when it should be {_aptabaseEventProperties.Length}) to parse as an {nameof(AptabaseAnalyticsEvent)}, " +
                    $"\nparts: {string.Join(", ", parts)}, \nline: {line}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse line '{line}'");
                Debug.LogException(ex);
            }

            return false;
        }

        private List<AptabaseParsedAnalyticsEvent> MatchTypes(AptabaseAnalyticsEvent[] events)
        {
            var output = new List<AptabaseParsedAnalyticsEvent>(events.Length);

            for (var i = 0; i < events.Length; i++)
            {
                var evt = events[i];
                if (evt == null)
                    continue;

                if (EditorUtility.DisplayCancelableProgressBar("Parsing Analytics Event",
                        $"Type matching event: {evt.EventName}",
                        (float)i / events.Length))
                    break;

                var targetType = _analyticsEventTypes.FirstOrDefault(t => t.Name.Contains(evt.EventName, StringComparison.OrdinalIgnoreCase));
                if (targetType == null)
                {
                    Debug.LogWarning($"No matching type found for event '{evt.EventName}'");
                    continue;
                }

                if (TryParse(targetType, evt, out var data))
                {
                    output.Add(new()
                    {
                        RawEvent = evt,
                        ParsedEvent = data
                    });
                }
            }

            return output;
        }

        private static bool TryParse(Type type, AptabaseAnalyticsEvent evt, out object data)
        {
            data = null;

            try
            {
                data = AptabaseAnalyticsEvent.ParseEventProperties(type, evt);
                return data != null;
            }
            catch (JsonException ex)
            {
                Debug.LogError($"Failed to parse event '{evt.EventName}' with type '{type.Name}', eventProperties: {evt.EventProperties}");
                Debug.LogException(ex);
            }

            return false;
        }
    }
}