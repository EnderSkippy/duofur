using Show;
using UnityEngine;
using UnityEngine.UIElements;

public class PlaybackUI : MonoBehaviour
{
    public Texture2D PlayIcon, PauseIcon;

    private ControlUI _controlUI;
    private Button _createButton, _playbackButton, _reverseButton, _stopButton, _forwardButton, _curtainButton;
    private Slider _dataSlider, _volumeSlider;

    private VisualElement _playback, _tools;

    private VisualElement _root, _bottomBar;

    private ShowController _showController;

    private void Awake()
    {
        _showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
        _controlUI = GetComponent<ControlUI>();

        _root = GetComponent<UIDocument>().rootVisualElement;

        _bottomBar = _root.Q<VisualElement>("BottomBar");
        _playback = _bottomBar.Q<VisualElement>("Playback");
        _tools = _bottomBar.Q<VisualElement>("Tools");

        _createButton = _tools.Q<Button>("Create");
        _playbackButton = _playback.Q<Button>("TogglePlayback");
        _reverseButton = _root.Q<Button>("ReverseShowtape");
        _stopButton = _root.Q<Button>("StopShowtape");
        _forwardButton = _root.Q<Button>("ForwardShowtape");
        _curtainButton = _root.Q<Button>("CurtainToggle");

        _dataSlider = _root.Q<Slider>("DataSlider");
        _volumeSlider = _root.Q<Slider>("VolumeSlider");

        _createButton.clicked += () => _controlUI.DisplayConvert();
        _playbackButton.clicked += () => _showController.TogglePlayback();
        _curtainButton.clicked += () => ToggleCurtains();
        _reverseButton.clicked += () => _showController.FFSong(-1);
        _stopButton.clicked += () => _showController.Stop();
        _forwardButton.clicked += () => _showController.FFSong(1);

        _dataSlider.RegisterValueChangedCallback(evt => UpdateDataTime(evt.newValue));
        _volumeSlider.RegisterValueChangedCallback(evt => UpdateVolume(evt.newValue));
    }

    private void Update()
    {
        if (_showController)
        {
            UpdatePlaybackActivity();
            if (_showController.playing)
            {
                if (_showController.referenceAudio.clip.length > 0)
                {
                    _dataSlider.value = _showController.referenceAudio.time /
                        _showController.referenceAudio.clip.length * 100;
                    _volumeSlider.value = _showController.referenceAudio.volume * 100;
                }

                if (_showController.referenceAudio.isPlaying)
                    _playbackButton.iconImage = PauseIcon;
                else
                    _playbackButton.iconImage = PlayIcon;
            }
            else
            {
                _playbackButton.iconImage = PlayIcon;
                _dataSlider.value = 0;
            }
        }
    }

    public void ToggleUI(bool toggle)
    {
        float targetPos = toggle ? 0 : -100;
        float targetAlpha = toggle ? 1f : 0f;
        LeanTweenType easeType = toggle ? LeanTweenType.easeOutQuad : LeanTweenType.easeInQuad;

        LeanTween.value(gameObject, _bottomBar.style.bottom.value.value, targetPos, 0.25f)
            .setOnUpdate(val => _bottomBar.style.bottom = val)
            .setEase(easeType);

        LeanTween.value(gameObject, _bottomBar.style.opacity.value, targetAlpha, 0.15f)
            .setOnUpdate(val => _bottomBar.style.opacity = val)
            .setEase(easeType);
    }

    /// <summary>
    ///     Updates the playback position of the showtape via the data slider
    /// </summary>
    /// <param name="value"></param>
    private void UpdateDataTime(float value)
    {
        if (_showController.playing)
        {
            if (_showController.referenceAudio.clip.length > 0)
            {
                _showController.referenceAudio.time = value / 100 * _showController.referenceAudio.clip.length;

                if (_showController.videoPath != null)
                    _showController.referenceVideo.time = value / 100 * _showController.referenceAudio.clip.length;
            }
        }
    }

    /// <summary>
    ///     Updates the volume of the show speaker via the volume slider
    /// </summary>
    /// <param name="value"></param>
    private void UpdateVolume(float value)
    {
        _showController.referenceAudio.volume = value / 100;
    }

    /// <summary>
    ///     Updates the Enabled properties of most interactive playback elements depending on if the show is playing or not
    /// </summary>
    private void UpdatePlaybackActivity()
    {
        bool isPlaying = _showController.playing;
        _reverseButton.SetEnabled(isPlaying);
        _stopButton.SetEnabled(isPlaying);
        _forwardButton.SetEnabled(isPlaying);
        _dataSlider.SetEnabled(isPlaying);
        _volumeSlider.SetEnabled(isPlaying);
    }


    private void ToggleCurtains()
    {
        // Tacky solution, but it'll do assuming all curtains in the scene have the same state.
        Curtains curtain = transform.root.GetComponentInChildren<Curtains>();

        if (curtain != null)
        {
            if (curtain.curtainOverride)
            {
                curtain.curtainOverride = false;
                _curtainButton.text = "Open Curtains";
            }
            else
            {
                curtain.curtainOverride = true;
                _curtainButton.text = "Close Curtains";
            }
        }
    }
}