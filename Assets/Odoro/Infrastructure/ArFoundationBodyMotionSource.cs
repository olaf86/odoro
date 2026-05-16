using System;
using Unity.XR.CoreUtils;
using UnityEngine;
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
        private ARPoseDriver arPoseDriver;
        private Camera arCamera;
        private MotionSourceActivity currentActivity;
        private bool bodyDetected;

        private void Awake()
        {
            EnsureSceneGraph();
        }

        private void OnEnable()
        {
            if (humanBodyManager != null)
            {
                humanBodyManager.humanBodiesChanged += HandleHumanBodiesChanged;
            }
        }

        private void OnDisable()
        {
            if (humanBodyManager != null)
            {
                humanBodyManager.humanBodiesChanged -= HandleHumanBodiesChanged;
            }
        }

        public void Activate(MotionSourceActivity activity)
        {
            currentActivity = activity;

            if (!IsSupported)
            {
                OnStatusTextChanged?.Invoke(StudioL10n.StatusArUnsupported);
                return;
            }

            EnsureSceneGraph();
            bodyDetected = false;
            arSessionObject.SetActive(true);
            xrOriginObject.SetActive(true);
            arSession.enabled = true;
            humanBodyManager.enabled = true;
            arCameraManager.enabled = true;
            arCameraBackground.enabled = true;
            arPoseDriver.enabled = true;
            OnStatusTextChanged?.Invoke(StudioL10n.StatusArPreparing);
        }

        public void Deactivate()
        {
            bodyDetected = false;

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

            if (arPoseDriver != null)
            {
                arPoseDriver.enabled = false;
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
            arPoseDriver = arCameraObject.AddComponent<ARPoseDriver>();
            arPoseDriver.enabled = false;

            xrOrigin.Camera = arCamera;
            xrOriginObject.SetActive(false);
        }

        private void HandleHumanBodiesChanged(ARHumanBodiesChangedEventArgs eventArgs)
        {
            var body = FirstTrackedBody(eventArgs);
            if (body == null)
            {
                if (bodyDetected)
                {
                    bodyDetected = false;
                    OnStatusTextChanged?.Invoke(StudioL10n.StatusArLost);
                }

                return;
            }

            if (!bodyDetected)
            {
                bodyDetected = true;
                OnStatusTextChanged?.Invoke(StudioL10n.StatusArDetected);
            }

            var frame = MakeFrame(body);
            OnFrame?.Invoke(frame);
        }

        private static ARHumanBody FirstTrackedBody(ARHumanBodiesChangedEventArgs eventArgs)
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
