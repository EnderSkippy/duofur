using UnityEngine;

public class AirCompressor : MonoBehaviour
{
    public bool power;

    [Space(10)] [Unit("PSI")] [Range(10, 300)]
    public float regulator = 80f;

    [Unit("PSI")] [ReadOnly] public float airPressure;

    private bool previousPowerState;

    private void Update()
    {
        if (power != previousPowerState)
        {
            previousPowerState = power;
            float targetPressure = power ? regulator : 0f;

            LeanTween.value(gameObject, airPressure, targetPressure, 10f)
                .setOnUpdate(val => { airPressure = val; });
        }

        airPressure = Mathf.Clamp(airPressure, 0f, regulator);
    }
}