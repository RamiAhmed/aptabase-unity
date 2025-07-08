using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AptabaseSDK.Viewer
{
    public sealed class AnalyticsEditorWindow : EditorWindow
    {
        private const string Title = "Analytics Window";

        [SerializeField]
        private Vector2 _scrollPosition;

        [SerializeField]
        private string _lastPath;

        [SerializeField]
        private bool _askForConfirmation = true; // Toggle to ask for confirmation before parsing file contents

        [SerializeField]
        private int _maxLines; // Maximum lines to read from the CSV file

        [SerializeField]
        private string[] _lastFileContents;

        [SerializeField]
        private bool _drawHeatmap = true; // Toggle to draw heatmap in the scene view
        [SerializeField]
        private bool _drawPositions = true; // Toggle to draw positions in the scene view

        [SerializeField]
        private Vector2 _cellSize = new(10f, 10f); // Size of each cell in the heatmap

        [SerializeField]
        private bool _showAnalyticsEvents = true; // Toggle to show analytics events in the UI

        [SerializeField]
        private int _selectedExportEventIndex;

        [SerializeField]
        private string _analyticsEventNameFilter = string.Empty;

        [SerializeField]
        private bool _showHeatmapTypes;

        private readonly Dictionary<string, object> _cachedEventData = new();
        private readonly Dictionary<Type, bool> _eventToggles = new();
        private readonly Dictionary<string, Vector2[]> _heatmapTypesMap = new();
        private readonly AptabaseHeatmapBuilder _heatmapBuilder = new();
        private readonly AptabaseAnalyticsImporter _importer = new();
        private readonly Dictionary<Type, bool[]> _selectedHeatmapTypes = new();

        private Dictionary<Type, PropertyInfo[]> _analyticsEventPositionProperties;
        private Dictionary<Type, PropertyInfo[]> _analyticsEventProperties;
        private object[] _analyticsEvents;
        private Dictionary<Type, AptabaseParsedAnalyticsEvent[]> _analyticsEventsMap;
        private AptabaseAnalyticsExporter _aptabaseAnalyticsExporter;
        private AptabaseParsedAnalyticsEvent[] _parsedAnalyticsEvents;

        private void OnEnable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            _heatmapBuilder?.Clear();
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            using var scrollScope = new EditorGUILayout.ScrollViewScope(_scrollPosition, EditorStyles.helpBox);
            _scrollPosition = scrollScope.scrollPosition;

            EditorGUILayout.LabelField(Title, EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Separator();

            _askForConfirmation = EditorGUILayout.Toggle("Ask for Confirmation", _askForConfirmation);
            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawImportAndParse();
            }

            if (_analyticsEvents == null)
            {
                if (_lastFileContents is { Length: <= 1 })
                    EditorGUILayout.HelpBox("No analytics events parsed yet. Please parse a CSV file first.", MessageType.Info);

                return;
            }

            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawHeatmapGUI();
            }

            EditorGUILayout.Separator();

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawAnalyticsEvents();
            }

            EditorGUILayout.Separator();
        }

        private void OnSceneGUI(SceneView _)
        {
            if (_drawHeatmap)
                _heatmapBuilder?.DrawHeatmap();

            if (_drawPositions)
                _heatmapBuilder?.DrawPositions();
        }

        private void DrawImportAndParse()
        {
            EditorGUILayout.LabelField("Import & Parse", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            _maxLines = EditorGUILayout.IntField("Max Lines to Read (0=all)", _maxLines);

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField("File Path", _lastPath ?? "No path set");
            EditorGUILayout.LabelField(
                "File Lines Read",
                _lastFileContents is { Length: > 1 }
                    ? $"{_lastFileContents.Length.ToString()} lines"
                    : "No contents read");
            EditorGUILayout.LabelField(
                "Parsed Analytics Events",
                _parsedAnalyticsEvents is { Length: > 0 }
                    ? $"{_parsedAnalyticsEvents.Length.ToString()} events"
                    : "No events parsed");

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import from CSV"))
                {
                    var path = EditorUtility.OpenFilePanel("Select CSV File", _lastPath, "csv");
                    if (string.IsNullOrEmpty(path))
                    {
                        Debug.LogWarning("No CSV file selected");
                        return;
                    }

                    _ = OpenFromFile(path);
                }

                if (!string.IsNullOrWhiteSpace(_lastPath))
                {
                    if (GUILayout.Button("Import from Last Path"))
                        _ = OpenFromFile(_lastPath);
                }
            }

            if (_lastFileContents is { Length: > 1 })
            {
                if (GUILayout.Button("Parse File Contents"))
                    ParseFileContents();
            }
            else
            {
                EditorGUILayout.HelpBox("No file contents to parse. Please import a CSV file first.", MessageType.Info);
            }
        }

        private void DrawHeatmapGUI()
        {
            EditorGUILayout.LabelField("Heatmap", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            _drawHeatmap = EditorGUILayout.Toggle("Draw Heatmap", _drawHeatmap);
            _drawPositions = EditorGUILayout.Toggle("Draw Positions", _drawPositions);
            _cellSize = EditorGUILayout.Vector2Field("Heatmap Cell Size", _cellSize);

            EditorGUILayout.Separator();

            _showHeatmapTypes = EditorGUILayout.Foldout(_showHeatmapTypes, $"Select Heatmap Types ({_analyticsEventPositionProperties.Count.ToString()})");

            using var indent1 = new EditorGUI.IndentLevelScope();

            if (_showHeatmapTypes)
            {
                foreach (var (type, positionProperties) in _analyticsEventPositionProperties)
                {
                    if (!_selectedHeatmapTypes.TryGetValue(type, out var selections))
                    {
                        selections = new bool[positionProperties.Length];
                        _selectedHeatmapTypes[type] = selections;
                    }

                    EditorGUILayout.LabelField(type.Name);

                    using var indent2 = new EditorGUI.IndentLevelScope();

                    for (var i = 0; i < positionProperties.Length; i++)
                    {
                        selections[i] = EditorGUILayout.Toggle(positionProperties[i].Name, selections[i]);
                    }

                    EditorGUILayout.Separator();
                }
            }

            EditorGUILayout.Separator();

            if (!_selectedHeatmapTypes.Any(s => s.Value.Any(x => x)))
            {
                EditorGUILayout.HelpBox("No heatmap types selected. Please select at least one type to generate a heatmap.", MessageType.Info);
                return;
            }

            if (GUILayout.Button("Generate Heatmap"))
                GenerateHeatmap();
        }

        private void DrawAnalyticsEvents()
        {
            EditorGUILayout.LabelField("Parsed Analytics Events", EditorStyles.boldLabel);
            EditorGUILayout.Separator();

            _selectedExportEventIndex = EditorGUILayout.Popup(
                "Select Event Type",
                _selectedExportEventIndex,
                _analyticsEventsMap.Keys.Select(t => t.Name).ToArray());

            if (_selectedExportEventIndex >= 0 && _selectedExportEventIndex < _analyticsEventsMap.Count)
            {
                var selectedType = _analyticsEventsMap.Keys.ElementAt(_selectedExportEventIndex);

                if (selectedType != null && GUILayout.Button($"Export \"{selectedType.Name}\" to CSV"))
                    _ = _aptabaseAnalyticsExporter.ExportToCsv(selectedType, _lastPath);

                EditorGUILayout.Separator();
            }

            _showAnalyticsEvents = EditorGUILayout.Foldout(_showAnalyticsEvents, $"Show Analytics Events ({_analyticsEvents.Length.ToString()})");
            if (!_showAnalyticsEvents)
                return;

            using var indent1 = new EditorGUI.IndentLevelScope();

            _analyticsEventNameFilter = EditorGUILayout.TextField("Event Name Filter", _analyticsEventNameFilter, EditorStyles.toolbarSearchField);

            EditorGUILayout.Separator();

            var filteredEvents = string.IsNullOrEmpty(_analyticsEventNameFilter)
                ? _analyticsEventsMap
                : _analyticsEventsMap.Where(kvp => kvp.Key.Name.Contains(_analyticsEventNameFilter, StringComparison.OrdinalIgnoreCase));

            if (!filteredEvents.Any())
            {
                EditorGUILayout.HelpBox("No analytics events found matching the filter.", MessageType.Info);
                return;
            }

            foreach (var (type, events) in filteredEvents)
            {
                if (!_eventToggles.TryGetValue(type, out var show))
                    _eventToggles.Add(type, false);

                _eventToggles[type] = EditorGUILayout.Foldout(show, $"{type.Name} ({events.Length.ToString()})");

                if (!show)
                    continue;

                using var indent2 = new EditorGUI.IndentLevelScope();

                foreach (var evt in events)
                {
                    using var verticalScope = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);
                    EditorGUILayout.Separator();

                    _cachedEventData.Clear();
                    evt.Populate(_cachedEventData);

                    foreach (var prop in _cachedEventData)
                    {
                        EditorGUILayout.LabelField(prop.Key, prop.Value.ToString());
                    }

                    EditorGUILayout.Separator();
                }

                EditorGUILayout.Separator();
            }
        }

        private void ParseFileContents()
        {
            if (!DisplayDialog("Parsing Analytics File",
                    $"Are you sure you want to parse the file contents? This will read and process up to {(_maxLines > 0 ? _maxLines : _lastFileContents.Length):N0} lines.",
                    "OK",
                    "Cancel"))
            {
                return;
            }

            try
            {
                _importer.ParseFile(_lastFileContents, _maxLines, out _parsedAnalyticsEvents);

                _analyticsEvents = _parsedAnalyticsEvents
                    .Select(p => p.ParsedEvent)
                    .ToArray();

                _analyticsEventsMap = _parsedAnalyticsEvents
                    .GroupBy(a => a.GetType(), a => a)
                    .OrderBy(e => e.Key.Name)
                    .ToDictionary(a => a.Key, a => a.ToArray());

                _analyticsEventProperties = new(_analyticsEvents.Length);
                foreach (var (type, _) in _analyticsEventsMap)
                {
                    var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.CanRead)
                        //.Where(p => p.Name != nameof(IAnalyticsEvent.Name))
                        .ToArray();

                    _analyticsEventProperties[type] = properties;
                }

                _analyticsEventPositionProperties = new(_analyticsEventsMap.Count);
                foreach (var (type, _) in _analyticsEventsMap)
                {
                    var positionProperties = _analyticsEventProperties[type]
                        .Where(p => p.PropertyType == typeof(Vector2) || p.PropertyType == typeof(Vector3))
                        .Where(p => p.Name.Contains("Position", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (positionProperties.Length > 0)
                        _analyticsEventPositionProperties[type] = positionProperties;
                }

                _aptabaseAnalyticsExporter = new(_parsedAnalyticsEvents, _analyticsEventProperties);
                _heatmapBuilder.Clear();
                _selectedHeatmapTypes.Clear();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to parse file contents: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        private void GenerateHeatmap()
        {
            if (!DisplayDialog("Generating Heatmap",
                    $"Are you sure you want to generate a heatmap for {_selectedHeatmapTypes.Count} types? This may take some time.",
                    "OK",
                    "Cancel"))
            {
                return;
            }

            _heatmapTypesMap.Clear();

            foreach (var (type, selections) in _selectedHeatmapTypes)
            {
                if (!_analyticsEventPositionProperties.TryGetValue(type, out var positionProperties))
                {
                    Debug.LogWarning($"No position properties found for type {type.Name}");
                    continue;
                }

                if (!_analyticsEventsMap.TryGetValue(type, out var events))
                {
                    Debug.LogWarning($"No events found for type {type.Name}");
                    continue;
                }

                for (var i = 0; i < selections.Length; i++)
                {
                    if (!selections[i])
                        continue;

                    if (i >= positionProperties.Length)
                    {
                        Debug.LogWarning($"Invalid position property index {i} for type {type.Name}");
                        continue;
                    }

                    var positionProperty = positionProperties[i];
                    var positionData = events
                        .Select(positionProperty.GetValue)
                        .Select(v => v is Vector3 vec ? new Vector2(vec.x, vec.z) : v)
                        .OfType<Vector2>()
                        .ToArray();

                    if (positionData.Length == 0)
                    {
                        Debug.LogWarning($"No position data found for \"{positionProperty.Name}\" in \"{type.Name}\"");
                        continue;
                    }

                    _heatmapTypesMap[$"{type.Name}.{positionProperty.Name}"] = positionData;
                }
            }

            if (_heatmapTypesMap.Count == 0)
            {
                Debug.LogWarning("No heatmap types selected or found.");
                return;
            }

            _heatmapBuilder.GenerateHeatmap(_cellSize, _heatmapTypesMap);
        }

        private async Task OpenFromFile(string path = null)
        {
            if (!string.IsNullOrEmpty(path))
                _lastPath = path;

            try
            {
                if (!DisplayDialog("Importing Analytics",
                        $"Are you sure you want to import analytics data from \"{_lastPath}\"?. This may take a while depending on the file size.",
                        "OK",
                        "Cancel"))
                {
                    return;
                }

                _lastFileContents = await _importer.ImportFrom(_lastPath);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to import analytics from {_lastPath}: {ex.Message}");
                Debug.LogException(ex);
            }
        }

        [MenuItem("Tools/Aptabase/" + Title, priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<AnalyticsEditorWindow>(Title);
            window.Show();
        }

        private bool DisplayDialog(string title, string message, string ok, string cancel)
            => !_askForConfirmation || EditorUtility.DisplayDialog(title, message, ok, cancel);
    }
}