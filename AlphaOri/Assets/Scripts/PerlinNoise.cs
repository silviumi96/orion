using UnityEngine;

public static class PerlinNoise
{
	public static float GetNoise2d(Vector2 position, float scale)
	{
		return Mathf.PerlinNoise
		(
			(position.x + 0.1f) / BlockData.CHUNK_LENGTH_IN_BLOCKS * scale,
			(position.y + 0.1f) / BlockData.CHUNK_LENGTH_IN_BLOCKS * scale
		);
	}

	public static bool GetNoise3d(Vector3 position, float offset, float scale, float limit)
	{
		var x = (position.x + offset + 0.1f) * scale;
		var y = (position.y + offset + 0.1f) * scale;
		var z = (position.z + offset + 0.1f) * scale;

		if ((Mathf.PerlinNoise(x, y) + Mathf.PerlinNoise(y, z) + 
			 Mathf.PerlinNoise(x, z) + Mathf.PerlinNoise(y, x) + 
			 Mathf.PerlinNoise(z, y) + Mathf.PerlinNoise(z, x)) / 6f > limit
		)	
			return true;
		else
			return false;
	}
}
