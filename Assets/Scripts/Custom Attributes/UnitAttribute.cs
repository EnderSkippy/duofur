using UnityEngine;

public class UnitAttribute : PropertyAttribute
{
    public string Unit;

    public UnitAttribute(string unit)
    {
        Unit = unit;
    }
}