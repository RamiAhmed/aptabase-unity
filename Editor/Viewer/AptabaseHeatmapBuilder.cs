using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AptabaseSDK.Viewer
{
    public class AptabaseHeatmapBuilder
    {
        private static readonly Color[] MapColors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            new(1f, 0.5f, 0f) // Orange
        };

        private static readonly Color[] PositionColors =
        {
            Color.green,
            Color.red,
            Color.yellow,
            Color.blue,
            Color.cyan,
            Color.magenta,
            Color.black
        };

        private static readonly Color BaseColor = new(1f, 1f, 1f, 0.01f);

        private readonly Vector3[] _vertices = new Vector3[4];

        private float _bufferSize;
        private Vector2 _cellSize;
        private Dictionary<string, float[,]> _heatmapData;
        private Dictionary<string, HeatmapData> _heatmapMetadata;
        private Dictionary<string, Vector2[]> _positionsMap;

        public void Clear()
        {
            _heatmapMetadata = null;
            _positionsMap = null;
            _heatmapData = null;
        }

        public void GenerateHeatmap(Vector2 cellSize, Dictionary<string, Vector2[]> positions, float bufferSize = 1.1f)
        {
            if (cellSize.x <= 0f || cellSize.y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize,
                    "Cell size must be positive and non-zero.");

            if (positions == null || positions.Count == 0)
                throw new ArgumentNullException(nameof(positions), "Positions cannot be null or empty.");

            if (bufferSize <= 1f)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, "Buffer size must be above 1");

            _cellSize = cellSize;
            _positionsMap = positions;
            _bufferSize = bufferSize;

            using var actionTimer = new ActionTimer(nameof(GenerateHeatmap));

            PopulateHeatmap();

            Debug.Log(
                $"Generated ({_heatmapMetadata.Count}) heatmap(s) -- buffer size: {_bufferSize}, cell size: {_cellSize}, from position lists: {_positionsMap.Count}");
        }

        public void DrawHeatmap()
        {
            if (_heatmapData == null)
                return;

            var index = 0;
            foreach (var (name, coords) in _heatmapData)
            {
                if (!_heatmapMetadata.TryGetValue(name, out var metadata))
                {
                    Debug.LogWarning($"Heatmap metadata for '{name}' not found.");
                    continue;
                }

                var max = metadata.MaxCoords;
                if (max.x <= 0 || max.y <= 0)
                {
                    Debug.LogWarning($"Invalid heatmap dimensions for '{name}': {max}");
                    continue;
                }

                var offset = metadata.Offsets;
                var targetColor = metadata.Color;

                var height = ++index;

                for (var x = 0; x < max.x; x++)
                for (var y = 0; y < max.y; y++)
                {
                    var value = coords[x, y];
                    var position = new Vector2(
                        x * _cellSize.x + offset.x,
                        y * _cellSize.y + offset.y);

                    var color = Color.Lerp(BaseColor, targetColor, Mathf.Clamp01(value));

                    _vertices[0] = new(position.x, height, position.y);
                    _vertices[1] = new(position.x + _cellSize.x, height, position.y);
                    _vertices[2] = new(position.x + _cellSize.x, height, position.y + _cellSize.y);
                    _vertices[3] = new(position.x, height, position.y + _cellSize.y);

                    Handles.DrawSolidRectangleWithOutline(_vertices, color, targetColor);
                }

                var handleStyle = new GUIStyle
                {
                    normal =
                    {
                        textColor = targetColor
                    }
                };

                Handles.Label(_vertices[index % 4], name, handleStyle);
            }
        }

        public void DrawPositions()
        {
            if (_positionsMap == null || _positionsMap.Count == 0)
                return;

            var index = 0;
            foreach (var (_, positions) in _positionsMap)
            {
                var color = PositionColors[index++ % PositionColors.Length];
                Handles.color = color;

                foreach (var position in positions)
                {
                    var pos = position;
                    pos.y = index;
                    Handles.DrawWireDisc(pos, Vector3.up, 0.1f);
                }
            }
        }

        private void PopulateHeatmap()
        {
            _heatmapData = new(_positionsMap.Count);
            _heatmapMetadata = new(_positionsMap.Count);

            var index = 0;
            foreach (var (name, positions) in _positionsMap)
            {
                // Calculate offsets and dimensions
                float minPosX = float.MaxValue,
                    minPosY = float.MaxValue,
                    maxPosX = float.MinValue,
                    maxPosY = float.MinValue;

                foreach (var position in positions)
                {
                    minPosX = Mathf.Min(minPosX, position.x);
                    minPosY = Mathf.Min(minPosY, position.y);
                    maxPosX = Mathf.Max(maxPosX, position.x);
                    maxPosY = Mathf.Max(maxPosY, position.y);
                }

                // Ceil/Floor to nearest region size multiple
                minPosX = Mathf.Floor(minPosX / _cellSize.x) * _cellSize.x;
                minPosY = Mathf.Floor(minPosY / _cellSize.y) * _cellSize.y;
                maxPosX = Mathf.Ceil(maxPosX / _cellSize.x) * _cellSize.x;
                maxPosY = Mathf.Ceil(maxPosY / _cellSize.y) * _cellSize.y;

                var width = (maxPosX - minPosX) * _bufferSize + Mathf.Epsilon;
                var height = (maxPosY - minPosY) * _bufferSize + Mathf.Epsilon;

                var offsetX = minPosX - (width - (maxPosX - minPosX)) * 0.5f;
                var offsetY = minPosY - (height - (maxPosY - minPosY)) * 0.5f;

                var maxX = (int)Mathf.Ceil(width / _cellSize.x);
                var maxY = (int)Mathf.Ceil(height / _cellSize.y);

                _heatmapMetadata[name] = new()
                {
                    Color = MapColors[index++ % MapColors.Length],
                    MaxCoords = new(maxX, maxY),
                    Offsets = new(offsetX, offsetY)
                };

                // Populate heatmap data array
                _heatmapData[name] = new float[maxX, maxY];
                foreach (var position in positions)
                {
                    var xIndex = (int)((position.x - offsetX) / _cellSize.x);
                    var yIndex = (int)((position.y - offsetY) / _cellSize.y);
                    _heatmapData[name][xIndex, yIndex]++;
                }

                // Normalize heatmap data
                var maxCount = _heatmapData[name].Cast<float>().Max();
                if (maxCount > 0f)
                {
                    for (var x = 0; x < maxX; x++)
                    for (var y = 0; y < maxY; y++)
                    {
                        _heatmapData[name][x, y] /= maxCount;
                    }
                }
            }
        }

        private class HeatmapData
        {
            public Color Color;
            public Vector2 MaxCoords;
            public Vector2 Offsets;
        }
    }
}