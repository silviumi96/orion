using UnityEngine;

public class PlayerController : MonoBehaviour
{
	private const float COLLIDER_HEIGHT = 2f;
	private const float HALF_COLLIDER_HEIGHT = 1f;
	private const float COLLIDER_WIDTH = 0.15f;
	private const float REACH_DISTANCE = 8f;
	private const float RAYCAST_STEP = 0.1f;
	private const float MOUSE_SENSITIVITY = 2;
	private const float SPEED = 10f;
	private const float JUMP_VELOCITY = 8f;
	private const float GRAVITY = -15f;
	private const byte SPAWN_BLOCK_ID = 1;

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
			InitiateJump();

		UpdateRaycast();
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
			if (Input.GetMouseButtonDown(0))
				world.GetChunkFromVector3(destroyBlockIndicator.position).EditVoxel(destroyBlockIndicator.position, 0);

			if (Input.GetMouseButtonDown(1))
			{
				world.GetChunkFromVector3(spawnBlockIndicator.position).EditVoxel(spawnBlockIndicator.position, SPAWN_BLOCK_ID);
			}
		}
	}

	private void ComputeVelocity()
	{
		if (verticalVelocity > GRAVITY)
			verticalVelocity += GRAVITY * Time.deltaTime;

		velocity = ((transform.forward * moveZ) + (transform.right * moveX)) * SPEED * Time.deltaTime;
		velocity += Vector3.up * verticalVelocity * Time.deltaTime;

		if (velocity.y < 0)
			velocity.y = CheckVelocityDownwards(velocity.y);
		else if (velocity.y > 0)
			velocity.y = CheckVelocityUpwards(velocity.y);

		if ((velocity.z > 0 && CheckCollisionFront()) || (velocity.z < 0 && CheckCollisionBack()))
			velocity.z = 0;
		if ((velocity.x > 0 && CheckCollisionRight()) || (velocity.x < 0 && CheckCollisionLeft()))
			velocity.x = 0;
	}

	public bool CheckCollisionRight()
	{

		if (world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y, transform.position.z)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + HALF_COLLIDER_HEIGHT, transform.position.z))
		)
			return true;
		else
			return false;

	}

	public bool CheckCollisionLeft()
	{

		if (world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y, transform.position.z)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + HALF_COLLIDER_HEIGHT, transform.position.z))
		)
			return true;
		else
			return false;

	}

	private bool CheckCollisionFront()
	{
		if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + HALF_COLLIDER_HEIGHT, transform.position.z + COLLIDER_WIDTH))
		)
			return true;
		else
			return false;
	}

	public bool CheckCollisionBack()
	{
		if (world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + HALF_COLLIDER_HEIGHT, transform.position.z - COLLIDER_WIDTH))
		)
			return true;
		else
			return false;
	}

	private float CheckVelocityUpwards(float velocityY)
	{
		if (world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + COLLIDER_HEIGHT + velocityY, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + COLLIDER_HEIGHT + velocityY, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + COLLIDER_HEIGHT + velocityY, transform.position.z + COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + COLLIDER_HEIGHT + velocityY, transform.position.z + COLLIDER_WIDTH))
		)
		{
			return 0;
		}
		else
		{
			return velocityY;
		}
	}

	private float CheckVelocityDownwards(float velocityY)
	{
		if (world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + velocityY, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + velocityY, transform.position.z - COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + COLLIDER_WIDTH, transform.position.y + velocityY, transform.position.z + COLLIDER_WIDTH)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - COLLIDER_WIDTH, transform.position.y + velocityY, transform.position.z + COLLIDER_WIDTH))
		)
		{
			isGrounded = true;
			return 0;
		}
		else
		{
			isGrounded = false;
			return velocityY;
		}
	}

	void InitiateJump()
	{
		verticalVelocity = JUMP_VELOCITY;
		isGrounded = false;
		shouldJump = false;
	}

	private void UpdateRaycast()
	{
		var currentPosition = new Vector3();
		var previousPosition = new Vector3();
		var step = RAYCAST_STEP;
		
		while (step < REACH_DISTANCE)
		{
			currentPosition = camera.position + (camera.forward * step);

			if (world.CheckForVoxel(currentPosition))
			{
				spawnBlockIndicator.gameObject.SetActive(true);
				destroyBlockIndicator.gameObject.SetActive(true);

				spawnBlockIndicator.position = previousPosition;
				destroyBlockIndicator.position = new Vector3(Mathf.FloorToInt(currentPosition.x), Mathf.FloorToInt(currentPosition.y), Mathf.FloorToInt(currentPosition.z));
				return;
			}

			previousPosition.x = Mathf.FloorToInt(currentPosition.x);
			previousPosition.y = Mathf.FloorToInt(currentPosition.y);
			previousPosition.z = Mathf.FloorToInt(currentPosition.z);

			step += RAYCAST_STEP;
		}

		spawnBlockIndicator.gameObject.SetActive(false);
		destroyBlockIndicator.gameObject.SetActive(false);
	}
}