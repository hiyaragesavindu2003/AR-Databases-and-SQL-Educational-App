
using UnityEngine;
using UnityEngine.InputSystem;

public class DatabaseInteractionManager : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void Update()
    {
        if (mainCamera == null)
            return;

        Vector2 screenPosition;

        // Android touchscreen
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();
        }
        // PC mouse
        else if (Mouse.current != null &&
                 Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
        }
        else
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Touched object: " + hit.collider.name);

            DatabaseCell cell =
                hit.collider.GetComponent<DatabaseCell>();

            if (cell != null)
            {
                cell.SelectCell();
            }
            else
            {
                Debug.Log("Object has no DatabaseCell script.");
            }
        }
        else
        {
            Debug.Log("No database cell detected.");
        }
    }
}
