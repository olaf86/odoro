using UnityEngine;
using UnityEngine.UIElements;
using static Odoro.OdoroStudioUiFactory;

namespace Odoro
{
    public sealed class StudioToastView
    {
        private readonly Label toastLabel;

        public StudioToastView(VisualElement parent)
        {
            toastLabel = CreateBodyLabel();
            toastLabel.name = "odoro-toast";
            toastLabel.style.position = Position.Absolute;
            toastLabel.style.left = 24f;
            toastLabel.style.right = 24f;
            toastLabel.style.bottom = 18f;
            toastLabel.style.paddingLeft = 14f;
            toastLabel.style.paddingRight = 14f;
            toastLabel.style.paddingTop = 10f;
            toastLabel.style.paddingBottom = 10f;
            toastLabel.style.backgroundColor = Palette.Toast;
            toastLabel.style.borderTopLeftRadius = 18f;
            toastLabel.style.borderTopRightRadius = 18f;
            toastLabel.style.borderBottomLeftRadius = 18f;
            toastLabel.style.borderBottomRightRadius = 18f;
            toastLabel.style.display = DisplayStyle.None;
            toastLabel.style.color = Color.white;
            toastLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(toastLabel);
        }

        public void Render(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                toastLabel.style.display = DisplayStyle.None;
                return;
            }

            toastLabel.text = message;
            toastLabel.style.display = DisplayStyle.Flex;
        }
    }
}
