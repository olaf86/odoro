using UnityEngine;

namespace Odoro
{
    public static class Palette
    {
        public static readonly Color Background = new Color(0.05f, 0.07f, 0.11f);
        public static readonly Color CaptureSkeleton = new Color(0.35f, 0.87f, 0.95f);
        public static readonly Color StageSkeleton = new Color(1.0f, 0.62f, 0.42f);
        public static readonly Color Toast = new Color(0.18f, 0.55f, 0.36f);
    }

    public static class GuiStyles
    {
        private static GUIStyle title;
        private static GUIStyle subtitle;
        private static GUIStyle sectionTitle;
        private static GUIStyle body;
        private static GUIStyle card;
        private static GUIStyle cardLabel;
        private static GUIStyle cardValue;
        private static GUIStyle primaryButton;
        private static GUIStyle secondaryButton;
        private static GUIStyle smallButton;
        private static GUIStyle stepperValue;
        private static GUIStyle footnote;
        private static GUIStyle window;
        private static GUIStyle toast;

        public static GUIStyle Title => title ??= BuildLabel(26, FontStyle.Bold, Color.white);
        public static GUIStyle Subtitle => subtitle ??= BuildLabel(12, FontStyle.Normal, new Color(0.74f, 0.80f, 0.86f));
        public static GUIStyle SectionTitle => sectionTitle ??= BuildLabel(20, FontStyle.Bold, Color.white);
        public static GUIStyle Body => body ??= BuildLabel(13, FontStyle.Normal, new Color(0.86f, 0.89f, 0.92f));
        public static GUIStyle Card => card ??= BuildCard();
        public static GUIStyle CardLabel => cardLabel ??= BuildLabel(12, FontStyle.Normal, new Color(0.72f, 0.78f, 0.84f));
        public static GUIStyle CardValue => cardValue ??= BuildLabel(16, FontStyle.Bold, Color.white);
        public static GUIStyle PrimaryButton => primaryButton ??= BuildButton(new Color(0.18f, 0.52f, 0.87f), Color.white);
        public static GUIStyle SecondaryButton => secondaryButton ??= BuildButton(new Color(0.16f, 0.19f, 0.24f), Color.white);
        public static GUIStyle SmallButton => smallButton ??= BuildButton(new Color(0.16f, 0.19f, 0.24f), Color.white, 16);
        public static GUIStyle StepperValue => stepperValue ??= BuildLabel(18, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        public static GUIStyle Footnote => footnote ??= BuildLabel(11, FontStyle.Normal, new Color(0.61f, 0.67f, 0.73f));
        public static GUIStyle Window => window ??= BuildWindow();
        public static GUIStyle Toast => toast ??= BuildToast();

        public static void ConfigureGuiSkin()
        {
            GUI.skin.label.richText = true;
            GUI.skin.button.richText = true;
        }

        private static GUIStyle BuildLabel(int fontSize, FontStyle fontStyle, Color textColor, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                wordWrap = true,
                alignment = alignment,
                richText = true,
                normal = { textColor = textColor },
                margin = new RectOffset(0, 0, 2, 2),
            };
        }

        private static GUIStyle BuildButton(Color backgroundColor, Color textColor, int fontSize = 14)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(16, 16, 10, 10),
                richText = true,
            };

            style.normal.background = MakeTexture(backgroundColor);
            style.hover.background = MakeTexture(backgroundColor * 1.1f);
            style.active.background = MakeTexture(backgroundColor * 0.9f);
            style.normal.textColor = textColor;
            style.hover.textColor = textColor;
            style.active.textColor = textColor;
            return style;
        }

        private static GUIStyle BuildCard()
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.11f, 0.14f, 0.19f, 0.94f)) },
                padding = new RectOffset(16, 16, 14, 14),
                margin = new RectOffset(0, 0, 6, 6),
            };
        }

        private static GUIStyle BuildWindow()
        {
            return new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTexture(new Color(0.08f, 0.10f, 0.14f, 0.92f)) },
                padding = new RectOffset(20, 20, 20, 20),
            };
        }

        private static GUIStyle BuildToast()
        {
            return new GUIStyle(GUI.skin.box)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(12, 12, 8, 8),
                normal =
                {
                    background = MakeTexture(new Color(0.14f, 0.36f, 0.24f)),
                    textColor = Color.white,
                },
            };
        }

        private static Texture2D MakeTexture(Color color)
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
