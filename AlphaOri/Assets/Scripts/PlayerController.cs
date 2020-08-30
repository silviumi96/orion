using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const float COLLIDER_WIDTH = 0.15f;
	private const float REACH_DISTANCE = 8f;
	private const float RAYCAST_STEP = 0.1f;
	private const float MOUSE_SENSITIVITY = 2;
	private const float SPEED = 10f;
	private const float JUMP_VELOCITY = 8f;
	private const float GRAVITY = -15f;
	private const int SPAWN_BLOCK_ID = 1;

	private float moveX;
	private float moveZ;
	private float mouseX;
	private float mouseY;
	private float verticalVelocity = 0;
	private bool shouldJump;
	private bool isGrounded;

	private Vector3 velocity;

	[SerializeField]
	private Transform destroyBlockIndicator;

	[SerializeField]
	private Transform spawnBlockIndicator;

	[SerializeField]
	private Transform camera;

	[SerializeField]
	private World world;

	private void Start()
	{
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void Update()
	{
		GetInput();
		ComputeVelocity();

		transform.Rotate(Vector3.up * mouseX * MOUSE_SENSITIVITY);
		camera.Rotate(Vector3.right * -mouseY * MOUSE_SENSITIVITY);
		transform.Translate(velocity, Space.World);

		if (shouldJump)
			Jump();

		placeCursorBlocks();
	}

	void Jump()
	{
		verticalVelocity = JUMP_VELOCITY;
		isGrounded = false;
		shouldJump = false;
	}

	private void ComputeVelocity()
	{
		// Affect vertical momentum with gravity.
		if (verticalVelocity > GRAVITY)
			verticalVelocity += Time.deltaTime * GRAVITY;

		velocity = ((transform.forward * moveZ) + (transform.right * moveX)) * Time.deltaTime * SPEED;

		// Apply vertical momentum (falling/jumping).
		velocity += Vector3.up * verticalVelocity * Time.deltaTime;

		if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
			velocity.z = 0;
		if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
			velocity.x = 0;

		if (velocity.y < 0)
			velocity.y = checkDownSpeed(velocity.y);
		else if (velocity.y > 0)
			velocity.y = checkUpSpeed(velocity.y);
	}

	private void GetInput()
	{
		mouseX = Input.GetAxisRaw("Mouse X");
		mouseY = Input.GetAxisRaw("Mouse Y");

		if (isGrounded)
		{
			moveX = Input.GetAxisRaw("Horizontal");
			moveZ = Input.GetAxisRaw("Vertical");

			if (Input.GetButtonDown("Jump"))
			{
				shouldJump = true;
			}
		}

		if (destroyBlockIndicator.gameObject.activeSelf)
		{
			// Destroy block.
			if (Input.GetMouseButtonDown(0))
				world.GetChunkFromVector3(destroyBlockIndicator.position).EditVoxel(destroyBlockIndicator.position, 0);

			// Place block.
			if (Input.GetMouseButtonDown(1))
			{
				world.GetChunkFromVector3(destroyBlockIndicator.position).EditVoxel(spawnBlockIndicator.position, SPAWN_BLOCK_ID);
			}
		}
	}

	private void placeCursorBlocks()
	{
		float step = RAYCAST_STEP;
		Vector3 lastPos = new Vector3();

		while (step < REACH_DISTANCE)
		{
			Vector3 pos = camera.position + (camera.forward * step);

			if (world.CheckForVoxel(pos))
			{

				destroyBlockIndicator.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
				spawnBlockIndicator.position = lastPos;

				destroyBlockIndicator.gameObject.SetActive(true);
				spawnBlockIndicator.gameObject.SetActive(true);

				return;

			}

			lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

			step += RAYCAST_STEP;

		}

		destroyBlockIndicator.gameObject.SetActive(false);
		spawnBlockIndicator.gameObject.SetActive(false);
	}

	private float checkDownSpeed(float downSpeed)
	{
		if (
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + downSpeed, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + downSpeed, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + downSpeed, transform.position.z + COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + downSpeed, transform.position.z + COLLIDER_WIDTH))
		   )
		{
			isGrounded = true;
			return 0;
		}
		else
		{
			isGrounded = false;
			return downSpeed;
		}
	}

	private float checkUpSpeed(float upSpeed)
	{
		if (
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + 2f + upSpeed, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + 2f + upSpeed, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + 2f + upSpeed, transform.position.z + COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + 2f + upSpeed, transform.position.z + COLLIDER_WIDTH))
		   )
		{
			return 0;
		}
		else
		{
			return upSpeed;
		}
	}

	public bool front
	{
		get
		{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + COLLIDER_WIDTH)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + COLLIDER_WIDTH))
				)
				return true;
			else
				return false;
		}
	}

	public bool back
	{
		get
		{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - COLLIDER_WIDTH)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - COLLIDER_WIDTH))
				)
				return true;
			else
				return false;
		}
	}

	public bool left
	{
		get
		{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + 1f, transform.position.z))
				)
				return true;
			else
				return false;
		}
	}

	public bool right
	{
		get
		{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + 1f, transform.position.z))
				)
				return true;
			else
				return false;
		}
	}
}
