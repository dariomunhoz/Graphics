﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Formats the provided descriptor into a linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class LightUnitSlider
    {
        static class SliderConfig
        {
            public const float k_IconSeparator      = 6;
            public const float k_MarkerWidth        = 4;
            public const float k_MarkerHeight       = 2;
            public const float k_MarkerTooltipScale = 4;
            public const float k_ThumbTooltipSize   = 10;
        }

        protected readonly LightUnitSliderUIDescriptor m_Descriptor;

        public LightUnitSlider(LightUnitSliderUIDescriptor descriptor)
        {
            m_Descriptor = descriptor;
        }

        public void Draw(Rect rect, SerializedProperty value)
        {
            BuildRects(rect, out var sliderRect, out var iconRect);

            var level = CurrentRange(value.floatValue);

            DoSlider(sliderRect, value, m_Descriptor.sliderRange, level.value);

            if (m_Descriptor.hasMarkers)
            {
                foreach (var r in m_Descriptor.valueRanges)
                {
                    var markerValue = r.value.y;
                    var markerPosition = GetPositionOnSlider(markerValue, r.value);
                    var markerTooltip = r.content.tooltip;
                    DoSliderMarker(sliderRect, markerPosition, markerValue, markerTooltip);
                }
            }

            var levelIconContent = level.content;
            var levelRange = level.value;
            DoIcon(iconRect, levelIconContent, levelRange.y);

            var thumbValue = value.floatValue;
            var thumbPosition = GetPositionOnSlider(thumbValue, level.value);
            var thumbTooltip = levelIconContent.tooltip;
            DoThumbTooltip(sliderRect, thumbPosition, thumbValue, thumbTooltip);
        }

        LightUnitSliderUIRange CurrentRange(float value)
        {
            foreach (var l in m_Descriptor.valueRanges)
            {
                if (value >= l.value.x && value <= l.value.y)
                {
                    return l;
                }
            }

            return LightUnitSliderUIRange.CautionRange(m_Descriptor.cautionTooltip, value);
        }

        void BuildRects(Rect baseRect, out Rect sliderRect, out Rect iconRect)
        {
            sliderRect = baseRect;
            sliderRect.width -= EditorGUIUtility.singleLineHeight + SliderConfig.k_IconSeparator;

            iconRect = baseRect;
            iconRect.x += sliderRect.width + SliderConfig.k_IconSeparator;
            iconRect.width = EditorGUIUtility.singleLineHeight;
        }

        private static Color k_DarkThemeColor = new Color32(196, 196, 196, 255);
        private static Color k_LiteThemeColor = new Color32(85, 85, 85, 255);
        static Color GetMarkerColor() => EditorGUIUtility.isProSkin ? k_DarkThemeColor : k_LiteThemeColor;

        void DoSliderMarker(Rect rect, float position, float value, string tooltip)
        {
            const float width  = SliderConfig.k_MarkerWidth;
            const float height = SliderConfig.k_MarkerHeight;

            var markerRect = rect;
            markerRect.width  = width;
            markerRect.height = height;

            // Vertically align with slider.
            markerRect.y += (EditorGUIUtility.singleLineHeight / 2f) - 1;

            // Horizontally place on slider.
            const float halfWidth = width * 0.5f;
            markerRect.x = rect.x + rect.width * position;

            // Center the marker on value.
            markerRect.x -= halfWidth;

            // Clamp to the slider edges.
            float min = rect.x;
            float max = (rect.x + rect.width) - width;
            markerRect.x = Mathf.Clamp(markerRect.x, min, max);

            // Draw marker by manually drawing the rect, and an empty label with the tooltip.
            EditorGUI.DrawRect(markerRect, GetMarkerColor());

            // Scale the marker tooltip for easier discovery
            const float markerTooltipRectScale = SliderConfig.k_MarkerTooltipScale;
            var markerTooltipRect = markerRect;
            markerTooltipRect.width  *= markerTooltipRectScale;
            markerTooltipRect.height *= markerTooltipRectScale;
            markerTooltipRect.x      -= (markerTooltipRect.width  * 0.5f) - 1;
            markerTooltipRect.y      -= (markerTooltipRect.height * 0.5f) - 1;
            EditorGUI.LabelField(markerTooltipRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        void DoIcon(Rect rect, GUIContent icon, float range)
        {
            var oldColor = GUI.color;
            GUI.color = Color.clear;
            EditorGUI.DrawTextureTransparent(rect, icon.image);
            GUI.color = oldColor;

            EditorGUI.LabelField(rect, GetLightUnitTooltip(icon.tooltip, range, m_Descriptor.unitName));
        }

        void DoThumbTooltip(Rect rect, float position, float value, string tooltip)
        {
            const float size = SliderConfig.k_ThumbTooltipSize;
            const float halfSize = SliderConfig.k_ThumbTooltipSize * 0.5f;

            var thumbMarkerRect = rect;
            thumbMarkerRect.width  = size;
            thumbMarkerRect.height = size;

            // Vertically align with slider
            thumbMarkerRect.y += halfSize - 1f;

            // Horizontally place tooltip on the wheel,
            thumbMarkerRect.x  = rect.x + (rect.width - size) * position;

            EditorGUI.LabelField(thumbMarkerRect, GetLightUnitTooltip(tooltip, value, m_Descriptor.unitName));
        }

        static GUIContent GetLightUnitTooltip(string baseTooltip, float value, string unit)
        {
            string formatValue;

            if (value >= 100000)
                formatValue = (value / 1000).ToString("#,0K");
            else if (value >= 10000)
                formatValue = (value / 1000).ToString("0.#") + "K";
            else
                formatValue = value.ToString("#0.0");

            string tooltip = baseTooltip + " | " + formatValue + " " + unit;

            return new GUIContent(string.Empty, tooltip);
        }

        protected virtual void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange, Vector2 valueRange)
        {
            DoSlider(rect, value, sliderRange);
        }

        /// <summary>
        /// Draws a linear slider mapped to the min/max value range. Override this for different slider behavior (texture background, power).
        /// </summary>
        protected virtual void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, sliderRange.x, sliderRange.y);
        }

        // Remaps value in the domain { Min0, Max0 } to { Min1, Max1 } (by default, normalizes it to (0, 1).
        static float Remap(float v, float x0, float y0, float x1 = 0f, float y1 = 1f) => x1 + (v - x0) * (y1 - x1) / (y0 - x0);

        protected virtual float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            return GetPositionOnSlider(value);
        }

        /// <summary>
        /// Maps a light unit value onto the slider. Keeps in sync placement of markers and tooltips with the slider power.
        /// Override this in case of non-linear slider.
        /// </summary>
        protected virtual float GetPositionOnSlider(float value)
        {
            return Remap(value, m_Descriptor.sliderRange.x, m_Descriptor.sliderRange.y);
        }
    }

    /// <summary>
    /// Formats the provided descriptor into an exponential slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class ExponentialLightUnitSlider : LightUnitSlider
    {
        private Vector3 m_ExponentialConstraints;

        /// <summary>
        /// Exponential slider modeled to set a f(0.5) value.
        /// ref: https://stackoverflow.com/a/17102320
        /// </summary>
        void PrepareExponentialConstraints(float lo, float mi, float hi)
        {
            float x = lo;
            float y = mi;
            float z = hi;

            // https://www.desmos.com/calculator/yx2yf4huia
            m_ExponentialConstraints.x = ((x * z) - (y * y)) / (x - (2 * y) + z);
            m_ExponentialConstraints.y = ((y - x) * (y - x)) / (x - (2 * y) + z);
            m_ExponentialConstraints.z = 2 * Mathf.Log((z - y) / (y - x));
        }

        float ValueToSlider(float x) => Mathf.Log((x - m_ExponentialConstraints.x) / m_ExponentialConstraints.y) / m_ExponentialConstraints.z;
        float SliderToValue(float x) => m_ExponentialConstraints.x + m_ExponentialConstraints.y * Mathf.Exp(m_ExponentialConstraints.z * x);

        public ExponentialLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            var halfValue = 300; // TODO: Compute the median
            PrepareExponentialConstraints(m_Descriptor.sliderRange.x, halfValue, m_Descriptor.sliderRange.y);
        }

        protected override float GetPositionOnSlider(float value)
        {
            return ValueToSlider(value);
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            value.floatValue = ExponentialSlider(rect, value.floatValue);
        }

        float ExponentialSlider(Rect rect, float value)
        {
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(value), 0f, 1f);
            return SliderToValue(internalValue);
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a piece-wise linear slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class PiecewiseLightUnitSlider : LightUnitSlider
    {
        struct Piece
        {
            public Vector2 domain;
            public Vector2 range;
            public Func<float, float> transform;
            public Func<float, float> inverseTransform;
        }

        // Piecewise function indexed by value ranges.
        private readonly Dictionary<int, Piece> m_PiecewiseFunctionMap = new Dictionary<int, Piece>();

        Func<float, float> GetTransformation(float x0, float x1, float y0, float y1)
        {
            var m = (y0 - y1) / (x0 - x1);
            var b = (m * -x0) + y0;

            return x => (m * x) + b;
        }

        float ValueToSlider(Piece piecewise, float x) => piecewise.inverseTransform(x);
        float SliderToValue(Piece piecewise, float x) => piecewise.transform(x);

        // Ideally we want a continuous, monotonically increasing function, but this is useful as we can easily fit a
        // distribution to a set of (huge) value ranges onto a slider.
        public PiecewiseLightUnitSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor)
        {
            // Sort the ranges into ascending order
            var sortedRanges = m_Descriptor.valueRanges.OrderBy(x => x.value.x).ToArray();

            // Compute the transformation for each value range.
            var sliderStep = 1.0f / m_Descriptor.valueRanges.Length;
            for (int i = 0; i < sortedRanges.Length; i++)
            {
                var r = sortedRanges[i].value;

                var x0 = (i + 0) * sliderStep;
                var x1 = (i + 1) * sliderStep;
                var y0 = r.x;
                var y1 = r.y;

                Piece piece;
                piece.domain = new Vector2(x0, x1);
                piece.range  = new Vector2(y0, y1);

                piece.transform = GetTransformation(x0, x1, y0, y1);

                // Compute the inverse
                CoreUtils.Swap(ref x0, ref y0);
                CoreUtils.Swap(ref x1, ref y1);
                piece.inverseTransform = GetTransformation(x0, x1, y0, y1);

                var k = sortedRanges[i].value.GetHashCode();
                m_PiecewiseFunctionMap.Add(k, piece);
            }
        }

        protected override float GetPositionOnSlider(float value, Vector2 valueRange)
        {
            var k = valueRange.GetHashCode();
            if (!m_PiecewiseFunctionMap.TryGetValue(k, out var piecewise))
                return -1f;

            return ValueToSlider(piecewise, value);
        }

        void UpdatePiece(ref Piece piece, float x)
        {
            foreach (var pair in m_PiecewiseFunctionMap)
            {
                var p = pair.Value;

                if (x >= p.domain.x && x <= p.domain.y)
                {
                    piece = p;
                    break;
                }
            }
        }

        void SliderOutOfBounds(Rect rect, SerializedProperty value)
        {
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, value.floatValue, 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                Piece p = new Piece();
                UpdatePiece(ref p, internalValue);
                value.floatValue = SliderToValue(p, internalValue);
            }
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange, Vector2 valueRange)
        {
            // Map the internal slider value to the current piecewise function
            var k = valueRange.GetHashCode();
            if (!m_PiecewiseFunctionMap.TryGetValue(k, out var piece))
            {
                // Assume that if the piece is not found, that means the unit value is out of bounds.
                SliderOutOfBounds(rect, value);
                return;
            }

            // Maintain an internal value to support a single linear continuous function
            EditorGUI.BeginChangeCheck();
            var internalValue = GUI.HorizontalSlider(rect, ValueToSlider(piece, value.floatValue), 0f, 1f);
            if (EditorGUI.EndChangeCheck())
            {
                // Ensure that the current function piece is being used to transform the value
                UpdatePiece(ref piece, internalValue);
                value.floatValue = SliderToValue(piece, internalValue);
            }
        }
    }

    /// <summary>
    /// Formats the provided descriptor into a temperature unit slider with contextual slider markers, tooltips, and icons.
    /// </summary>
    class TemperatureSlider : LightUnitSlider
    {
        private LightEditor.Settings m_Settings;

        private static Texture2D s_KelvinGradientTexture;

        static Texture2D GetKelvinGradientTexture(LightEditor.Settings settings)
        {
            if (s_KelvinGradientTexture == null)
            {
                var kelvinTexture = (Texture2D)typeof(LightEditor.Settings).GetField("m_KelvinGradientTexture", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(settings);

                // This seems to be the only way to gamma-correct the internal gradient tex (aside from drawing it manually).
                var kelvinTextureLinear = new Texture2D(kelvinTexture.width, kelvinTexture.height, TextureFormat.RGBA32, true);
                kelvinTextureLinear.SetPixels(kelvinTexture.GetPixels());
                kelvinTextureLinear.Apply();

                s_KelvinGradientTexture = kelvinTextureLinear;
            }

            return s_KelvinGradientTexture;
        }

        public TemperatureSlider(LightUnitSliderUIDescriptor descriptor) : base(descriptor) {}

        public void SetLightSettings(LightEditor.Settings settings)
        {
            m_Settings = settings;
        }

        protected override void DoSlider(Rect rect, SerializedProperty value, Vector2 sliderRange)
        {
            SliderWithTextureNoTextField(rect, value, sliderRange, m_Settings);
        }

        // Note: We could use the internal SliderWithTexture, however: the internal slider func forces a text-field (and no ability to opt-out of it).
        void SliderWithTextureNoTextField(Rect rect, SerializedProperty value, Vector2 range, LightEditor.Settings settings)
        {
            GUI.DrawTexture(rect, GetKelvinGradientTexture(settings));

            var sliderBorder = new GUIStyle("ColorPickerSliderBackground");
            var sliderThumb = new GUIStyle("ColorPickerHorizThumb");
            value.floatValue = GUI.HorizontalSlider(rect, value.floatValue, range.x, range.y, sliderBorder, sliderThumb);
        }
    }

    internal class LightUnitSliderUIDrawer
    {
        static Dictionary<LightUnit, LightUnitSlider> k_LightUnitSliderMap;
        static LightUnitSlider k_ExposureSlider;
        static TemperatureSlider k_TemperatureSlider;

        static LightUnitSliderUIDrawer()
        {
            k_LightUnitSliderMap = new Dictionary<LightUnit, LightUnitSlider>
            {
                { LightUnit.Lux,     new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.LuxDescriptor)     },
                { LightUnit.Lumen,   new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.LumenDescriptor)   },
                { LightUnit.Candela, new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.CandelaDescriptor) },
                { LightUnit.Ev100,   new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.EV100Descriptor)   },
                { LightUnit.Nits,    new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.NitsDescriptor)    },
            };

            // Exposure is in EV100, but we load a separate due to the different icon set.
            k_ExposureSlider = new PiecewiseLightUnitSlider(LightUnitSliderDescriptors.ExposureDescriptor);

            // Kelvin is not classified internally as a light unit so we handle it independently as well.
            k_TemperatureSlider = new TemperatureSlider(LightUnitSliderDescriptors.TemperatureDescriptor);
        }

        public void Draw(LightUnit unit, SerializedProperty value, Rect rect)
        {
            if (!k_LightUnitSliderMap.TryGetValue(unit, out var lightUnitSlider))
                return;

            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                lightUnitSlider.Draw(rect, value);
            }
        }

        public void DrawExposureSlider(SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                k_ExposureSlider.Draw(rect, value);
            }
        }

        public void DrawTemperatureSlider(LightEditor.Settings settings, SerializedProperty value, Rect rect)
        {
            using (new EditorGUI.IndentLevelScope(-EditorGUI.indentLevel))
            {
                k_TemperatureSlider.SetLightSettings(settings);
                k_TemperatureSlider.Draw(rect, value);
            }
        }
    }
}
