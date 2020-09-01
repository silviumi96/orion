using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PerlinNoise  {

    public static float Get2D (Vector2 position, float offset, float scale) 
    {
        return Mathf.PerlinNoise((position.x + 0.1f) / Voxel.CHUNK_LENGTH_IN_VOXELS * scale + offset, (position.y + 0.1f) / Voxel.CHUNK_LENGTH_IN_VOXELS * scale + offset);
    }

    public static bool Get3D (Vector3 position, float offset, float scale, float threshold) 
    {
        float x = (position.x + offset + 0.1f) * scale;
        float y = (position.y + offset + 0.1f) * scale;
        float z = (position.z + offset + 0.1f) * scale;

        float XY = Mathf.PerlinNoise(x, y);
        float YZ = Mathf.PerlinNoise(y, z);
        float XZ = Mathf.PerlinNoise(x, z);
        float YX = Mathf.PerlinNoise(y, x);
        float ZY = Mathf.PerlinNoise(z, y);
        float ZX = Mathf.PerlinNoise(z, x);

        if ((XY + YZ + XZ + YX + ZY + ZX) / 6f > threshold)
            return true;
        else
            return false;
    }
}
