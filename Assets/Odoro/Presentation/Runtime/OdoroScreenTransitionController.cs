using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Odoro
{
    public readonly struct OdoroScreenTransitionProfile
    {
        public readonly float durationSeconds;
        public readonly float incomingSlidePixels;
        public readonly float outgoingSlidePixels;
        public readonly float veilPeakOpacity;
        public readonly int frameIntervalMilliseconds;
        public readonly Color veilColor;

        public OdoroScreenTransitionProfile(
            float durationSeconds,
            float incomingSlidePixels,
            float outgoingSlidePixels,
            float veilPeakOpacity,
            int frameIntervalMilliseconds,
            Color veilColor
        )
        {
            this.durationSeconds = Mathf.Max(0.01f, durationSeconds);
            this.incomingSlidePixels = Mathf.Max(0f, incomingSlidePixels);
            this.outgoingSlidePixels = Mathf.Max(0f, outgoingSlidePixels);
            this.veilPeakOpacity = Mathf.Clamp01(veilPeakOpacity);
            this.frameIntervalMilliseconds = Mathf.Max(1, frameIntervalMilliseconds);
            this.veilColor = veilColor;
        }

        public static OdoroScreenTransitionProfile Default => new OdoroScreenTransitionProfile(
            durationSeconds: 0.22f,
            incomingSlidePixels: 18f,
            outgoingSlidePixels: 8f,
            veilPeakOpacity: 0.22f,
            frameIntervalMilliseconds: 16,
            veilColor: Color.black
        );

        public OdoroScreenTransitionProfile WithDuration(float nextDurationSeconds)
        {
            return new OdoroScreenTransitionProfile(
                nextDurationSeconds,
                incomingSlidePixels,
                outgoingSlidePixels,
                veilPeakOpacity,
                frameIntervalMilliseconds,
                veilColor
            );
        }

        public OdoroScreenTransitionProfile WithVeilPeakOpacity(float nextVeilPeakOpacity)
        {
            return new OdoroScreenTransitionProfile(
                durationSeconds,
                incomingSlidePixels,
                outgoingSlidePixels,
                nextVeilPeakOpacity,
                frameIntervalMilliseconds,
                veilColor
            );
        }
    }

    public sealed class OdoroScreenTransitionController
    {
        private readonly Dictionary<StudioScreen, VisualElement> screens;
        private readonly VisualElement veil;
        private readonly OdoroScreenTransitionProfile profile;

        private StudioScreen activeScreen;
        private bool hasActiveScreen;
        private int transitionVersion;
        private IVisualElementScheduledItem activeTransition;

        public OdoroScreenTransitionController(
            VisualElement parent,
            Dictionary<StudioScreen, VisualElement> screens,
            OdoroScreenTransitionProfile profile
        )
        {
            this.screens = screens;
            this.profile = profile;

            veil = new VisualElement { name = "odoro-screen-transition-veil" };
            veil.AddToClassList("odoro-screen-transition-veil");
            veil.pickingMode = PickingMode.Ignore;
            veil.style.position = Position.Absolute;
            veil.style.left = 0f;
            veil.style.right = 0f;
            veil.style.top = 0f;
            veil.style.bottom = 0f;
            veil.style.backgroundColor = profile.veilColor;
            veil.style.opacity = 0f;
            veil.style.display = DisplayStyle.None;
            parent.Add(veil);

            foreach (var screen in screens.Values)
            {
                HideImmediately(screen);
            }
        }

        public void Show(StudioScreen nextScreen)
        {
            if (!screens.TryGetValue(nextScreen, out var incoming))
            {
                return;
            }

            if (!hasActiveScreen)
            {
                ShowImmediately(nextScreen, incoming);
                return;
            }

            if (activeScreen == nextScreen)
            {
                incoming.style.display = DisplayStyle.Flex;
                return;
            }

            activeTransition?.Pause();
            var outgoing = screens[activeScreen];
            StartTransition(activeScreen, outgoing, nextScreen, incoming);
        }

        private void ShowImmediately(StudioScreen nextScreen, VisualElement incoming)
        {
            foreach (var screen in screens.Values)
            {
                HideImmediately(screen);
            }

            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 1f;
            incoming.style.translate = TranslatePixels(0f);
            veil.style.display = DisplayStyle.None;
            veil.style.opacity = 0f;
            activeScreen = nextScreen;
            hasActiveScreen = true;
        }

        private void StartTransition(
            StudioScreen outgoingScreen,
            VisualElement outgoing,
            StudioScreen incomingScreen,
            VisualElement incoming
        )
        {
            var version = ++transitionVersion;
            var startedAt = Time.unscaledTime;
            var slideDirection = SlideDirection(outgoingScreen, incomingScreen);
            var routeProfile = ProfileForRoute(incomingScreen);

            foreach (var screen in screens.Values)
            {
                if (screen != outgoing && screen != incoming)
                {
                    HideImmediately(screen);
                }
            }

            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 0f;
            incoming.style.translate = TranslatePixels(routeProfile.incomingSlidePixels * slideDirection);
            outgoing.style.display = DisplayStyle.Flex;
            outgoing.style.opacity = 1f;
            outgoing.style.translate = TranslatePixels(0f);
            veil.style.display = DisplayStyle.Flex;
            veil.style.opacity = 0f;

            activeScreen = incomingScreen;
            IVisualElementScheduledItem transitionItem = null;
            transitionItem = incoming.schedule.Execute(() =>
            {
                if (version != transitionVersion)
                {
                    transitionItem?.Pause();
                    return;
                }

                var elapsed = Time.unscaledTime - startedAt;
                var progress = Mathf.Clamp01(elapsed / routeProfile.durationSeconds);
                var eased = EaseOutCubic(progress);
                var outgoingOpacity = 1f - eased;
                var incomingOpacity = eased;
                var veilOpacity = Mathf.Sin(progress * Mathf.PI) * routeProfile.veilPeakOpacity;

                incoming.style.opacity = incomingOpacity;
                incoming.style.translate = TranslatePixels(routeProfile.incomingSlidePixels * (1f - eased) * slideDirection);
                outgoing.style.opacity = outgoingOpacity;
                outgoing.style.translate = TranslatePixels(-routeProfile.outgoingSlidePixels * eased * slideDirection);
                veil.style.opacity = veilOpacity;

                if (progress >= 1f)
                {
                    CompleteTransition(outgoing, incoming);
                    transitionItem?.Pause();
                }
            }).Every(routeProfile.frameIntervalMilliseconds);
            activeTransition = transitionItem;
        }

        private OdoroScreenTransitionProfile ProfileForRoute(StudioScreen incomingScreen)
        {
            if (incomingScreen == StudioScreen.Capture)
            {
                return profile.WithDuration(0.16f).WithVeilPeakOpacity(0.14f);
            }

            if (incomingScreen == StudioScreen.Stage)
            {
                return profile.WithDuration(0.26f).WithVeilPeakOpacity(0.24f);
            }

            return profile;
        }

        private void CompleteTransition(VisualElement outgoing, VisualElement incoming)
        {
            outgoing.style.display = DisplayStyle.None;
            outgoing.style.opacity = 0f;
            outgoing.style.translate = TranslatePixels(0f);
            incoming.style.display = DisplayStyle.Flex;
            incoming.style.opacity = 1f;
            incoming.style.translate = TranslatePixels(0f);
            veil.style.opacity = 0f;
            veil.style.display = DisplayStyle.None;
        }

        private static void HideImmediately(VisualElement screen)
        {
            screen.style.display = DisplayStyle.None;
            screen.style.opacity = 0f;
            screen.style.translate = TranslatePixels(0f);
        }

        private static int SlideDirection(StudioScreen from, StudioScreen to)
        {
            return ScreenOrder(to) >= ScreenOrder(from) ? 1 : -1;
        }

        private static int ScreenOrder(StudioScreen screen)
        {
            return screen switch
            {
                StudioScreen.Capture => 0,
                StudioScreen.RecordingSettings => 1,
                StudioScreen.ClipsLibrary => 2,
                StudioScreen.Stage => 3,
                _ => 0,
            };
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static Translate TranslatePixels(float y)
        {
            return new Translate(
                new Length(0f, LengthUnit.Pixel),
                new Length(y, LengthUnit.Pixel),
                0f
            );
        }
    }
}
