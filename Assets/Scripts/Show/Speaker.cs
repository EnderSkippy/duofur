using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Speaker : MonoBehaviour
{
    private ShowController _showController;
    private AudioSource audioSource;

    private void Start()
    {
        _showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!_showController || !audioSource) return;

        if (_showController.playing)
        {
            audioSource.clip = _showController.referenceAudio.clip;
            audioSource.volume = _showController.referenceAudio.volume;
            audioSource.time = _showController.referenceAudio.time;
        }
    }

    private void LateUpdate()
    {
        audioSource.enabled = _showController.referenceAudio.enabled;
    }
}