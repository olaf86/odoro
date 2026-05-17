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
        public MotionStudioState state;
        public MotionRecordingContext recordingContext;
        public CaptureMode captureMode;
        public MotionClip selectedClip;
        public MotionTakeSummary selectedTake;
        public IReadOnlyList<MotionTakeSummary> libraryClips;
        public bool hasAvatar;
        public string transientMessage;
        public string captureHeadline;
        public string captureSummary;
        public string captureStatus;
        public string captureModeLabel;
        public string captureMetrics;
        public string captureTrackingSignal;
        public Color captureTrackingSignalColor;
        public float captureProgress;
        public bool captureSkeletonVisible;
        public string stageTitle;
        public string stageSummary;
        public string stageModeLabel;
        public string stageHint;
    }
}
