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
            if (State != PlacementState.Scanning || ARSession.state != ARSessionState.SessionTracking ||
                anchorManager.subsystem == null || !anchorManager.subsystem.running ||
                !raycastManager.isActiveAndEnabled || !planeManager.isActiveAndEnabled) return;
            var touch = Touchscreen.current;
            if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame) return;
            Vector2 position = touch.primaryTouch.position.ReadValue();
            // Explicit UI raycast avoids depending on the UI module's Update order.
            if (EventSystem.current == null) return;
            var pointer = new PointerEventData(EventSystem.current) { position = position };
            uiHits.Clear();
            EventSystem.current.RaycastAll(pointer, uiHits);
            if (uiHits.Count != 0) return;
            if (!raycastManager.Raycast(position, hits, TrackableType.PlaneWithinPolygon)) return;
            foreach (var hit in hits)
            {
                var plane = planeManager.GetPlane(hit.trackableId);
                if (plane == null || plane.alignment != PlaneAlignment.HorizontalUp ||
                    plane.trackingState != TrackingState.Tracking ||
                    Vector3.Dot(plane.transform.up, Vector3.up) < 0.9f) continue;
                State = PlacementState.PlacementPending;
                feedback = "Placing SQL JOIN visualization...";
                PlaceAsync(hit.pose, ++operationVersion);
                break;
            }
        }

        private async void PlaceAsync(Pose pose, int version)
        {
            ARAnchor anchor = null;
            SQLJoinVisualization visualization = null;
            bool committed = false;
            try
            {
                var result = await anchorManager.TryAddAnchorAsync(pose);
                anchor = result.value;
                if (!result.status.IsSuccess() || anchor == null)
                    throw new InvalidOperationException("AR anchor creation failed: " + result.status);
                if (this == null || !isActiveAndEnabled || version != operationVersion) return;
                if (ARSession.state != ARSessionState.SessionTracking)
                    throw new InvalidOperationException("Tracking was lost while creating the anchor. Try again.");

                visualization = Instantiate(visualizationPrefab, anchor.transform, false);
                visualization.transform.localPosition = Vector3.zero;
                visualization.transform.localScale = Vector3.one * Mathf.Max(0.01f, visualizationScale);
                // Text faces local -Z; point that side toward the camera, keeping the board upright.
                Vector3 awayFromCamera = Vector3.ProjectOnPlane(anchor.transform.position - arCamera.transform.position, Vector3.up);
                if (awayFromCamera.sqrMagnitude < 0.0001f)
                    awayFromCamera = Vector3.ProjectOnPlane(arCamera.transform.forward, Vector3.up);
                if (awayFromCamera.sqrMagnitude < 0.0001f) awayFromCamera = Vector3.forward;
                visualization.transform.rotation = Quaternion.LookRotation(awayFromCamera, Vector3.up);
                if (!controller.BindVisualization(visualization))
                    throw new InvalidOperationException("SQL JOIN visualization binding failed.");

                placedAnchor = anchor;
                placedVisualization = visualization;
                State = PlacementState.Placed;
                committed = true;
            }
            catch (Exception exception)
            {
                if (this != null)
                {
                    feedback = "Placement failed. Tap a tracked horizontal surface to retry.";
                    Debug.LogWarning("SQL JOIN placement: " + exception.Message, this);
                }
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
                    if (this != null && version == operationVersion) State = PlacementState.Scanning;
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
            catch (Exception exception) { Debug.LogWarning("Anchor cleanup: " + exception.Message); }
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
