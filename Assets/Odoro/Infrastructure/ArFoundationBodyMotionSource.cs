using System;
using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Odoro
{
    public sealed class ArFoundationBodyMotionSource : MonoBehaviour, IMotionSource, IPrimaryCameraSource
    {
        public CaptureMode CaptureMode => CaptureMode.RearBody3D;

        public bool IsSupported
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public Camera PrimaryCamera => arCamera;
        public bool ManagesCamera => arCamera != null;

        public event Action<MotionFrame> OnFrame;
        public event Action<string> OnStatusTextChanged;

        private GameObject arSessionObject;
        private GameObject xrOriginObject;
        private GameObject arCameraObject;
        private ARSession arSession;
        private ARInputManager arInputManager;
        private XROrigin xrOrigin;
        private ARHumanBodyManager humanBodyManager;
        private ARCameraManager arCameraManager;
        private ARCameraBackground arCameraBackground;
        private TrackedPoseDriver trackedPoseDriver;
        private Camera arCamera;
        private MotionSourceActivity currentActivity;
        private Coroutine availabilityRoutine;
        private string lastStatusText;
        private ARSessionState lastSessionState = ARSessionState.None;
        private bool bodyDetected;
        private bool active;

        private void Awake()
        {
            EnsureSceneGraph();
        }

        private void OnEnable()
        {
            ARSession.stateChanged += HandleSessionStateChanged;
            if (humanBodyManager != null)
            {
                humanBodyManager.trackablesChanged.AddListener(HandleHumanBodiesChanged);
            }
        }

        private void OnDisable()
        {
            ARSession.stateChanged -= HandleSessionStateChanged;
            if (humanBodyManager != null)
            {
                humanBodyManager.trackablesChanged.RemoveListener(HandleHumanBodiesChanged);
            }
        }

        public void Activate(MotionSourceActivity activity)
        {
            currentActivity = activity;
            active = true;
            bodyDetected = false;

            if (!IsSupported)
            {
                EmitStatusText(StudioL10n.StatusArUnsupported);
                return;
            }

            EnsureSceneGraph();
            arSessionObject.SetActive(true);
            xrOriginObject.SetActive(true);
            arSession.enabled = true;
            arSession.requestedTrackingMode = TrackingMode.PositionAndRotation;
            humanBodyManager.enabled = false;
            arCameraManager.enabled = false;
            arCameraBackground.enabled = false;
            trackedPoseDriver.enabled = false;

            if (availabilityRoutine != null)
            {
                StopCoroutine(availabilityRoutine);
            }

            availabilityRoutine = StartCoroutine(CheckAvailabilityAndStart());
        }

        public void Deactivate()
        {
            active = false;
            bodyDetected = false;
            lastStatusText = null;

            if (availabilityRoutine != null)
            {
                StopCoroutine(availabilityRoutine);
                availabilityRoutine = null;
            }

            if (humanBodyManager != null)
            {
                humanBodyManager.enabled = false;
            }

            if (arCameraBackground != null)
            {
                arCameraBackground.enabled = false;
            }

            if (arCameraManager != null)
            {
                arCameraManager.enabled = false;
            }

            if (trackedPoseDriver != null)
            {
                trackedPoseDriver.enabled = false;
            }

            if (arSession != null)
            {
                arSession.enabled = false;
            }

            if (xrOriginObject != null)
            {
                xrOriginObject.SetActive(false);
            }
        }

        public void Tick(float now)
        {
            if (!active || !IsSupported)
            {
                return;
            }

            if (arCameraManager == null || !arCameraManager.enabled)
            {
                return;
            }

            if (arCameraManager.permissionGranted)
            {
                if (lastStatusText == StudioL10n.StatusArNeedsCameraPermission && !bodyDetected)
                {
                    EmitStatusText(StatusTextForSessionState(lastSessionState));
                }

                return;
            }

            if (lastSessionState is ARSessionState.Ready or ARSessionState.SessionInitializing or ARSessionState.SessionTracking)
            {
                EmitStatusText(StudioL10n.StatusArNeedsCameraPermission);
            }
        }

        private IEnumerator CheckAvailabilityAndStart()
        {
            EmitStatusText(StudioL10n.StatusArCheckingAvailability);
            yield return ARSession.CheckAvailability();

            if (!active)
            {
                availabilityRoutine = null;
                yield break;
            }

            if (ARSession.state == ARSessionState.NeedsInstall)
            {
                EmitStatusText(StudioL10n.StatusArNeedsInstall);
                yield return ARSession.Install();
            }

            if (ARSession.state is ARSessionState.Unsupported or ARSessionState.None)
            {
                EmitStatusText(StudioL10n.StatusArUnsupported);
                availabilityRoutine = null;
                yield break;
            }

            arCameraManager.enabled = true;
            arCameraBackground.enabled = true;
            trackedPoseDriver.enabled = true;
            humanBodyManager.enabled = true;

            if (!arCameraManager.permissionGranted)
            {
                EmitStatusText(StudioL10n.StatusArNeedsCameraPermission);
            }
            else
            {
                EmitStatusText(StudioL10n.StatusArPreparing);
            }

            availabilityRoutine = null;
        }

        private void EnsureSceneGraph()
        {
            if (xrOriginObject != null)
            {
                return;
            }

            arSessionObject = new GameObject("Odoro AR Session");
            arSessionObject.transform.SetParent(transform, false);
            arSession = arSessionObject.AddComponent<ARSession>();
            arInputManager = arSessionObject.AddComponent<ARInputManager>();

            xrOriginObject = new GameObject("Odoro XR Origin");
            xrOriginObject.transform.SetParent(transform, false);
            xrOrigin = xrOriginObject.AddComponent<XROrigin>();
            humanBodyManager = xrOriginObject.AddComponent<ARHumanBodyManager>();
            humanBodyManager.pose2DRequested = false;
            humanBodyManager.pose3DRequested = true;
            humanBodyManager.pose3DScaleEstimationRequested = false;
            humanBodyManager.enabled = false;

            arCameraObject = new GameObject("AR Camera");
            arCameraObject.transform.SetParent(xrOriginObject.transform, false);
            arCameraObject.tag = "MainCamera";
            arCamera = arCameraObject.AddComponent<Camera>();
            arCamera.clearFlags = CameraClearFlags.SolidColor;
            arCamera.backgroundColor = Color.black;
            arCamera.nearClipPlane = 0.1f;
            arCamera.farClipPlane = 20f;
            arCameraManager = arCameraObject.AddComponent<ARCameraManager>();
            arCameraManager.enabled = false;
            arCameraBackground = arCameraObject.AddComponent<ARCameraBackground>();
            arCameraBackground.enabled = false;
            trackedPoseDriver = arCameraObject.AddComponent<TrackedPoseDriver>();
            ConfigureTrackedPoseDriver(trackedPoseDriver);
            trackedPoseDriver.enabled = false;

            xrOrigin.Camera = arCamera;
            xrOriginObject.SetActive(false);
        }

        private void HandleSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
        {
            if (!active)
            {
                return;
            }

            lastSessionState = eventArgs.state;

            if (!bodyDetected)
            {
                EmitStatusText(StatusTextForSessionState(eventArgs.state));
            }
        }

        private void HandleHumanBodiesChanged(ARTrackablesChangedEventArgs<ARHumanBody> eventArgs)
        {
            var body = FirstTrackedBody(eventArgs);
            if (body == null)
            {
                if (bodyDetected)
                {
                    bodyDetected = false;
                    EmitStatusText(StudioL10n.StatusArLost);
                }

                return;
            }

            if (!bodyDetected)
            {
                bodyDetected = true;
                EmitStatusText(StudioL10n.StatusArDetected);
            }

            var frame = MakeFrame(body);
            OnFrame?.Invoke(frame);
        }

        private void EmitStatusText(string statusText)
        {
            if (string.Equals(lastStatusText, statusText, StringComparison.Ordinal))
            {
                return;
            }

            lastStatusText = statusText;
            OnStatusTextChanged?.Invoke(statusText);
        }

        private string StatusTextForSessionState(ARSessionState state)
        {
            return state switch
            {
                ARSessionState.CheckingAvailability => StudioL10n.StatusArCheckingAvailability,
                ARSessionState.NeedsInstall or ARSessionState.Installing => StudioL10n.StatusArNeedsInstall,
                ARSessionState.Unsupported => StudioL10n.StatusArUnsupported,
                ARSessionState.Ready => StudioL10n.StatusArPreparing,
                ARSessionState.SessionInitializing => StudioL10n.StatusArSessionInitializing,
                ARSessionState.SessionTracking => StudioL10n.StatusArLost,
                _ => StudioL10n.StatusArPreparing,
            };
        }

        private static ARHumanBody FirstTrackedBody(ARTrackablesChangedEventArgs<ARHumanBody> eventArgs)
        {
            for (var i = 0; i < eventArgs.updated.Count; i += 1)
            {
                if (eventArgs.updated[i] != null)
                {
                    return eventArgs.updated[i];
                }
            }

            for (var i = 0; i < eventArgs.added.Count; i += 1)
            {
                if (eventArgs.added[i] != null)
                {
                    return eventArgs.added[i];
                }
            }

            return null;
        }

        private static void ConfigureTrackedPoseDriver(TrackedPoseDriver poseDriver)
        {
            poseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            poseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            poseDriver.ignoreTrackingState = false;

            var positionAction = new InputAction("AR Camera Position", expectedControlType: "Vector3");
            positionAction.AddBinding("<XRHMD>/centerEyePosition");
            positionAction.AddBinding("<TrackedDevice>/devicePosition");
            poseDriver.positionInput = new InputActionProperty(positionAction);

            var rotationAction = new InputAction("AR Camera Rotation", expectedControlType: "Quaternion");
            rotationAction.AddBinding("<XRHMD>/centerEyeRotation");
            rotationAction.AddBinding("<TrackedDevice>/deviceRotation");
            poseDriver.rotationInput = new InputActionProperty(rotationAction);

            var trackingStateAction = new InputAction("AR Camera Tracking State", expectedControlType: "Integer");
            trackingStateAction.AddBinding("<XRHMD>/trackingState");
            trackingStateAction.AddBinding("<TrackedDevice>/trackingState");
            poseDriver.trackingStateInput = new InputActionProperty(trackingStateAction);
        }

        private static MotionFrame MakeFrame(ARHumanBody body)
        {
            var joints = body.joints;
            var positions = new Vector3[joints.Length];
            var rotations = new MotionJointRotation[joints.Length];

            for (var jointIndex = 0; jointIndex < joints.Length; jointIndex += 1)
            {
                var joint = joints[jointIndex];
                var worldPosition = body.transform.TransformPoint(joint.anchorPose.position);
                var worldRotation = body.transform.rotation * joint.anchorPose.rotation;
                positions[jointIndex] = worldPosition;
                rotations[jointIndex] = new MotionJointRotation(worldRotation);
            }

            return new MotionFrame
            {
                time = Time.unscaledTime,
                jointPositions = positions,
                jointRotations = rotations,
            };
        }
    }
}
