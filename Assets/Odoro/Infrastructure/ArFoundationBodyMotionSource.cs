using System;
using System.Collections;
using Unity.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Odoro
{
    public sealed class ArFoundationBodyMotionSource : MonoBehaviour, IMotionSource, IPrimaryCameraSource, IMotionSourceDebugInfo
    {
        private const int ArKitHips = 1;
        private const int ArKitLeftUpperLeg = 2;
        private const int ArKitLeftLeg = 3;
        private const int ArKitLeftFoot = 4;
        private const int ArKitRightUpperLeg = 7;
        private const int ArKitRightLeg = 8;
        private const int ArKitRightFoot = 9;
        private const int ArKitSpine1 = 12;
        private const int ArKitSpine7 = 18;
        private const int ArKitLeftShoulder = 19;
        private const int ArKitLeftUpperArm = 20;
        private const int ArKitLeftForearm = 21;
        private const int ArKitLeftHand = 22;
        private const int ArKitNeck1 = 47;
        private const int ArKitHead = 51;
        private const int ArKitRightShoulder = 63;
        private const int ArKitRightUpperArm = 64;
        private const int ArKitRightForearm = 65;
        private const int ArKitRightHand = 66;

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
        public string[] DebugLines => debugLines;

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
        private string[] debugLines = { "ARKit debug: waiting for body frame." };

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

                debugLines = new[]
                {
                    $"ARKit debug: no tracked body ({eventArgs.added.Count} added, {eventArgs.updated.Count} updated).",
                    $"AR session: {lastSessionState}",
                };
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
                if (IsBodyUsable(eventArgs.updated[i]))
                {
                    return eventArgs.updated[i];
                }
            }

            for (var i = 0; i < eventArgs.added.Count; i += 1)
            {
                if (IsBodyUsable(eventArgs.added[i]))
                {
                    return eventArgs.added[i];
                }
            }

            return null;
        }

        private static bool IsBodyUsable(ARHumanBody body)
        {
            return body != null
                && body.trackingState != TrackingState.None
                && body.joints.IsCreated
                && body.joints.Length > ArKitRightHand;
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

        private MotionFrame MakeFrame(ARHumanBody body)
        {
            var joints = body.joints;
            var rawPositions = new Vector3[joints.Length];
            var rawRotations = new Quaternion[joints.Length];

            for (var jointIndex = 0; jointIndex < joints.Length; jointIndex += 1)
            {
                var joint = joints[jointIndex];
                var worldPosition = body.transform.TransformPoint(joint.anchorPose.position);
                var worldRotation = body.transform.rotation * joint.anchorPose.rotation;
                rawPositions[jointIndex] = worldPosition;
                rawRotations[jointIndex] = worldRotation;
            }

            var positions = new Vector3[OdoroSkeletonDefinition.JointCount];
            var rotations = new MotionJointRotation[OdoroSkeletonDefinition.JointCount];

            SetJoint(OdoroJointName.Root, ArKitHips, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.Spine, ArKitSpine1, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.Chest, ArKitSpine7, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.Neck, ArKitNeck1, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.Head, ArKitHead, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftShoulder, ArKitLeftShoulder, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftUpperArm, ArKitLeftUpperArm, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftElbow, ArKitLeftForearm, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftWrist, ArKitLeftHand, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightShoulder, ArKitRightShoulder, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightUpperArm, ArKitRightUpperArm, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightElbow, ArKitRightForearm, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightWrist, ArKitRightHand, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftHip, ArKitLeftUpperLeg, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftKnee, ArKitLeftLeg, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.LeftFoot, ArKitLeftFoot, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightHip, ArKitRightUpperLeg, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightKnee, ArKitRightLeg, rawPositions, rawRotations, positions, rotations);
            SetJoint(OdoroJointName.RightFoot, ArKitRightFoot, rawPositions, rawRotations, positions, rotations);

            positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.LeftAnkle)] =
                Vector3.Lerp(
                    positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.LeftKnee)],
                    positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.LeftFoot)],
                    0.82f
                );
            positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.RightAnkle)] =
                Vector3.Lerp(
                    positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.RightKnee)],
                    positions[OdoroSkeletonDefinition.IndexOf(OdoroJointName.RightFoot)],
                    0.82f
                );

            if (ContainsJoint(rawRotations, ArKitLeftFoot))
            {
                rotations[OdoroSkeletonDefinition.IndexOf(OdoroJointName.LeftAnkle)] = new MotionJointRotation(rawRotations[ArKitLeftFoot]);
            }

            if (ContainsJoint(rawRotations, ArKitRightFoot))
            {
                rotations[OdoroSkeletonDefinition.IndexOf(OdoroJointName.RightAnkle)] = new MotionJointRotation(rawRotations[ArKitRightFoot]);
            }

            debugLines = BuildDebugLines(joints, rawPositions, positions);

            return new MotionFrame
            {
                time = Time.unscaledTime,
                jointPositions = positions,
                jointRotations = rotations,
            };
        }

        private static void SetJoint(
            OdoroJointName jointName,
            int arKitIndex,
            Vector3[] rawPositions,
            Quaternion[] rawRotations,
            Vector3[] positions,
            MotionJointRotation[] rotations
        )
        {
            var targetIndex = OdoroSkeletonDefinition.IndexOf(jointName);
            if (targetIndex < 0)
            {
                return;
            }

            if (!ContainsJoint(rawPositions, arKitIndex))
            {
                return;
            }

            positions[targetIndex] = rawPositions[arKitIndex];
            rotations[targetIndex] = new MotionJointRotation(rawRotations[arKitIndex]);
        }

        private static bool ContainsJoint(Array joints, int index)
        {
            return joints != null && index >= 0 && index < joints.Length;
        }

        private static bool ContainsJoint(NativeArray<XRHumanBodyJoint> joints, int index)
        {
            return joints.IsCreated && index >= 0 && index < joints.Length;
        }

        private static string[] BuildDebugLines(
            NativeArray<XRHumanBodyJoint> joints,
            Vector3[] rawPositions,
            Vector3[] canonicalPositions
        )
        {
            return new[]
            {
                $"ARKit joints: {joints.Length}",
                $"AR parents L arm: {ParentLabel(joints, ArKitLeftShoulder)}/{ParentLabel(joints, ArKitLeftUpperArm)}/{ParentLabel(joints, ArKitLeftForearm)}/{ParentLabel(joints, ArKitLeftHand)}",
                $"AR parents R arm: {ParentLabel(joints, ArKitRightShoulder)}/{ParentLabel(joints, ArKitRightUpperArm)}/{ParentLabel(joints, ArKitRightForearm)}/{ParentLabel(joints, ArKitRightHand)}",
                $"AR raw shoulder L19 {PositionLabel(rawPositions, ArKitLeftShoulder)} R63 {PositionLabel(rawPositions, ArKitRightShoulder)}",
                $"AR raw upper L19->20 {DeltaLabel(rawPositions, ArKitLeftShoulder, ArKitLeftUpperArm)} R63->64 {DeltaLabel(rawPositions, ArKitRightShoulder, ArKitRightUpperArm)}",
                $"AR raw forearm L21->22 {DeltaLabel(rawPositions, ArKitLeftForearm, ArKitLeftHand)} R65->66 {DeltaLabel(rawPositions, ArKitRightForearm, ArKitRightHand)}",
                $"Canon shoulder L {PositionLabel(canonicalPositions, OdoroJointName.LeftShoulder)} R {PositionLabel(canonicalPositions, OdoroJointName.RightShoulder)}",
                $"Canon upper L {DeltaLabel(canonicalPositions, OdoroJointName.LeftShoulder, OdoroJointName.LeftUpperArm)} R {DeltaLabel(canonicalPositions, OdoroJointName.RightShoulder, OdoroJointName.RightUpperArm)}",
                $"Canon hip width {DeltaLabel(canonicalPositions, OdoroJointName.LeftHip, OdoroJointName.RightHip)} shoulder width {DeltaLabel(canonicalPositions, OdoroJointName.LeftShoulder, OdoroJointName.RightShoulder)}",
            };
        }

        private static string ParentLabel(NativeArray<XRHumanBodyJoint> joints, int index)
        {
            return ContainsJoint(joints, index) ? $"{index}<-{joints[index].parentIndex}" : $"{index}<---";
        }

        private static string PositionLabel(Vector3[] positions, OdoroJointName jointName)
        {
            return PositionLabel(positions, OdoroSkeletonDefinition.IndexOf(jointName));
        }

        private static string PositionLabel(Vector3[] positions, int index)
        {
            if (!ContainsJoint(positions, index))
            {
                return "--";
            }

            var position = positions[index];
            return $"({position.x:0.00},{position.y:0.00},{position.z:0.00})";
        }

        private static string DeltaLabel(Vector3[] positions, OdoroJointName startJoint, OdoroJointName endJoint)
        {
            return DeltaLabel(
                positions,
                OdoroSkeletonDefinition.IndexOf(startJoint),
                OdoroSkeletonDefinition.IndexOf(endJoint)
            );
        }

        private static string DeltaLabel(Vector3[] positions, int startIndex, int endIndex)
        {
            if (!ContainsJoint(positions, startIndex) || !ContainsJoint(positions, endIndex))
            {
                return "--";
            }

            var delta = positions[endIndex] - positions[startIndex];
            return $"d({delta.x:0.00},{delta.y:0.00},{delta.z:0.00})";
        }
    }
}
