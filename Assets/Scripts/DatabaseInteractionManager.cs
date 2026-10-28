
using UnityEngine;
using UnityEngine.InputSystem;

public class DatabaseInteractionManager : MonoBehaviour
{
    private Camera testCamera;

    void Start()
    {
        testCamera = Camera.main;

        if (testCamera == null)
            Debug.LogError("No MainCamera found!");
        else
            Debug.Log("Camera found: " + testCamera.name);
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("Mouse clicked!");

            if (testCamera == null)
                return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = testCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Ray hit: " + hit.collider.name);

                DatabaseCell cell =
                    hit.collider.GetComponent<DatabaseCell>();

                if (cell != null)
                {
                    cell.SelectCell();
                }
                else
                {
                    Debug.LogWarning(
                        "Object has no DatabaseCell script."
                    );
                }
            }
            else
            {
                Debug.LogWarning("Ray hit nothing!");
            }
        }
    }
}
