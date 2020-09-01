using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
	public static readonly int WORLD_SIZE_IN_VOXELS = Voxel.WORLD_LENGTH_IN_CHUNKS * Voxel.CHUNK_LENGTH_IN_VOXELS;

	public int seed;
	public BiomeAttributes biome;

	public Transform player;
	public Vector3 spawnPosition;

	public Material material;
	public Material transparentMaterial;

	public VoxelType[] blocktypes;

	Chunk[,] chunks = new Chunk[Voxel.WORLD_LENGTH_IN_CHUNKS, Voxel.WORLD_LENGTH_IN_CHUNKS];

	List<ChunkCoord> activeChunks = new List<ChunkCoord>();
	public ChunkCoord playerChunkCoord;
	ChunkCoord playerLastChunkCoord;

	List<ChunkCoord> chunksToCreate = new List<ChunkCoord>();
	public List<Chunk> chunksToUpdate = new List<Chunk>();
	public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

	Thread ChunkUpdateThread;
	public object ChunkUpdateThreadLock = new object();

	private void Start()
	{
		Random.InitState(seed);

		ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
		ChunkUpdateThread.Start();

		spawnPosition = new Vector3((Voxel.WORLD_LENGTH_IN_CHUNKS * Voxel.CHUNK_LENGTH_IN_VOXELS) / 2f, Voxel.CHUNK_HEIGHT_IN_VOXELS - 50f, (Voxel.WORLD_LENGTH_IN_CHUNKS * Voxel.CHUNK_LENGTH_IN_VOXELS) / 2f);
		GenerateWorld();
		playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
	}

	private void Update()
	{
		playerChunkCoord = GetChunkCoordFromVector3(player.position);

		// Only update the chunks if the player has moved from the chunk they were previously on.
		if (!playerChunkCoord.Equals(playerLastChunkCoord))
			CheckViewDistance();

		if (chunksToCreate.Count > 0)
			CreateChunk();

		if (chunksToDraw.Count > 0)
		{
			if (chunksToDraw.Peek().CanEdit)
				chunksToDraw.Dequeue().CreateMesh();
		}
	}

	void GenerateWorld()
	{
		for (int x = (Voxel.WORLD_LENGTH_IN_CHUNKS / 2) - Voxel.VIEW_DISTANCE_IN_CHUNKS; x < (Voxel.WORLD_LENGTH_IN_CHUNKS / 2) + Voxel.VIEW_DISTANCE_IN_CHUNKS; x++)
		{
			for (int z = (Voxel.WORLD_LENGTH_IN_CHUNKS / 2) - Voxel.VIEW_DISTANCE_IN_CHUNKS; z < (Voxel.WORLD_LENGTH_IN_CHUNKS / 2) + Voxel.VIEW_DISTANCE_IN_CHUNKS; z++)
			{

				ChunkCoord newChunk = new ChunkCoord(x, z);
				chunks[x, z] = new Chunk(newChunk, this);
				chunksToCreate.Add(newChunk);

			}
		}

		player.position = spawnPosition;
		CheckViewDistance();
	}

	void CreateChunk()
	{
		ChunkCoord c = chunksToCreate[0];
		chunksToCreate.RemoveAt(0);
		chunks[c.X, c.Z].Init();
	}

	void UpdateChunks()
	{
		bool updated = false;
		int index = 0;

		lock (ChunkUpdateThreadLock)
		{

			while (!updated && index < chunksToUpdate.Count - 1)
			{

				if (chunksToUpdate[index].CanEdit)
				{
					chunksToUpdate[index].UpdateChunk();
					activeChunks.Add(chunksToUpdate[index].coord);
					chunksToUpdate.RemoveAt(index);
					updated = true;
				}
				else
					index++;

			}
		}
	}

	void ThreadedUpdate()
	{
		while (true)
		{
			if (chunksToUpdate.Count > 0)
				UpdateChunks();
		}
	}

	private void OnDisable()
	{
		ChunkUpdateThread.Abort();
	}

	ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x / Voxel.CHUNK_LENGTH_IN_VOXELS);
		int z = Mathf.FloorToInt(pos.z / Voxel.CHUNK_LENGTH_IN_VOXELS);
		return new ChunkCoord(x, z);
	}

	public Chunk GetChunkFromVector3(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x / Voxel.CHUNK_LENGTH_IN_VOXELS);
		int z = Mathf.FloorToInt(pos.z / Voxel.CHUNK_LENGTH_IN_VOXELS);
		return chunks[x, z];
	}

	void CheckViewDistance()
	{
		ChunkCoord coord = GetChunkCoordFromVector3(player.position);
		playerLastChunkCoord = playerChunkCoord;

		List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

		activeChunks.Clear();

		for (int x = coord.X - Voxel.VIEW_DISTANCE_IN_CHUNKS; x < coord.X + Voxel.VIEW_DISTANCE_IN_CHUNKS; x++)
		{
			for (int z = coord.Z - Voxel.VIEW_DISTANCE_IN_CHUNKS; z < coord.Z + Voxel.VIEW_DISTANCE_IN_CHUNKS; z++)
			{
				if (IsChunkInside(new ChunkCoord(x, z)))
				{
					if (chunks[x, z] == null)
					{
						chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
						chunksToCreate.Add(new ChunkCoord(x, z));
					}
					else if (!chunks[x, z].IsActive)
					{
						chunks[x, z].IsActive = true;
					}
					activeChunks.Add(new ChunkCoord(x, z));
				}

				for (int i = 0; i < previouslyActiveChunks.Count; i++)
				{
					if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
						previouslyActiveChunks.RemoveAt(i);
				}
			}
		}

		foreach (ChunkCoord c in previouslyActiveChunks)
			chunks[c.X, c.Z].IsActive = false;
	}

	public bool CheckForVoxel(Vector3 pos)
	{
		ChunkCoord thisChunk = new ChunkCoord(pos);

		if (!IsChunkInside(thisChunk) || pos.y < 0 || pos.y > Voxel.CHUNK_HEIGHT_IN_VOXELS)
			return false;

		if (chunks[thisChunk.X, thisChunk.Z] != null && chunks[thisChunk.X, thisChunk.Z].CanEdit)
			return blocktypes[chunks[thisChunk.X, thisChunk.Z].GetVoxelFromGlobalVector3(pos).Id].isSolid;

		return blocktypes[GetVoxelId(pos)].isSolid;
	}

	public VoxelId GetVoxelState(Vector3 pos)
	{
		ChunkCoord thisChunk = new ChunkCoord(pos);

		if (!IsChunkInside(thisChunk) || pos.y < 0 || pos.y > Voxel.CHUNK_HEIGHT_IN_VOXELS)
			return null;

		if (chunks[thisChunk.X, thisChunk.Z] != null && chunks[thisChunk.X, thisChunk.Z].CanEdit)
			return chunks[thisChunk.X, thisChunk.Z].GetVoxelFromGlobalVector3(pos);

		return new VoxelId(GetVoxelId(pos));
	}

	public byte GetVoxelId(Vector3 pos)
	{
		int yPos = Mathf.FloorToInt(pos.y);

		if (!IsVoxelInside(pos))
			return 0;

		if (yPos == 0)
			return 1;

		int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * PerlinNoise.Get2D(new Vector2(pos.x, pos.z), 7, biome.terrainScale)) + biome.solidGroundHeight;
		byte voxelValue = 0;

		if (yPos == terrainHeight)
			voxelValue = 3;
		else if (yPos < terrainHeight && yPos > terrainHeight - 4)
			voxelValue = 2;
		else if (yPos > terrainHeight)
			return 0;
		else
			voxelValue = 1;

		if (voxelValue == 2)
		{
			foreach (Lode lode in biome.lodes)
			{
				if (yPos > lode.minHeight && yPos < lode.maxHeight)
					if (PerlinNoise.Get3D(pos, lode.noiseOffset, lode.scale, lode.threshold))
						voxelValue = lode.blockID;
			}
		}

		return voxelValue;
	}

	private bool IsChunkInside(ChunkCoord coord)
	{
		if (coord.X > 0 && coord.X < Voxel.WORLD_LENGTH_IN_CHUNKS - 1 && coord.Z > 0 && coord.Z < Voxel.WORLD_LENGTH_IN_CHUNKS - 1)
			return true;
		else
			return
				false;
	}

	private bool IsVoxelInside(Vector3 pos)
	{
		if (pos.x >= 0 && pos.x < WORLD_SIZE_IN_VOXELS && pos.y >= 0 && pos.y < Voxel.CHUNK_HEIGHT_IN_VOXELS && pos.z >= 0 && pos.z < WORLD_SIZE_IN_VOXELS)
			return true;
		else
			return false;
	}
}

[System.Serializable]
public class VoxelType
{
	public string Name;
	public bool isSolid;
	public bool renderNeighborFaces;
	public float transparency;
	public Sprite icon;

	public int backFaceTexture;
	public int frontFaceTexture;
	public int topFaceTexture;
	public int bottomFaceTexture;
	public int leftFaceTexture;
	public int rightFaceTexture;

	public int GetTextureID(int faceIndex)
	{
		switch (faceIndex)
		{
			case 0:
				return backFaceTexture;
			case 1:
				return frontFaceTexture;
			case 2:
				return topFaceTexture;
			case 3:
				return bottomFaceTexture;
			case 4:
				return leftFaceTexture;
			case 5:
				return rightFaceTexture;
			default:
				Debug.LogError("Cannot find texture");
				return 0;
		}
	}
}
