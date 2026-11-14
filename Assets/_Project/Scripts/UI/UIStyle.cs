using UnityEngine;

namespace ChickenShooter
{
    /// <summary>Built-in font access so UI never depends on imported font assets.</summary>
    public static class UIStyle
    {
        private static Font _font;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;

                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        public static readonly Color Accent = new Color(0.45f, 0.9f, 1f);
        public static readonly Color Warn = new Color(1f, 0.72f, 0.32f);
        public static readonly Color Danger = new Color(1f, 0.35f, 0.4f);
    }
}
