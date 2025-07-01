using UnityEngine;

namespace ProceduralPixels.BakeAO
{
	public class OccluderParameters : MonoBehaviour
	{
		[Range(0.0f, 1.0f)]
		public float occluderStrength = 1.0f;
	} 
}
