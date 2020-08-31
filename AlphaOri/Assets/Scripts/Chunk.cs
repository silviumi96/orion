using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
	public ChunkCoordinates Coordinates;

	private GameObject gameObject;
	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;

	int vertexIndex = 0;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	List<int> transparentTriangles = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	List<Color> colors = new List<Color>();

	public Vector3 position;

	public VoxelId[,,] voxelMap = new VoxelId[BlockData.CHUNK_LENGTH_IN_BLOCKS, BlockData.CHUNK_HEIGHT_IN_BLOCKS, BlockData.CHUNK_LENGTH_IN_BLOCKS];

	World world;

	private bool _isActive;
	private bool isVoxelMapPopulated = false;

	public Chunk(ChunkCoordinates _coord, World _world)
	{
		Coordinates = _coord;
		world = _world;
	}

	public void Init()
	{
		gameObject = new GameObject();
		meshFilter = gameObject.AddComponent<MeshFilter>();
		meshRenderer = gameObject.AddComponent<MeshRenderer>();

		meshRenderer.material = world.material;

		gameObject.transform.SetParent(world.transform);
		gameObject.transform.position = new Vector3(Coordinates.X * BlockData.CHUNK_LENGTH_IN_BLOCKS, 0f, Coordinates.Z * BlockData.CHUNK_LENGTH_IN_BLOCKS);
		gameObject.name = "Chunk " + Coordinates.X + ", " + Coordinates.Z;
		position = gameObject.transform.position;

		PopulateVoxelMap();
	}

	void PopulateVoxelMap()
	{

		for (int y = 0; y < BlockData.CHUNK_HEIGHT_IN_BLOCKS; y++)
		{
			for (int x = 0; x < BlockData.CHUNK_LENGTH_IN_BLOCKS; x++)
			{
				for (int z = 0; z < BlockData.CHUNK_LENGTH_IN_BLOCKS; z++)
				{

					voxelMap[x, y, z] = new VoxelId(world.GetVoxel(new Vector3(x, y, z) + position));

				}
			}
		}

		isVoxelMapPopulated = true;

		lock (world.ChunkUpdateThreadLock)
		{
			world.chunksToUpdate.Add(this);
		}

	}

	public void UpdateChunk()
	{
		ClearMeshData();

		for (int y = 0; y < BlockData.CHUNK_HEIGHT_IN_BLOCKS; y++)
		{
			for (int x = 0; x < BlockData.CHUNK_LENGTH_IN_BLOCKS; x++)
			{
				for (int z = 0; z < BlockData.CHUNK_LENGTH_IN_BLOCKS; z++)
				{

					if (world.blocktypes[voxelMap[x, y, z].id].isSolid)
						UpdateMeshData(new Vector3(x, y, z));

				}
			}
		}

		world.chunksToDraw.Enqueue(this);


	}

	void ClearMeshData()
	{

		vertexIndex = 0;
		vertices.Clear();
		triangles.Clear();
		transparentTriangles.Clear();
		uvs.Clear();
		colors.Clear();

	}

	public bool isActive
	{

		get { return _isActive; }
		set
		{

			_isActive = value;
			if (gameObject != null)
				gameObject.SetActive(value);

		}

	}

	public bool isEditable
	{

		get
		{

			if (!isVoxelMapPopulated)
				return false;
			else
				return true;

		}

	}

	bool IsVoxelInChunk(int x, int y, int z)
	{

		if (x < 0 || x > BlockData.CHUNK_LENGTH_IN_BLOCKS - 1 || y < 0 || y > BlockData.CHUNK_HEIGHT_IN_BLOCKS - 1 || z < 0 || z > BlockData.CHUNK_LENGTH_IN_BLOCKS - 1)
			return false;
		else
			return true;

	}

	public void EditVoxel(Vector3 pos, byte newID)
	{

		int xCheck = Mathf.FloorToInt(pos.x);
		int yCheck = Mathf.FloorToInt(pos.y);
		int zCheck = Mathf.FloorToInt(pos.z);

		xCheck -= Mathf.FloorToInt(gameObject.transform.position.x);
		zCheck -= Mathf.FloorToInt(gameObject.transform.position.z);

		voxelMap[xCheck, yCheck, zCheck].id = newID;

		lock (world.ChunkUpdateThreadLock)
		{

			world.chunksToUpdate.Insert(0, this);
			UpdateSurroundingVoxels(xCheck, yCheck, zCheck);

		}

	}

	void UpdateSurroundingVoxels(int x, int y, int z)
	{

		Vector3 thisVoxel = new Vector3(x, y, z);

		for (int p = 0; p < 6; p++)
		{

			Vector3 currentVoxel = thisVoxel + BlockData.FACE_SCAN_OFFSET[p];

			if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
			{

				world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));

			}

		}

	}

	VoxelId CheckVoxel(Vector3 pos)
	{

		int x = Mathf.FloorToInt(pos.x);
		int y = Mathf.FloorToInt(pos.y);
		int z = Mathf.FloorToInt(pos.z);

		if (!IsVoxelInChunk(x, y, z))
			return world.GetVoxelState(pos + position);

		return voxelMap[x, y, z];

	}

	public VoxelId GetVoxelFromGlobalVector3(Vector3 pos)
	{
		int xCheck = Mathf.FloorToInt(pos.x);
		int yCheck = Mathf.FloorToInt(pos.y);
		int zCheck = Mathf.FloorToInt(pos.z);

		xCheck -= Mathf.FloorToInt(position.x);
		zCheck -= Mathf.FloorToInt(position.z);

		return voxelMap[xCheck, yCheck, zCheck];
	}

	void UpdateMeshData(Vector3 pos)
	{

		int x = Mathf.FloorToInt(pos.x);
		int y = Mathf.FloorToInt(pos.y);
		int z = Mathf.FloorToInt(pos.z);

		byte blockID = voxelMap[x, y, z].id;

		for (int p = 0; p < 6; p++)
		{
			VoxelId neighbor = CheckVoxel(pos + BlockData.FACE_SCAN_OFFSET[p]);

			if (neighbor != null && world.blocktypes[neighbor.id].renderNeighborFaces)
			{
				vertices.Add(pos + BlockData.VERTICES[BlockData.TRIANGLES[p, 0]]);
				vertices.Add(pos + BlockData.VERTICES[BlockData.TRIANGLES[p, 1]]);
				vertices.Add(pos + BlockData.VERTICES[BlockData.TRIANGLES[p, 2]]);
				vertices.Add(pos + BlockData.VERTICES[BlockData.TRIANGLES[p, 3]]);

				AddTexture(world.blocktypes[blockID].GetTextureID(p));

				triangles.Add(vertexIndex);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 2);
				triangles.Add(vertexIndex + 1);
				triangles.Add(vertexIndex + 3);

				vertexIndex += 4;
			}
		}
	}

	public void CreateMesh()
	{
		Mesh mesh = new Mesh();
		mesh.vertices = vertices.ToArray();
		mesh.triangles = triangles.ToArray();
		mesh.uv = uvs.ToArray();
		mesh.colors = colors.ToArray();

		mesh.RecalculateNormals();

		meshFilter.mesh = mesh;
	}

	void AddTexture(int textureID)
	{
		float y = textureID / BlockData.ATLAS_LENGTH_IN_CELLS;
		float x = textureID - (y * BlockData.ATLAS_LENGTH_IN_CELLS);

		x *= BlockData.ATLAS_SIZE_NORMALIZED;
		y *= BlockData.ATLAS_SIZE_NORMALIZED;

		y = 1f - y - BlockData.ATLAS_SIZE_NORMALIZED;

		uvs.Add(new Vector2(x, y));
		uvs.Add(new Vector2(x, y + BlockData.ATLAS_SIZE_NORMALIZED));
		uvs.Add(new Vector2(x + BlockData.ATLAS_SIZE_NORMALIZED, y));
		uvs.Add(new Vector2(x + BlockData.ATLAS_SIZE_NORMALIZED, y + BlockData.ATLAS_SIZE_NORMALIZED));
	}
}

public class ChunkCoordinates
{
	public int X { get; private set; }
	public int Z { get; private set; }

	public ChunkCoordinates(int x = 0, int z = 0)
	{
		X = x;
		Z = z;
	}

	public ChunkCoordinates(Vector3 position)
	{
		X = Mathf.FloorToInt(position.x) / BlockData.CHUNK_LENGTH_IN_BLOCKS;
		Z = Mathf.FloorToInt(position.z) / BlockData.CHUNK_LENGTH_IN_BLOCKS;
	}

	public bool Equals(ChunkCoordinates coordinates)
	{
		if (coordinates.X == X && coordinates.Z == Z && coordinates == null)
			return true;

		return false;
	}
}

public class VoxelId
{
	public byte id;

	public VoxelId(byte _id = 0)
	{
		id = _id;
	}

}
