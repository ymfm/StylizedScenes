using UnityEngine;

[CreateAssetMenu(fileName = "TerrainData", menuName = "")]
public class TerrainData : UpdatableData
{
    public float uniformScale = 5f;
    public bool useFlatShading;
	public bool useFalloff;
	public float meshHeightMultiplier;
	public AnimationCurve meshHeightCurve;
}
