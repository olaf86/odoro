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
        public Action showModelSelection;
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
        public Action<string> selectAvatarOption;
        public Action showDebugHud;
        public Action hideDebugHud;
        public Action startDebugFrameCapture;
        public Action stopDebugFrameCapture;
        public Action shareDebugMotionFrames;
        public Action toggleDebugAvatarArmSwap;
    }

    public sealed class OdoroStudioUiSnapshot
    {
        public StudioScreen screen;
        public string transientMessage;
        public CaptureScreenSnapshot capture;
        public RecordingSettingsScreenSnapshot settings;
        public StageScreenSnapshot stage;
        public LibraryScreenSnapshot library;
        public ModelSelectionScreenSnapshot modelSelection;
        public DebugHudSnapshot debug;
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

    public sealed class ModelSelectionScreenSnapshot
    {
        public IReadOnlyList<StageAvatarOptionSnapshot> options;
        public string selectedOptionId;
        public bool isBusy;
    }

    public sealed class StageAvatarOptionSnapshot
    {
        public string id;
        public string title;
        public string subtitle;
        public bool isSelected;
        public bool usesAvatar;
        public bool requiresDownload;
    }

    public sealed class LibraryScreenSnapshot
    {
        public IReadOnlyList<MotionTakeSummary> clips;
    }

    public sealed class DebugHudSnapshot
    {
        public bool isAvailable;
        public bool isVisible;
        public bool isCapturing;
        public bool canShare;
        public bool avatarArmSwapEnabled;
        public string[] lines;
        public string captureButtonLabel;
        public string avatarArmSwapButtonLabel;
    }
}
