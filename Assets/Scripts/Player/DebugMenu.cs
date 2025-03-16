using UnityEngine;
using UnityEngine.InputSystem;

public class DebugMenu : MonoBehaviour
{
    private bool _showDebug;
    private InputAction debugAction;

    private void Start()
    {
        debugAction = InputSystem.actions.FindAction("Debug");
    }

    private void Update()
    {
        if (debugAction.triggered) _showDebug = !_showDebug;
    }

    private void OnGUI()
    {
        if (_showDebug) GUILayout.Label($"{Mathf.Round(1.0f / Time.deltaTime)}FPS [F1]");
    }
}