using System;
using Global;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using Random = UnityEngine.Random;

/// <summary>
///     This script controls movements & more for the animatronic the script is attached to. Formerly known as
///     Character_Valves.cs
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class Animatronic : MonoBehaviour
{
    public enum DualPressureState
    {
        Off,
        Rockafire
    }

    public enum SoundStyle
    {
        Rockafire
    }

    public enum SpecialState
    {
        Off,
        Cyber,
        CyberSwinger
    }

    [Header("General")] [Range(0, 1)] public float PSIScale = 1;

    public Movement[] movements;

    public SoundStyle soundStyle;
    public bool valves;
    public bool squeaks;
    public bool airLeaks;

    [Header("Attributes")] public DualPressureState dualPressureState = DualPressureState.Off;

    [Header("Special State")] public SpecialState specialState = SpecialState.Off;

    public int headOutBit;
    public int headInBit;
    private AirCompressor _airCompressor;
    private AudioClip[] _airLeakAudios;

    [Header("Sounds")] private AudioSource _generalAudioSource;

    // Playables
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _mixer;
    private AnimationPlayableOutput _output;

    private ShowController _showController;
    private AudioClip[] _squeakAudios;
    private AudioClip[] _valveAudiosIn; // 0 should always exist, 1 is an alt sound
    private AudioSource _valveAudioSource;
    private AudioClip[] _valveAudiosOut; // 0 should always exist, 1 is an alt sound


    private Animator animator;

    private float headSwingAccel;

    private bool numeratorLoop;

    private void Awake()
    {
        if (movements.Length == 0)
            Debug.LogWarning(
                $"No movements assigned to {gameObject.name} so disabling. Please assign at least one in the inspector.");
        _showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
        _airCompressor = GameObject.FindGameObjectWithTag("Air Compressor").GetComponent<AirCompressor>();
    }

    private void Start()
    {
        transform.parent.name = transform.parent.name.Replace("(Clone)", "").Trim();

        Debug.Log("Startup Performed on " + name);

        animator = GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.CullCompletely;
        animator.runtimeAnimatorController = null;

        _valveAudioSource = new GameObject("ValveSounds").AddComponent<AudioSource>();
        _valveAudioSource.transform.SetParent(transform.parent);
        _valveAudioSource.transform.localPosition = new Vector3(0, -1, 0);
        _valveAudioSource.volume = 0.25f;
        _valveAudioSource.spatialBlend = 1;
        _valveAudioSource.rolloffMode = AudioRolloffMode.Linear;
        _valveAudioSource.minDistance = 0.1f;
        _valveAudioSource.maxDistance = 5;

        AudioLowPassFilter filter = _valveAudioSource.gameObject.AddComponent<AudioLowPassFilter>();
        filter.cutoffFrequency = 1000;
        filter.lowpassResonanceQ = 1f;

        _generalAudioSource = GetComponent<AudioSource>();
        _generalAudioSource.volume = 0.5f;
        _generalAudioSource.spatialBlend = 1;
        _generalAudioSource.maxDistance = 3;

        _valveAudiosIn = Resources.LoadAll<AudioClip>("Audio/Animatronics/" + soundStyle + "/Valve/In/");
        _valveAudiosOut = Resources.LoadAll<AudioClip>("Audio/Animatronics/" + soundStyle + "/Valve/Out/");
        _squeakAudios = Resources.LoadAll<AudioClip>("Audio/Animatronics/" + soundStyle + "/Squeak/");
        _airLeakAudios = Resources.LoadAll<AudioClip>("Audio/Animatronics/" + soundStyle + "/AirLeak/");

        SetupPlayables();
        _graph.Play();

        Debug.Log($"{gameObject.name} is Ready! Playables: {_mixer.GetInputCount()}");
    }

    private void Update()
    {
        if (_showController.active) CreateMovements(Time.deltaTime * _showController.updateRate);
    }

    private void OnDisable()
    {
        _graph.Destroy();
    }

    /// <summary>
    ///     Simulates the exact position for each animation layer of the character.
    ///     Thanks to Himitsu for completely overhauling the entire sim to improve
    ///     its efficency.
    /// </summary>
    /// <param name="timeDeltaTime"></param>
    public void CreateMovements(float timeDeltaTime)
    {
        if (_showController != null)
            for (int i = 0; i < movements.Length; i++)
            {
                if (specialState == SpecialState.Cyber || specialState == SpecialState.CyberSwinger)
                    while (i <= 1)
                    {
                        CreateCyberMovements(i, timeDeltaTime);
                        i++;
                    }

                //Assign PSI and Valve Position
                float currentValvePos = _mixer.GetInputWeight(i);
                float valvePsi = _airCompressor.airPressure / 1050f * PSIScale * timeDeltaTime;

                bool state;
                bool dpr = false;
                if (movements[i].drawer == Drawer.Top)
                {
                    state = _showController.topDrawer[movements[i].bit - 1];
                    switch (dualPressureState)
                    {
                        case DualPressureState.Off:
                            valvePsi *= 0.65f;
                            break;
                        case DualPressureState.Rockafire:
                            dpr = _showController.topDrawer[40];
                            valvePsi *= 0.75f;
                            break;
                    }
                }
                else
                {
                    state = _showController.bottomDrawer[movements[i].bit - 1];
                    switch (dualPressureState)
                    {
                        case DualPressureState.Off:
                            valvePsi *= 0.65f;
                            break;
                        case DualPressureState.Rockafire:
                            dpr = _showController.bottomDrawer[60];
                            break;
                    }
                }

                float smash;
                float smashSpeed;
                if (state)
                {
                    //Outwards Movement Calculation
                    movements[i].weightOut = Mathf.Min(
                        movements[i].weightOut + movements[i].flowControlOut * movements[i].flowControlOut / 2f,
                        1f + (1f - movements[i].flowControlOut) * 0.3f);
                    movements[i].weightIn = 0f;
                    currentValvePos += valvePsi * movements[i].flowControlOut * movements[i].weightOut *
                                       movements[i].gravityScaleOut *
                                       movements[i].invert;
                    smash = movements[i].smashOut;
                    smashSpeed = movements[i].smashSpeedOut;
                }
                else
                {
                    //Inwards Movement Calculation
                    movements[i].weightIn = Mathf.Min(
                        movements[i].weightIn + movements[i].flowControlIn * movements[i].flowControlIn / 2f,
                        1f + (1f - movements[i].flowControlIn) * 0.3f);
                    movements[i].weightOut = 0f;
                    currentValvePos -= valvePsi * movements[i].flowControlIn * movements[i].weightIn *
                                       movements[i].gravityScale * movements[i].invert;
                    smash = movements[i].smashIn;
                    smashSpeed = movements[i].smashSpeedIn;
                }

                if (movements[i].smashState != state)
                {
                    movements[i].smashState = state;
                    movements[i].smashIteration = 1;
                    movements[i].invert = 1;
                }

                //Smash Calculation
                if (movements[i].invert == 1)
                    movements[i].invert = Mathf.Min(movements[i].invert + timeDeltaTime * smashSpeed, 1f);
                if (currentValvePos < 0 && smash != 0 && movements[i].smashIteration < 4)
                {
                    movements[i].invert =
                        -smash * (Mathf.Abs(currentValvePos + 1) / movements[i].smashIteration / timeDeltaTime);
                    movements[i].smashState = state;
                    movements[i].smashIteration++;
                }

                if (currentValvePos > 1 && smash != 0 && movements[i].smashIteration < 4)
                {
                    movements[i].invert = -smash * (currentValvePos / movements[i].smashIteration / timeDeltaTime);
                    movements[i].smashState = state;
                    movements[i].smashIteration++;
                }

                //Final Value
                currentValvePos = Mathf.Min(Mathf.Max(currentValvePos, 0f), 1f);
                HandleSounds(i);
                _mixer.SetInputWeight(i, currentValvePos);
            }
    }


    /// <summary>
    ///     A special simulation for Cyberamic characters, as their heads and necks
    ///     are the only outlier to the normal pushing in and out of most
    ///     animatronic movements.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="timeDeltatime"></param>
    public void CreateCyberMovements(int e, float timeDeltatime)
    {
        /* Will figure out soon.
        bool state;
        bool nothing = true;
        bool dpr = false;
        float currentAnimState = characterValves.GetFloat(hash);
        float finalPSIScale = bitChart.PSI / 1050f * PSIScale;

        if (headSwingAccel > 0)
            headSwingAccel = Mathf.Clamp(headSwingAccel - Time.deltaTime * 0.055f, 0, 0.1f);
        else
            headSwingAccel = Mathf.Clamp(headSwingAccel + Time.deltaTime * 0.055f, -0.1f, 0);


        //Check which bit
        if (e == 0)
        {
            state = bitChart.topDrawer[headOutBit - 1];
            if (state) nothing = false;
            //Recheck
            if (currentAnimState == 0f && state)
            {
                i = 1;
                hash = cylParamHash[i];
                i = cylParamId[i];
                currentAnimState = characterValves.GetFloat(hash);
                state = false;
            }
            else
            {
                state = false;
            }
        }
        else
        {
            state = bitChart.topDrawer[headInBit - 1];
            if (state) nothing = false;
            if (!state && specialState == SpecialState.CyberSwinger)
            {
                i = 1;
                hash = cylParamHash[i];
                i = cylParamId[i];
                currentAnimState = characterValves.GetFloat(hash);
                movements[i].weightIn = Mathf.Min(movements[i].weightIn + movements[i].flowControlIn * movements[i].flowControlIn / 2f,
                    1f + (1f - movements[i].flowControlIn) * 0.3f * timeDeltatime);
                movements[i].weightOut = 0f;
                currentAnimState -=
                    finalPSIScale * movements[i].flowControlIn * movements[i].weightIn * timeDeltatime * movements[i].gravityScale;
            }

            //Recheck
            if (currentAnimState == 1f && state)
            {
                i = 1;
                hash = cylParamHash[i];
                i = cylParamId[i];
                currentAnimState = characterValves.GetFloat(hash);
            }
        }

        //Apply Anims
        if (currentAnimState == 0f && !state)
        {
            characterValves.SetLayerWeight(i, 0f);
        }
        else if (currentAnimState != 1f || !state)
        {
            characterValves.SetLayerWeight(i, 1f);
            if (dpr) finalPSIScale *= 0.75f;
            if (state && !nothing)
            {
                movements[i].weightOut = Mathf.Min(movements[i].weightOut + movements[i].flowControlOut * movements[i].flowControlOut / 2f,
                    1f + (1f - movements[i].flowControlOut) * 0.3f * timeDeltatime);
                movements[i].weightIn = 0f;
                currentAnimState += finalPSIScale * movements[i].flowControlOut * movements[i].weightOut * timeDeltatime *
                                    movements[i].gravityScaleOut;
                headSwingAccel += finalPSIScale * movements[i].flowControlOut * movements[i].weightOut * timeDeltatime *
                                  movements[i].gravityScaleOut;
            }
            else if (!state && !nothing)
            {
                movements[i].weightIn = Mathf.Min(movements[i].weightIn + movements[i].flowControlIn * movements[i].flowControlIn / 2f,
                    1f + (1f - movements[i].flowControlIn) * 0.3f * timeDeltatime);
                movements[i].weightOut = 0f;
                currentAnimState -=
                    finalPSIScale * movements[i].flowControlIn * movements[i].weightIn * timeDeltatime * movements[i].gravityScale;
                headSwingAccel -= finalPSIScale * movements[i].flowControlIn * movements[i].weightIn * timeDeltatime * movements[i].gravityScale;
            }
        }

        currentAnimState += headSwingAccel * 0.06f;
        currentAnimState = Mathf.Min(Mathf.Max(currentAnimState, 0f), 1f);
        characterValves.SetFloat(hash, currentAnimState);
        */
    }

    /// <summary>
    ///     Sets up the graph, mixer, output and clips for the new Playables system
    ///     Playables is a dynamic replacement for manually creating AnimationControllers for animatronics
    /// </summary>
    public void SetupPlayables()
    {
        if (movements == null || movements.Length == 0) return;

        _graph = PlayableGraph.Create();
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _mixer = AnimationLayerMixerPlayable.Create(_graph, movements.Length);
        _output = AnimationPlayableOutput.Create(_graph, gameObject.name, GetComponent<Animator>());
        _output.SetSourcePlayable(_mixer);

        for (int i = 0; i < movements.Length; i++)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(_graph, movements[i].animation);
            playable.SetApplyFootIK(false);
            playable.SetApplyPlayableIK(false);
            movements[i].Playable = playable;

            _graph.Connect(playable, 0, _mixer, i);
            _mixer.SetInputWeight(i, 0);
            _mixer.SetLayerAdditive((uint)i, true);
        }
    }


    private void HandleSounds(int i)
    {
        if (!valves) return;

        bool isTopDrawer = movements[i].drawer == Drawer.Top;
        bool currentState = isTopDrawer
            ? _showController.topDrawer[movements[i].bit]
            : _showController.bottomDrawer[movements[i].bit];

        if (currentState && !movements[i].previousState) // Valve In
        {
            if (_valveAudiosIn.Length > 0)
            {
                _valveAudioSource.PlayOneShot(_valveAudiosIn[Random.Range(0, _valveAudiosIn.Length)]);
                movements[i].mid = false;
            }
        }
        else if (currentState && movements[i].previousState)
        {
            if (!movements[i].mid && squeaks) // Squeaks
            {
                movements[i].mid = true;
                if (_squeakAudios.Length > 0)
                    if (Random.Range(0, 50) == 0) // Random chance for a squeak!
                    {
                        _generalAudioSource.pitch = Random.Range(0.2f, 0.6f);
                        _generalAudioSource.PlayOneShot(_squeakAudios[Random.Range(0, _squeakAudios.Length)]);
                    }
            }
        }
        else if (!currentState && movements[i].previousState) // Valve Out
        {
            if (_valveAudiosOut.Length > 0)
            {
                _valveAudioSource.PlayOneShot(_valveAudiosOut[Random.Range(0, _valveAudiosOut.Length)]);
                movements[i].mid = false;
            }
        }

        // update the previous state
        movements[i].previousState = currentState;
    }
}

/// <summary>
///     This contains all the data about the animatronic's movement. Replaces the deprecated arrays in the Animatronic.cs
///     (formerly known as Character_Valves.cs) script
/// </summary>
[Serializable]
public class Movement
{
    [Header("General")] public string name;

    public AnimationClip animation;
    public int bit;
    public Drawer drawer;

    [Header("Flows")] [Range(0, 4)] public float flowControlOut = 1f;

    [Range(0, 4)] public float flowControlIn = 1f;

    [Space(10)] [Range(0, 2)] public float gravityScale = 1f;

    [Range(0, 2)] public float gravityScaleOut = 1f;

    [Space(10)] [Range(0, 1)] public float smashOut;

    [Range(0, 1)] public float smashIn;
    [Range(0, 1)] public float smashSpeedOut;
    [Range(0, 1)] public float smashSpeedIn;
    [HideInInspector] public int smashIteration;
    [HideInInspector] public bool smashState;
    [HideInInspector] [Range(0, 1)] public float weightOut;
    [HideInInspector] [Range(0, 1)] public float weightIn;

    [Space(10)]
    // Genuinely have no clue why this has to be a float instead of a bool.
    // The code is just too confusing, so I'm leaving it here as a float
    // unless someone wants to try turn it into a bool
    [HideInInspector]
    public float invert;

    // Sounds
    [HideInInspector] public bool previousState; // If it's already been played or not
    [HideInInspector] public bool mid; // If it's in the middle of a movement, used for air leaks & squeaks
    public AnimationClipPlayable Playable;
}