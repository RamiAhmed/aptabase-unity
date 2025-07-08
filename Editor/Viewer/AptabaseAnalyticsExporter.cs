using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AptabaseSDK.Viewer
{
    public class AptabaseAnalyticsExporter
    {
        private readonly Dictionary<Type, PropertyInfo[]> _analyticsEventProperties;
        private readonly Dictionary<Type, AptabaseParsedAnalyticsEvent[]> _analyticsEventsMap;
        private readonly Dictionary<string, object> _cachedEventData;

        public AptabaseAnalyticsExporter(
            IList<AptabaseParsedAnalyticsEvent> analyticsEvents,
            Dictionary<Type, PropertyInfo[]> analyticsEventProperties)
            : this(
                analyticsEvents
                    .GroupBy(e => e.ParsedEvent.GetType())
                    .ToDictionary(g => g.Key, g => g.ToArray()),
                analyticsEventProperties)
        {
        }

        public AptabaseAnalyticsExporter(
            Dictionary<Type, AptabaseParsedAnalyticsEvent[]> analyticsEventsMap,
            Dictionary<Type, PropertyInfo[]> analyticsEventProperties)
        {
            _analyticsEventsMap = analyticsEventsMap;
            _analyticsEventProperties = analyticsEventProperties;
            _cachedEventData = new();
        }

        public async Task ExportToCsv(Type selectedType, string exportPath, CancellationToken cancellationToken = default)
        {
            if (!_analyticsEventsMap.TryGetValue(selectedType, out var events))
            {
                Debug.LogWarning($"No events found for type {selectedType.Name}");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Export Analytics Events to CSV",
                exportPath,
                $"{selectedType.Name}-{DateTime.Now:d}.csv",
                "csv");

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Export cancelled or path is empty");
                return;
            }

            using var actionTimer = new ActionTimer(nameof(ExportToCsv));

            try
            {
                var builder = new StringBuilder();

                var headers = _analyticsEventProperties[selectedType]
                    .Select(p => p.Name)
                    .OrderBy(n => n)
                    .Concat(AptabaseParsedAnalyticsEvent.AptabaseHeaders);

                builder.AppendLine(string.Join(",", headers));

                for (var i = 0; i < events.Length; i++)
                {
                    var evt = events[i];
                    if (EditorUtility.DisplayCancelableProgressBar("Exporting Analytics Events",
                            $"Exporting event: {evt}",
                            (float)i / events.Length)
                        || cancellationToken.IsCancellationRequested)
                    {
                        Debug.LogWarning("Export cancelled by user");
                        return;
                    }

                    _cachedEventData.Clear();
                    evt.Populate(_cachedEventData);

                    foreach (var (_, value) in _cachedEventData)
                    {
                        builder.Append(value is string str ? $"\"{str}\"" : value?.ToString() ?? "");
                        builder.Append(",");
                    }

                    builder.AppendLine();
                }

                await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);

                Debug.Log($"{events.Length} analytics events exported to \"{path}\"");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to export analytics events: {ex.Message}");
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}