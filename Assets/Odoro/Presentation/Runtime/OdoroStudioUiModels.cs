using System;
using System.Collections.Generic;
using UnityEngine;

namespace Odoro
{
    public sealed class OdoroStudioUiActions
    {
        public Action showCapture;
        public Action showSettings;
        public Action showLibrary;
        public Action showStage;
        public Action startRecording;
        public Action stopRecording;
        public Action toggleSkeleton;
        public Action togglePlayback;
        public Action showModelInfo;
        public Action saveTake;
        public Action decreaseBpm;
        public Action increaseBpm;
        public Action decreaseNumerator;
        public Action increaseNumerator;
        public Action decreaseDenominator;
        public Action increaseDenominator;
        public Action decreaseCountInBars;
        public Action increaseCountInBars;
        public Action<MotionTakeSummary> openTake;
    }

    public sealed class OdoroStudioUiSnapshot
    {
        public StudioScreen screen;
        public string transientMessage;
        public CaptureScreenSnapshot capture;
        public RecordingSettingsScreenSnapshot settings;
        public StageScreenSnapshot stage;
        public LibraryScreenSnapshot library;
    }

    public sealed class CaptureScreenSnapshot
    {
        public string headline;
        public string summary;
        public string status;
        public string modeLabel;
        public string metrics;
        public string trackingSignal;
        public Color trackingSignalColor;
        public float progress;
        public bool skeletonVisible;
        public bool hasSelectedClip;
        public bool isRecording;
    }

    public sealed class RecordingSettingsScreenSnapshot
    {
        public float bpm;
        public int timeSignatureNumerator;
        public int timeSignatureDenominator;
        public int countInBarCount;
        public float fixedCaptureDuration;
        public bool skeletonVisible;
    }

    public sealed class StageScreenSnapshot
    {
        public bool hasSelectedClip;
        public bool isPlaying;
        public string title;
        public string summary;
        public string modeLabel;
        public string hint;
    }

    public sealed class LibraryScreenSnapshot
    {
        public IReadOnlyList<MotionTakeSummary> clips;
    }
}
