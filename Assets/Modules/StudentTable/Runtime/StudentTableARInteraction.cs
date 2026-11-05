using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

// Isolated Module 1 AR input: original educational content and DatabaseCell behavior.
public sealed class StudentTableARInteraction : MonoBehaviour
{
    [SerializeField] private Camera arCamera;
    [SerializeField] private TMP_Text infoText;
    private Transform tableRoot;
    private DatabaseCell selectedCell;
    private int bindFrame;
    private Material textMaterial;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Start()
    {
        if (arCamera == null || infoText == null)
        {
            Debug.LogError("Assign Module 1 AR camera and InfoText.", this);
            enabled = false;
            return;
        }
        textMaterial = new Material(infoText.fontSharedMaterial);
        textMaterial.SetColor("_FaceColor", Color.white);
        infoText.fontSharedMaterial = textMaterial;
        infoText.color = new Color(0.07f, 0.12f, 0.16f);
        infoText.text = "Scan a horizontal surface, then tap to place the Student Table.";
    }

    public bool BindTable(Transform root)
    {
        if (root == null || arCamera == null || infoText == null || !isActiveAndEnabled) return false;
        ClearSelection();
        tableRoot = root;
        bindFrame = Time.frameCount;
        if (infoText != null)
            infoText.text = "Tap a cell in the Student Table to learn about primary keys, records, and attributes.";
        return true;
    }

    public void UnbindTable()
    {
        ClearSelection();
        tableRoot = null;
    }

    private void ClearSelection()
    {
        if (selectedCell != null) selectedCell.ResetCell();
        selectedCell = null;
    }

    private void Update()
    {
        if (tableRoot == null || arCamera == null || Time.frameCount <= bindFrame ||
            ARSession.state != ARSessionState.SessionTracking) return;
        var touch = Touchscreen.current;
        if (touch == null || !touch.primaryTouch.press.wasPressedThisFrame || EventSystem.current == null) return;
        Vector2 position = touch.primaryTouch.position.ReadValue();
        uiHits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, uiHits);
        if (uiHits.Count != 0) return;
        if (!Physics.Raycast(arCamera.ScreenPointToRay(position), out var hit)) return;
        var cell = hit.collider.GetComponentInParent<DatabaseCell>();
        if (cell == null || !cell.isActiveAndEnabled || !cell.transform.IsChildOf(tableRoot)) return;
        ClearSelection();
        cell.SelectCell();
        selectedCell = cell;
        ShowCellInformation(cell);
    }

    private void OnDestroy()
    {
        if (textMaterial != null) Destroy(textMaterial);
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
