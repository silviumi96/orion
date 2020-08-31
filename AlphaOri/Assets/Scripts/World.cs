using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class World : MonoBehaviour
{
	public static readonly int WORLD_SIZE_IN_VOXELS = BlockData.WORLD_LENGTH_IN_CHUNKS * BlockData.CHUNK_LENGTH_IN_BLOCKS;

	private const int SEED = 4444;
	private const int GROUND_TO_SURFACE_HEIGHT = 40;
	private const int SURFACE_TO_SKY_HEIGHT = 40;
	private const float TERRAIN_SCALE = 0.25f;

	public Lode[] Lodes;

	public Transform player;
	public Vector3 spawnPosition;

	public Material material;
	public Material transparentMaterial;

	public VoxelType[] blocktypes;

	Chunk[,] chunks = new Chunk[BlockData.WORLD_LENGTH_IN_CHUNKS, BlockData.WORLD_LENGTH_IN_CHUNKS];

	List<ChunkCoordinates> activeChunks = new List<ChunkCoordinates>();
	public ChunkCoordinates playerChunkCoord;
	ChunkCoordinates playerLastChunkCoord;

	List<ChunkCoordinates> chunksToCreate = new List<ChunkCoordinates>();
	public List<Chunk> chunksToUpdate = new List<Chunk>();
	public Queue<Chunk> chunksToDraw = new Queue<Chunk>();

	Thread ChunkUpdateThread;
	public object ChunkUpdateThreadLock = new object();

	private void Start()
	{

		Random.InitState(SEED);


		ChunkUpdateThread = new Thread(new ThreadStart(ThreadedUpdate));
		ChunkUpdateThread.Start();


		spawnPosition = new Vector3((BlockData.WORLD_LENGTH_IN_CHUNKS * BlockData.CHUNK_LENGTH_IN_BLOCKS) / 2f, BlockData.CHUNK_HEIGHT_IN_BLOCKS - 50f, (BlockData.WORLD_LENGTH_IN_CHUNKS * BlockData.CHUNK_LENGTH_IN_BLOCKS) / 2f);
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
			if (chunksToDraw.Peek().isEditable)
				chunksToDraw.Dequeue().CreateMesh();
		}


	}

	void GenerateWorld()
	{

		for (int x = (BlockData.WORLD_LENGTH_IN_CHUNKS / 2) - BlockData.VIEW_DISTANCE_IN_CHUNKS; x < (BlockData.WORLD_LENGTH_IN_CHUNKS / 2) + BlockData.VIEW_DISTANCE_IN_CHUNKS; x++)
		{
			for (int z = (BlockData.WORLD_LENGTH_IN_CHUNKS / 2) - BlockData.VIEW_DISTANCE_IN_CHUNKS; z < (BlockData.WORLD_LENGTH_IN_CHUNKS / 2) + BlockData.VIEW_DISTANCE_IN_CHUNKS; z++)
			{

				ChunkCoordinates newChunk = new ChunkCoordinates(x, z);
				chunks[x, z] = new Chunk(newChunk, this);
				chunksToCreate.Add(newChunk);

			}
		}

		player.position = spawnPosition;
		CheckViewDistance();

	}

	void CreateChunk()
	{

		ChunkCoordinates c = chunksToCreate[0];
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

				if (chunksToUpdate[index].isEditable)
				{
					chunksToUpdate[index].UpdateChunk();
					activeChunks.Add(chunksToUpdate[index].Coordinates);
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

	ChunkCoordinates GetChunkCoordFromVector3(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x / BlockData.CHUNK_LENGTH_IN_BLOCKS);
		int z = Mathf.FloorToInt(pos.z / BlockData.CHUNK_LENGTH_IN_BLOCKS);
		return new ChunkCoordinates(x, z);
	}

	public Chunk GetChunkFromVector3(Vector3 pos)
	{
		int x = Mathf.FloorToInt(pos.x / BlockData.CHUNK_LENGTH_IN_BLOCKS);
		int z = Mathf.FloorToInt(pos.z / BlockData.CHUNK_LENGTH_IN_BLOCKS);
		return chunks[x, z];
	}

	void CheckViewDistance()
	{

		ChunkCoordinates coord = GetChunkCoordFromVector3(player.position);
		playerLastChunkCoord = playerChunkCoord;

		List<ChunkCoordinates> previouslyActiveChunks = new List<ChunkCoordinates>(activeChunks);

		activeChunks.Clear();

		// Loop through all chunks currently within view distance of the player.
		for (int x = coord.X - BlockData.VIEW_DISTANCE_IN_CHUNKS; x < coord.X + BlockData.VIEW_DISTANCE_IN_CHUNKS; x++)
		{
			for (int z = coord.Z - BlockData.VIEW_DISTANCE_IN_CHUNKS; z < coord.Z + BlockData.VIEW_DISTANCE_IN_CHUNKS; z++)
			{

				// If the current chunk is in the world...
				if (IsChunkInside(new ChunkCoordinates(x, z)))
				{

					// Check if it active, if not, activate it.
					if (chunks[x, z] == null)
					{
						chunks[x, z] = new Chunk(new ChunkCoordinates(x, z), this);
						chunksToCreate.Add(new ChunkCoordinates(x, z));
					}
					else if (!chunks[x, z].isActive)
					{
						chunks[x, z].isActive = true;
					}
					activeChunks.Add(new ChunkCoordinates(x, z));
				}

				// Check through previously active chunks to see if this chunk is there. If it is, remove it from the list.
				for (int i = 0; i < previouslyActiveChunks.Count; i++)
				{

					if (previouslyActiveChunks[i].Equals(new ChunkCoordinates(x, z)))
						previouslyActiveChunks.RemoveAt(i);

				}

			}
		}

		// Any chunks left in the previousActiveChunks list are no longer in the player's view distance, so loop through and disable them.
		foreach (ChunkCoordinates c in previouslyActiveChunks)
			chunks[c.X, c.Z].isActive = false;

	}

	public bool CheckForVoxel(Vector3 pos)
	{

		ChunkCoordinates thisChunk = new ChunkCoordinates(pos);

		if (!IsChunkInside(thisChunk) || pos.y < 0 || pos.y > BlockData.CHUNK_HEIGHT_IN_BLOCKS)
			return false;

		if (chunks[thisChunk.X, thisChunk.Z] != null && chunks[thisChunk.X, thisChunk.Z].isEditable)
			return blocktypes[chunks[thisChunk.X, thisChunk.Z].GetVoxelFromGlobalVector3(pos).id].isSolid;

		return blocktypes[GetVoxel(pos)].isSolid;

	}

	public VoxelId GetVoxelState(Vector3 pos)
	{

		ChunkCoordinates thisChunk = new ChunkCoordinates(pos);

		if (!IsChunkInside(thisChunk) || pos.y < 0 || pos.y > BlockData.CHUNK_HEIGHT_IN_BLOCKS)
			return null;

		if (chunks[thisChunk.X, thisChunk.Z] != null && chunks[thisChunk.X, thisChunk.Z].isEditable)
			return chunks[thisChunk.X, thisChunk.Z].GetVoxelFromGlobalVector3(pos);

		return new VoxelId(GetVoxel(pos));

	}

	public byte GetVoxel(Vector3 pos)
	{
		int yPos = Mathf.FloorToInt(pos.y);

		if (!IsVoxelInside(pos))
			return 0;

		if (yPos == 0)
			return 1;

		int terrainHeight = Mathf.FloorToInt(SURFACE_TO_SKY_HEIGHT * PerlinNoise.GetNoise2d(new Vector2(pos.x, pos.z), TERRAIN_SCALE)) + GROUND_TO_SURFACE_HEIGHT;
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
			foreach (var lode in Lodes)
			{
				if (yPos > lode.minHeight && yPos < lode.maxHeight)
					if (PerlinNoise.GetNoise3d(pos, lode.noiseOffset, lode.scale, lode.threshold))
						voxelValue = lode.blockID;
			}
		}

		return voxelValue;


	}

	bool IsChunkInside(ChunkCoordinates coord)
	{

		if (coord.X > 0 && coord.X < BlockData.WORLD_LENGTH_IN_CHUNKS - 1 && coord.Z > 0 && coord.Z < BlockData.WORLD_LENGTH_IN_CHUNKS - 1)
			return true;
		else
			return
				false;

	}

	bool IsVoxelInside(Vector3 pos)
	{

		if (pos.x >= 0 && pos.x < WORLD_SIZE_IN_VOXELS && pos.y >= 0 && pos.y < BlockData.CHUNK_HEIGHT_IN_BLOCKS && pos.z >= 0 && pos.z < WORLD_SIZE_IN_VOXELS)
			return true;
		else
			return false;

	}

}

[System.Serializable]
public class Lode
{
	public string nodeName;
	public byte blockID;
	public int minHeight;
	public int maxHeight;
	public float scale;
	public float threshold;
	public float noiseOffset;
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

	// Back, Front, Top, Bottom, Left, Right

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
