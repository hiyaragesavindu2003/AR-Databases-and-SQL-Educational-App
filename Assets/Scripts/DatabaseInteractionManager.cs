
using UnityEngine;
using UnityEngine.InputSystem;

public class DatabaseInteractionManager : MonoBehaviour
{
    private Camera arCamera;
    private DatabaseCell selectedCell;

    void Start()
    {
        arCamera = Camera.main;

        if (arCamera == null)
            Debug.LogError("AR Camera not found!");
        else
            Debug.Log("Using camera: " + arCamera.name);
    }

    void Update()
    {
        if (arCamera == null)
            return;

        Vector2 screenPosition;

        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();

            Debug.Log("ANDROID TOUCH DETECTED");
        }
        else if (Mouse.current != null &&
                 Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition =
                Mouse.current.position.ReadValue();

            Debug.Log("MOUSE CLICK DETECTED");
        }
        else
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Hit object: " + hit.collider.name);

            DatabaseCell cell =
                hit.collider.GetComponentInParent<DatabaseCell>();

            if (cell != null)
            {
                if (selectedCell != null)
                    selectedCell.ResetCell();

                cell.SelectCell();
                selectedCell = cell;

                Debug.Log("CELL SELECTED: " + cell.cellName);
            }
            else
            {
                Debug.LogWarning("Hit object has no DatabaseCell.");
            }
        }
        else
        {
            Debug.LogWarning("Touch raycast missed all colliders.");
        }
    }
}
