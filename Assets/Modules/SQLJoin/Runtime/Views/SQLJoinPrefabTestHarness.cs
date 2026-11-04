using System.Collections;
using UnityEngine;

namespace ARSQLExplorer.SQLJoin
{
    // Desktop-only test controls. Does not implement JOIN or playback behavior.
    public sealed class SQLJoinPrefabTestHarness : MonoBehaviour
    {
        [SerializeField] private SQLJoinVisualization visualizationPrefab;
        [SerializeField] private SQLJoinController controller;
        private SQLJoinVisualization instance;
        private bool ready;
        private bool transformed;
        private string message = "Waiting for controller UI...";

        private IEnumerator Start()
        {
            // All scene Start methods complete before instantiation/binding.
            yield return null;
            if (visualizationPrefab == null || controller == null)
            {
                Debug.LogError("Prefab test requires a visualization prefab and controller.", this);
                yield break;
            }
            ready = true;
            Bind();
        }

        public void Bind()
        {
            if (!ready) return;
            if (instance == null)
            {
                instance = Instantiate(visualizationPrefab);
                instance.name = "SQLJoinVisualization_RuntimeTest";
                ApplyTransform();
            }
            message = controller.BindVisualization(instance)
                ? "Bound: one prefab instance. JOIN controls ready."
                : "Binding failed; inspect Console.";
        }

        public void Unbind()
        {
            if (!ready) return;
            controller.UnbindVisualization();
            message = "Unbound: tables retained; playback disabled. Bind / Rebind to continue.";
        }

        public void ToggleTransform()
        {
            if (instance == null) return;
            transformed = !transformed;
            ApplyTransform();
            message = transformed ? "Translated, rotated, and uniformly scaled. Try Next Step."
                : "Original test transform restored.";
        }

        private void ApplyTransform()
        {
            // Compensate for the prefab's bottom-center pivot for desktop framing.
            instance.transform.SetPositionAndRotation(
                transformed ? new Vector3(1.1f, -4.5f, 0.2f) : new Vector3(0.8f, -5.72f, 0),
                transformed ? Quaternion.Euler(8, 18, 4) : Quaternion.identity);
            instance.transform.localScale = Vector3.one * (transformed ? 0.78f : 1f);
        }

        private void OnGUI()
        {
            var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(Screen.width / 1920f, Screen.height / 1080f, 1));
            GUI.Box(new Rect(40, 215, 1840, 44), "");
            GUI.enabled = ready;
            if (GUI.Button(new Rect(50, 222, 190, 30), "Bind / Rebind")) Bind();
            if (GUI.Button(new Rect(250, 222, 160, 30), "Unbind")) Unbind();
            GUI.enabled = ready && instance != null;
            if (GUI.Button(new Rect(420, 222, 240, 30), "Toggle Transform")) ToggleTransform();
            GUI.enabled = true;
            GUI.Label(new Rect(680, 226, 1180, 30), message);
            GUI.matrix = oldMatrix;
        }

        private void OnDestroy()
        {
            if (controller != null) controller.UnbindVisualization();
            if (instance != null) Destroy(instance.gameObject);
        }
    }
}
