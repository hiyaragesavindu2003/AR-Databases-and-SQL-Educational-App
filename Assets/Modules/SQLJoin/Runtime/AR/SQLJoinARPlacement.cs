using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARSQLExplorer.SQLJoin
{
    public sealed class SQLJoinARPlacement : MonoBehaviour
    {
        public enum PlacementState { Scanning, PlacementPending, Placed }
        [SerializeField] private XROrigin xrOrigin;
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Camera arCamera;
        [SerializeField] private SQLJoinVisualization visualizationPrefab;
        [SerializeField] private SQLJoinController controller;
        [SerializeField, Min(0.01f)] private float visualizationScale = 0.075f;

        public PlacementState State { get; private set; }
        private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
        private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
        private ARAnchor placedAnchor;
        private SQLJoinVisualization placedVisualization;
        private int operationVersion;
        private string lastReadiness;
        private const string LogPrefix = "[SQLJoinARPlacement] ";
        private string feedback = "Move your device to detect a horizontal surface, then tap to place.";

        private void Start()
        {
            if (xrOrigin == null || raycastManager == null || planeManager == null ||
                anchorManager == null || arCamera == null || visualizationPrefab == null || controller == null)
            {
                Debug.LogError("Assign all SQL JOIN AR placement references.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            bool anchorReady = anchorManager.subsystem != null && anchorManager.subsystem.running;
            string readiness = ARSession.state + "; anchorReady=" + anchorReady
                + "; raycastEnabled=" + raycastManager.isActiveAndEnabled
                + "; planesEnabled=" + planeManager.isActiveAndEnabled;
            if (readiness != lastReadiness)
            {
                Debug.Log(LogPrefix + "Readiness: " + readiness, this);
                lastReadiness = readiness;
            }
            var touch = Touchscreen.current;
            if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame) return;
            Vector2 position = touch.primaryTouch.position.ReadValue();
            Debug.Log(LogPrefix + "Touch at " + position + "; state=" + State, this);
            if (State != PlacementState.Scanning || ARSession.state != ARSessionState.SessionTracking ||
                !anchorReady || !raycastManager.isActiveAndEnabled || !planeManager.isActiveAndEnabled)
            {
                Debug.Log(LogPrefix + "Touch ignored: " + State + "; " + readiness, this);
                return;
            }
            // Explicit UI raycast avoids depending on the UI module's Update order.
            if (EventSystem.current == null)
            {
                feedback = "Input: EventSystem missing.";
                Debug.LogWarning(LogPrefix + feedback, this);
                return;
            }
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            if (uiHits.Count != 0)
            {
                Debug.Log(LogPrefix + "UI touch rejected: " + uiHits[0].gameObject.name, this);
                return;
            }
            if (!raycastManager.Raycast(position, hits, TrackableType.PlaneWithinPolygon))
            {
                feedback = "Surface: no plane hit. Scan slowly and retry.";
                Debug.Log(LogPrefix + "AR raycast miss; detected planes=" + planeManager.trackables.count, this);
                return;
            }
            Debug.Log(LogPrefix + "AR raycast hits=" + hits.Count, this);
            foreach (var hit in hits)
            {
                var plane = planeManager.GetPlane(hit.trackableId);
                Debug.Log(LogPrefix + "Plane " + hit.trackableId + ": " + (plane == null ? "missing"
                    : "alignment=" + plane.alignment + "; tracking=" + plane.trackingState
                        + "; upDot=" + Vector3.Dot(plane.transform.up, Vector3.up)), this);
                if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp ||
                    plane.trackingState != TrackingState.Tracking ||
                    Vector3.Dot(plane.transform.up, Vector3.up) < 0.9f) continue;
                State = PlacementState.PlacementPending;
                Debug.Log(LogPrefix + "Scanning -> PlacementPending; pose=" + hit.pose, this);
                feedback = "Placing SQL JOIN visualization...";
                PlaceAsync(hit.pose, ++operationVersion);
                break;
            }
            if (State == PlacementState.Scanning)
            {
                feedback = "Surface: no tracked upward horizontal plane.";
                Debug.Log(LogPrefix + "All raycast hits rejected by plane filter.", this);
            }
        }

        private async void PlaceAsync(Pose pose, int version)
        {
            ARAnchor anchor = null;
            SQLJoinVisualization visualization = null;
            bool committed = false;
            string stage = "Anchor";
            try
            {
                Debug.Log(LogPrefix + "Attempt " + version + ": anchor creation start.", this);
                var result = await anchorManager.TryAddAnchorAsync(pose);
                Debug.Log(LogPrefix + "Attempt " + version + ": anchor status=" + result.status, this);
                if (!result.status.IsSuccess())
                    throw new InvalidOperationException("AR anchor creation failed: " + result.status);
                anchor = result.value;
                if (anchor == null) throw new InvalidOperationException("Anchor success returned a null anchor.");
                if (this == null || !isActiveAndEnabled || version != operationVersion)
                {
                    Debug.Log(LogPrefix + "Attempt " + version + ": completion cancelled; cleaning anchor.");
                    return;
                }
                stage = "Tracking";
                if (ARSession.state != ARSessionState.SessionTracking)
                    throw new InvalidOperationException("Tracking was lost while creating the anchor. Try again.");

                stage = "Prefab";
                Debug.Log(LogPrefix + "Attempt " + version + ": instantiating prefab under anchor " + anchor.trackableId, this);
                visualization = Instantiate(visualizationPrefab, anchor.transform, false);
                Debug.Log(LogPrefix + "Attempt " + version + ": prefab instantiated.", this);
                visualization.transform.localPosition = Vector3.zero;
                visualization.transform.localScale = Vector3.one * Mathf.Max(0.01f, visualizationScale);
                // Text faces local -Z; point that side toward the camera, keeping the board upright.
                Vector3 awayFromCamera = Vector3.ProjectOnPlane(anchor.transform.position - arCamera.transform.position, Vector3.up);
                if (awayFromCamera.sqrMagnitude < 0.0001f)
                    awayFromCamera = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
                if (awayFromCamera.sqrMagnitude < 0.0001f) awayFromCamera = Vector3.forward;
                visualization.transform.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
                stage = "Binding";
                Debug.Log(LogPrefix + "Attempt " + version + ": BindVisualization start.", this);
                bool bound = controller.BindVisualization(visualization);
                Debug.Log(LogPrefix + "Attempt " + version + ": BindVisualization=" + bound, this);
                if (!bound)
                    throw new InvalidOperationException("SQL JOIN visualization binding failed.");

                placedAnchor = anchor;
                placedVisualization = visualization;
                State = PlacementState.Placed;
                committed = true;
                Debug.Log(LogPrefix + "Attempt " + version + ": PlacementPending -> Placed.", this);
            }
            catch (Exception exception)
            {
                if (this != null)
                {
                    feedback = stage + " failed. Tap surface to retry; see device logs.";
                    Debug.LogError(LogPrefix + "Attempt " + version + " failed at " + stage, this);
                }
                Debug.LogException(exception);
            }
            finally
            {
                if (!committed)
                {
                    if (visualization != null)
                    {
                        if (controller != null) controller.UnbindVisualization();
                        visualization.gameObject.SetActive(false);
                        Destroy(visualization.gameObject);
                    }
                    RemoveAnchor(anchor);
                    if (this != null && version == operationVersion)
                    {
                        State = PlacementState.Scanning;
                        Debug.Log(LogPrefix + "Attempt " + version + ": cleanup complete -> Scanning.", this);
                    }
                }
            }
        }

        private void RemoveAnchor(ARAnchor anchor)
        {
            if (anchor == null) return;
            try
            {
                if (anchorManager != null && anchorManager.subsystem != null && anchorManager.subsystem.running &&
                    anchorManager.TryRemoveAnchor(anchor)) return;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Anchor cleanup failed; destroying anchor object.");
                Debug.LogException(exception);
            }
            if (anchor != null) Destroy(anchor.gameObject);
        }

        private void OnDisable()
        {
            // Invalidate pending async completion; it will remove its own temporary anchor.
            operationVersion++;
            if (State == PlacementState.PlacementPending) State = PlacementState.Scanning;
        }

        private void OnDestroy()
        {
            if (placedVisualization != null)
            {
                if (controller != null) controller.UnbindVisualization();
                Destroy(placedVisualization.gameObject);
            }
            RemoveAnchor(placedAnchor);
        }

        private void OnGUI()
        {
            if (State == PlacementState.Placed) return;
            GUI.Label(new Rect(16, Screen.height - 42, Screen.width - 32, 38), feedback);
        }
    }
}
