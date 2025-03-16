using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class ControlUI : MonoBehaviour
{
    private Button _createNew, _createConvert, _createCancel;

    private VisualElement _createPopup;

    private VisualElement _popup;
    private ProgressBar _progressBar;

    private VisualElement _progressPopup;
    private VisualElement _root;

    private ShowController _showManager;
    private VisualElement _warningPopup;
    private Button _warningProceed, _warningCancel;

    private bool _warningResult; // Track the result
    private void Awake()
    {
        _showManager = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
    }

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;

        _popup = _root.Q<VisualElement>("Popup");
        _popup.visible = false;
        _warningPopup = _popup.Q<VisualElement>("Warning");
        _warningPopup.visible = false;
        _warningProceed = _warningPopup.Q<Button>("WarningProceed");
        _warningCancel = _warningPopup.Q<Button>("WarningCancel");

        _createPopup = _popup.Q<VisualElement>("Create");
        _createNew = _createPopup.Q<Button>("New");
        _createConvert = _createPopup.Q<Button>("Convert");
        _createCancel = _createPopup.Q<Button>("Cancel");

        _progressPopup = _popup.Q<VisualElement>("Progress");
        _progressBar = _progressPopup.Q<ProgressBar>("ProgressBar");
    }

    public void DisplayWarning(string content, Action<bool> callback)
    {
        _warningPopup.Q<Label>("Content").text = content;
        _popup.visible = true;
        _warningPopup.visible = true;

        _warningProceed.clicked += () =>
        {
            _popup.visible = false;
            _warningPopup.visible = false;
            callback(true);
        };

        _warningCancel.clicked += () =>
        {
            _popup.visible = false;
            _warningPopup.visible = false;
            callback(false);
        };
    }

    public void DisplayConvert()
    {
        
        _popup.visible = true;
        _createPopup.visible = true;

        _createCancel.clicked += () =>
        {
            _popup.visible = false;
            _createPopup.visible = false;
        };

        _createNew.clicked += () =>
        {
            _popup.visible = false;
            _createPopup.visible = false;
            _showManager.New();
        };

        _createConvert.clicked += () =>
        {
            _popup.visible = false;
            _createPopup.visible = false;
            _showManager.Convert();
        };
    }

    public void UpdateProgress(float progress)
    {
        if (progress > 0)
        {
            _popup.visible = true;
            _progressPopup.visible = true;
            _progressBar.value = progress;
        }
        else
        {
            _popup.visible = false;
            _progressPopup.visible = false;
        }
    }
}