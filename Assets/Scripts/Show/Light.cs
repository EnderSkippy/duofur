using System;
using Global;
using UnityEngine;
using UnityEngine.Serialization;

namespace Show
{
    /// <summary>
    ///     Light controller script. This allows many properties of a Light to be manipulated by the show controller.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class Light : MonoBehaviour
    {
        public enum LightMode
        {
            Normal,
            Strobe,
            Flash
        }

        public enum SpecialMode
        {
            None = 0,
            Helicopter,
            CuStar
        }

        [Header("General Settings")] [FormerlySerializedAs("lightBit")]
        public int bit;

        [FormerlySerializedAs("invertBit")] public bool invert;
        public Drawer drawer;

        [Header("Light Settings")] [Range(0, 1000)]
        public float intensity;

        [Range(0, 3)] public float fadeTime;
        public LightMode lightMode;

        [Header("Special Settings")] public SpecialMode specialMode;


        public bool materialLight;
        public GameObject emissiveObject;
        public string emmissiveMatName;
        public float emissiveMultiplier = 1;
        public Color emissiveMatColor = Color.white;
        public bool materialStars;
        public Texture2D[] starCookies;


        // DEPRECATED - Only here for caching before conversion.
        // If values exist, you **must** convert via Duofur > RR Conversion Tools
        [HideInInspector] [Obsolete] public float intensityMultiplier;
        [HideInInspector] [Obsolete] public char topOrBottom;
        [HideInInspector] [Obsolete] public bool strobe;
        [HideInInspector] [Obsolete] public bool flash;
        [HideInInspector] [Obsolete] public float fadeSpeed;
        private float acceleration;
        private UnityEngine.Light currentLight;
        private int currentTextureSet;

        private Material emissiveTexture;

        private bool errorOccured;

        //Values
        private bool flashCheck;
        private float nextTime;

        private ShowController showController;
        private float speed;
        private bool textureSet;


        private void Awake()
        {
            if (!materialLight)
                currentLight = GetComponent<UnityEngine.Light>();
            else
                foreach (Material matt in emissiveObject.GetComponent<MeshRenderer>().materials)
                    if (matt.name == emmissiveMatName)
                    {
                        emissiveTexture = matt;
                        emissiveTexture.EnableKeyword("_EMISSIVE_COLOR_MAP");
                    }

            if (materialStars) emissiveObject.SetActive(false);

            showController = GameObject.FindGameObjectWithTag("Show Controller").GetComponent<ShowController>();
        }

        private void Update()
        {
            if (showController.active) UpdateLight();
        }

        public void UpdateLight()
        {
            if (errorOccured == false)
                try
                {
                    bool onOff = false;
                    if (drawer == Drawer.Top && showController.topDrawer[bit - 1])
                        onOff = true;
                    else if (drawer == Drawer.Bottom && showController.bottomDrawer[bit - 1])
                        onOff = true;
                    if (invert) onOff = !onOff;

                    if (lightMode == LightMode.Flash)
                    {
                        if (onOff)
                        {
                            if (!flashCheck)
                            {
                                flashCheck = true;
                                nextTime = 1;
                            }
                            else
                            {
                                nextTime -= 1 / fadeTime * Time.deltaTime;
                            }
                        }
                        else
                        {
                            if (flashCheck) flashCheck = false;
                            nextTime -= 1 / fadeTime * Time.deltaTime;
                        }
                    }
                    else if (lightMode == LightMode.Strobe)
                    {
                        if (onOff)
                        {
                            if (nextTime != 0)
                                nextTime -= 1 / fadeTime * 2 * Time.deltaTime;
                            else
                                nextTime = 1;
                        }
                        else
                        {
                            nextTime -= 1 / fadeTime * 2 * Time.deltaTime;
                        }
                    }
                    else
                    {
                        if (onOff)
                            nextTime += 1 / fadeTime * Time.deltaTime;
                        else
                            nextTime -= 1 / fadeTime * Time.deltaTime;
                    }

                    nextTime = Mathf.Min(Mathf.Max(nextTime, 0), 1);
                    if (!materialLight)
                    {
                        currentLight.intensity = intensity * nextTime;
                    }
                    else if (!materialStars)
                    {
                        emissiveTexture.SetColor("_EmissiveColor",
                            emissiveMatColor * nextTime * emissiveMultiplier);
                    }
                    else
                    {
                        if (onOff && !emissiveObject.activeSelf) emissiveObject.SetActive(true);
                        if (!onOff && emissiveObject.activeSelf) emissiveObject.SetActive(false);
                    }

                    if (specialMode == SpecialMode.Helicopter)
                    {
                        if (onOff)
                        {
                            acceleration = Mathf.Max(50, acceleration + Time.deltaTime);
                        }
                        else
                        {
                            if (speed > 0)
                                acceleration = Mathf.Min(-50, acceleration + Time.deltaTime / 8.0f);
                            else
                                acceleration = 0;
                        }

                        speed = Mathf.Clamp(acceleration * Time.deltaTime + speed, 0, 40);
                        transform.Rotate(new Vector3(0, 0, speed * Time.deltaTime * 8.0f));
                    }

                    if (specialMode == SpecialMode.CuStar)
                    {
                        if (onOff) acceleration += Time.deltaTime;

                        if (acceleration > 10.0f)
                        {
                            acceleration = 0;
                            flashCheck = !flashCheck;
                            textureSet = false;
                        }

                        if (!flashCheck)
                        {
                            if (speed > 4) speed = 0;

                            if (currentTextureSet != Mathf.FloorToInt(speed) + 1)
                            {
                                currentLight.cookie = starCookies[Mathf.FloorToInt(speed) + 1];
                                currentTextureSet = Mathf.FloorToInt(speed) + 1;
                            }

                            currentLight.intensity =
                                Mathf.Clamp(Mathf.Sin(speed * Mathf.PI * 2 - Mathf.PI / 2.0f) + 1, 0.5f, 1) *
                                intensity * nextTime;

                            speed += Time.deltaTime * 15;
                        }
                        else
                        {
                            speed += Time.deltaTime;

                            if (!textureSet) currentLight.cookie = starCookies[0];
                            textureSet = true;
                            currentLight.intensity =
                                (Mathf.Sin(speed * 40 - 0.5f) + 1) * intensity * nextTime;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Light Controller error at " + gameObject.name + ": " + ex.Message + "\n" +
                                   ex.StackTrace);
                    errorOccured = true;
                }
        }
    }
}