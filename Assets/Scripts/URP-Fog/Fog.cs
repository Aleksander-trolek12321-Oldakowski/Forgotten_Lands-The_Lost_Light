using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable, VolumeComponentMenu("Custom/Fog")]
public class Fog : VolumeComponent, IPostProcessComponent
{
    public ColorParameter fogColor = new ColorParameter(Color.gray);
    public FloatParameter density = new FloatParameter(0.03f);
    public FloatParameter start = new FloatParameter(0f);
    public FloatParameter end = new FloatParameter(60f);
    public FloatParameter height = new FloatParameter(0f);
    public FloatParameter heightDensity = new FloatParameter(1f);
    public BoolParameter excludeSkybox = new BoolParameter(true);

    public bool IsActive() => density.value > 0f && end.value > start.value;
    public bool IsTileCompatible() => false;
}
