
using UnityEngine;

public class DatabaseCell : MonoBehaviour
{
    public string cellName = "Student_ID";
    public bool isPrimaryKey = true;

    private Renderer cellRenderer;
    private Color originalColor;

    void Start()
    {
        cellRenderer = GetComponent<Renderer>();

        if (cellRenderer != null)
        {
            originalColor = cellRenderer.material.color;
        }
    }

    public void SelectCell()
    {
        Debug.Log("Selected cell: " + cellName);

        if (isPrimaryKey)
        {
            Debug.Log("This is a Primary Key. It uniquely identifies each record.");
        }

        if (cellRenderer != null)
        {
            cellRenderer.material.color = Color.green;
        }
    }

    public void ResetCell()
    {
        if (cellRenderer != null)
        {
            cellRenderer.material.color = originalColor;
        }
    }
}
