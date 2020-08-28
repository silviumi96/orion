using UnityEngine;

public static class Voxel
{
	public static readonly int CHUNK_LENGTH_IN_VOXELS = 16;
	public static readonly int CHUNK_HEIGHT_IN_VOXELS = 128;

	public static readonly int WORLD_LENGTH_IN_CHUNKS = 128;
	public static readonly int VIEW_DISTANCE_IN_CHUNKS = 4;

	public static readonly int ATLAS_LENGTH_IN_VOXELS = 16;
	public static readonly float ATLAS_SIZE_NORMALIZED = 1f / ATLAS_LENGTH_IN_VOXELS;

	public static readonly Vector3[] VERTICES = new Vector3[8]
	{
		new Vector3(0.0f, 0.0f, 0.0f), //0
		new Vector3(1.0f, 0.0f, 0.0f), //1
		new Vector3(1.0f, 1.0f, 0.0f), //2
		new Vector3(0.0f, 1.0f, 0.0f), //3
		new Vector3(0.0f, 0.0f, 1.0f), //4
		new Vector3(1.0f, 0.0f, 1.0f), //5
		new Vector3(1.0f, 1.0f, 1.0f), //6
		new Vector3(0.0f, 1.0f, 1.0f), //7
	};

	public static readonly int[,] TRIANGLES = new int[6, 4]
	{
		{0, 3, 1, 2},
		{5, 6, 4, 7},
		{3, 7, 2, 6},
		{1, 5, 0, 4},
		{4, 7, 0, 3},
		{1, 2, 5, 6}
	};

	public static readonly Vector3[] FACES = new Vector3[6]
	{
		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, 0.0f, 1.0f),
		new Vector3(0.0f, 1.0f, 0.0f),
		new Vector3(0.0f, -1.0f, 0.0f),
		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3(1.0f, 0.0f, 0.0f)
	};

	public static readonly Vector2[] UVS = new Vector2[4]
	{
		new Vector2 (0.0f, 0.0f),
		new Vector2 (0.0f, 1.0f),
		new Vector2 (1.0f, 0.0f),
		new Vector2 (1.0f, 1.0f)
	};
}
