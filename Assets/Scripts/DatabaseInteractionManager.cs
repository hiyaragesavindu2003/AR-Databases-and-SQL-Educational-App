
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DatabaseInteractionManager : MonoBehaviour
{
    private Camera arCamera;
    private DatabaseCell selectedCell;

    [Header("Educational Information UI")]
    public TMP_Text infoText;

    void Start()
    {
        arCamera = Camera.main;

        if (arCamera == null)
            Debug.LogError("AR Camera not found!");
        else
            Debug.Log("Using camera: " + arCamera.name);

        if (infoText != null)
        {
            infoText.text =
                "Tap a cell in the Student Table to learn about " +
                "primary keys, records, and attributes.";
        }
    }

    void Update()
    {
        if (arCamera == null)
            return;

        Vector2 screenPosition;

        // Android touchscreen
        if (Touchscreen.current != null &&
            Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition =
                Touchscreen.current.primaryTouch.position.ReadValue();

            Debug.Log("ANDROID TOUCH DETECTED");
        }
        // PC mouse
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
            DatabaseCell cell =
                hit.collider.GetComponentInParent<DatabaseCell>();

            if (cell != null)
            {
                if (selectedCell != null)
                    selectedCell.ResetCell();

                cell.SelectCell();
                selectedCell = cell;

                ShowCellInformation(cell);

                Debug.Log("CELL SELECTED: " + cell.cellName);
            }
        }
    }

    void ShowCellInformation(DatabaseCell cell)
    {
        if (infoText == null)
        {
            Debug.LogWarning("InfoText is not assigned!");
            return;
        }

        string cellName = cell.cellName;

        if (cell.isPrimaryKey)
        {
            infoText.text =
                "Selected: " + cellName + "\n\n" +
                "PRIMARY KEY\n" +
                "A primary key uniquely identifies each record " +
                "in a relational database table.";
        }
        else
        {
            infoText.text =
                "Selected: " + cellName + "\n\n" +
                "RELATIONAL DATABASE\n" +
                "This is a cell in the Students table. " +
                "Each row represents a student record, " +
                "and each column represents an attribute.";
        }
    }
}
