using UnityEngine;

[CreateAssetMenu(fileName = "RotorDefinition", menuName = "Rotor definition")]
public class RotorDefinition : ScriptableObject
{
	public float Radius                            = 40f;
	public float Height                            = 40f;
	public float TopBottomOffset                   = 2f;
	public float ProfileSize                       = 6f;
	public float ProfileStartEndSize               = 7f;
	public float ProfileSizeChangeDistance         = 2f;	// distance from both ends where profile size is interpolated from StartEndSize to Size
	public float InnerOffset1                      = 7f;
	public float InnerOffset2                      = 14f;
	public int   LayersCount                       = 4;
	public float VerticalDistanceBetweenConnectors = 1f;

	private float FirstLayerTop => Height - TopBottomOffset - ProfileSize / 2f;
	private float LayerHeight   => (Height - TopBottomOffset * 2f - ProfileSize) / LayersCount;
	
	public float GetLayerTopY(int layer)
	{
		return FirstLayerTop - LayerHeight * (layer + 0.5f) + (ProfileSize + VerticalDistanceBetweenConnectors) * 0.5f;
	}

	public float GetLayerBottomY(int layer)
	{
		return FirstLayerTop - LayerHeight * (layer + 0.5f) - (ProfileSize + VerticalDistanceBetweenConnectors) * 0.5f;
	}
}
