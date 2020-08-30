using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
	public ChunkCoord coord;

	GameObject chunkObject;
	MeshRenderer meshRenderer;
	MeshFilter meshFilter;

	int vertexIndex = 0;
	List<Vector3> vertices = new List<Vector3>();
	List<int> triangles = new List<int>();
	List<int> transparentTriangles = new List<int>();
	List<Vector2> uvs = new List<Vector2>();
	List<Color> colors = new List<Color>();

	public Vector3 position;

	public VoxelState[,,] voxelMap = new VoxelState[Voxel.CHUNK_LENGTH_IN_VOXELS, Voxel.CHUNK_HEIGHT_IN_VOXELS, Voxel.CHUNK_LENGTH_IN_VOXELS];

	World world;

	private bool _isActive;
	private bool isVoxelMapPopulated = false;

	public Chunk(ChunkCoord _coord, World _world)
	{
		coord = _coord;
		world = _world;
	}

	public void Init()
	{
		chunkObject = new GameObject();
		meshFilter = chunkObject.AddComponent<MeshFilter>();
		meshRenderer = chunkObject.AddComponent<MeshRenderer>();

		meshRenderer.material = world.material;

		chunkObject.transform.SetParent(world.transform);
		chunkObject.transform.position = new Vector3(coord.x * Voxel.CHUNK_LENGTH_IN_VOXELS, 0f, coord.z * Voxel.CHUNK_LENGTH_IN_VOXELS);
		chunkObject.name = "Chunk " + coord.x + ", " + coord.z;
		position = chunkObject.transform.position;

		PopulateVoxelMap();
	}

	void PopulateVoxelMap()
	{

		for (int y = 0; y < Voxel.CHUNK_HEIGHT_IN_VOXELS; y++)
		{
			for (int x = 0; x < Voxel.CHUNK_LENGTH_IN_VOXELS; x++)
			{
				for (int z = 0; z < Voxel.CHUNK_LENGTH_IN_VOXELS; z++)
				{

					voxelMap[x, y, z] = new VoxelState(world.GetVoxel(new Vector3(x, y, z) + position));

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

		for (int y = 0; y < Voxel.CHUNK_HEIGHT_IN_VOXELS; y++)
		{
			for (int x = 0; x < Voxel.CHUNK_LENGTH_IN_VOXELS; x++)
			{
				for (int z = 0; z < Voxel.CHUNK_LENGTH_IN_VOXELS; z++)
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
			if (chunkObject != null)
				chunkObject.SetActive(value);

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

		if (x < 0 || x > Voxel.CHUNK_LENGTH_IN_VOXELS - 1 || y < 0 || y > Voxel.CHUNK_HEIGHT_IN_VOXELS - 1 || z < 0 || z > Voxel.CHUNK_LENGTH_IN_VOXELS - 1)
			return false;
		else
			return true;

	}

	public void EditVoxel(Vector3 pos, byte newID)
	{

		int xCheck = Mathf.FloorToInt(pos.x);
		int yCheck = Mathf.FloorToInt(pos.y);
		int zCheck = Mathf.FloorToInt(pos.z);

		xCheck -= Mathf.FloorToInt(chunkObject.transform.position.x);
		zCheck -= Mathf.FloorToInt(chunkObject.transform.position.z);

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

			Vector3 currentVoxel = thisVoxel + Voxel.FACES[p];

			if (!IsVoxelInChunk((int)currentVoxel.x, (int)currentVoxel.y, (int)currentVoxel.z))
			{

				world.chunksToUpdate.Insert(0, world.GetChunkFromVector3(currentVoxel + position));

			}

		}

	}

	VoxelState CheckVoxel(Vector3 pos)
	{

		int x = Mathf.FloorToInt(pos.x);
		int y = Mathf.FloorToInt(pos.y);
		int z = Mathf.FloorToInt(pos.z);

		if (!IsVoxelInChunk(x, y, z))
			return world.GetVoxelState(pos + position);

		return voxelMap[x, y, z];

	}

	public VoxelState GetVoxelFromGlobalVector3(Vector3 pos)
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
			VoxelState neighbor = CheckVoxel(pos + Voxel.FACES[p]);

			if (neighbor != null && world.blocktypes[neighbor.id].renderNeighborFaces)
			{
				vertices.Add(pos + Voxel.VERTICES[Voxel.TRIANGLES[p, 0]]);
				vertices.Add(pos + Voxel.VERTICES[Voxel.TRIANGLES[p, 1]]);
				vertices.Add(pos + Voxel.VERTICES[Voxel.TRIANGLES[p, 2]]);
				vertices.Add(pos + Voxel.VERTICES[Voxel.TRIANGLES[p, 3]]);

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
		float y = textureID / Voxel.ATLAS_LENGTH_IN_VOXELS;
		float x = textureID - (y * Voxel.ATLAS_LENGTH_IN_VOXELS);

		x *= Voxel.ATLAS_SIZE_NORMALIZED;
		y *= Voxel.ATLAS_SIZE_NORMALIZED;

		y = 1f - y - Voxel.ATLAS_SIZE_NORMALIZED;

		uvs.Add(new Vector2(x, y));
		uvs.Add(new Vector2(x, y + Voxel.ATLAS_SIZE_NORMALIZED));
		uvs.Add(new Vector2(x + Voxel.ATLAS_SIZE_NORMALIZED, y));
		uvs.Add(new Vector2(x + Voxel.ATLAS_SIZE_NORMALIZED, y + Voxel.ATLAS_SIZE_NORMALIZED));
	}
}

public class ChunkCoord
{
	public int x;
	public int z;

	public ChunkCoord(int _x = 0, int _z = 0)
	{
		x = _x;
		z = _z;
	}

	public ChunkCoord(Vector3 pos)
	{

		int xCheck = Mathf.FloorToInt(pos.x);
		int zCheck = Mathf.FloorToInt(pos.z);

		x = xCheck / Voxel.CHUNK_LENGTH_IN_VOXELS;
		z = zCheck / Voxel.CHUNK_LENGTH_IN_VOXELS;

	}

	public bool Equals(ChunkCoord other)
	{
		if (other == null)
			return false;
		else if (other.x == x && other.z == z)
			return true;
		else
			return false;
	}
}

public class VoxelState
{
	public byte id;

	public VoxelState(byte _id = 0)
	{
		id = _id;
	}

}
