
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

public class ARTablePlacement : MonoBehaviour
{
    public GameObject tablePrefab;
    public ARRaycastManager raycastManager;

    private GameObject placedTable;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Update()
    {
        if (placedTable != null)
            return;

        if (Touchscreen.current == null ||
            !Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return;

        Vector2 touchPosition =
            Touchscreen.current.primaryTouch.position.ReadValue();

        if (raycastManager.Raycast(
            touchPosition,
            hits,
            TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            placedTable = Instantiate(
                tablePrefab,
                hitPose.position,
                hitPose.rotation
            );

            Debug.Log("Student Table placed successfully!");
        }
    }
}
