using UnityEngine;

public class Player : MonoBehaviour
{
	private const int PLACE_BLOCK_ID = 1;
	public bool isGrounded;

	private Transform cam;
	private World world;

	public float walkSpeed = 10f;
	public float jumpForce = 5f;
	public float gravity = -9.8f;

	public float playerWidth = 0.15f;

	private float horizontal;
	private float vertical;
	private float mouseHorizontal;
	private float mouseVertical;
	private float mouseSpeed = 2;
	private Vector3 velocity;
	private float verticalMomentum = 0;
	private bool jumpRequest;

	public Transform highlightBlock;
	public Transform placeBlock;
	public float checkIncrement = 0.1f;
	public float reach = 8f;

	private void Start()
	{
		cam = GameObject.Find("Main Camera").transform;
		world = GameObject.Find("World").GetComponent<World>();

		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}

	private void Update()
	{
		GetPlayerInputs();
		CalculateVelocity();

		transform.Rotate(Vector3.up * mouseHorizontal * mouseSpeed);
		cam.Rotate(Vector3.right * -mouseVertical * mouseSpeed);
		transform.Translate(velocity, Space.World);

		if (jumpRequest)
			Jump();

		placeCursorBlocks();
	}

	void Jump()
	{
		verticalMomentum = jumpForce;
		isGrounded = false;
		jumpRequest = false;
	}

	private void CalculateVelocity()
	{
		if (verticalMomentum > gravity)
			verticalMomentum += Time.deltaTime * gravity;

		velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.deltaTime * walkSpeed;

		velocity += Vector3.up * verticalMomentum * Time.deltaTime;

		if ((velocity.z > 0 && CheckFrontCollision()) || (velocity.z < 0 && CheckBackCollision()))
			velocity.z = 0;
		if ((velocity.x > 0 && CheckRightCollision()) || (velocity.x < 0 && CheckLeftCollision()))
			velocity.x = 0;

		if (velocity.y < 0)
			velocity.y = CheckDownwardsVelocity(velocity.y);
		else if (velocity.y > 0)
			velocity.y = CheckUpwardsVelocity(velocity.y);
	}

	private void GetPlayerInputs()
	{
		mouseHorizontal = Input.GetAxisRaw("Mouse X");
		mouseVertical = Input.GetAxisRaw("Mouse Y");

		if (isGrounded)
		{
			horizontal = Input.GetAxisRaw("Horizontal");
			vertical = Input.GetAxisRaw("Vertical");

			if (Input.GetButtonDown("Jump"))
			{
				jumpRequest = true;
			}
		}

		if (highlightBlock.gameObject.activeSelf)
		{
			if (Input.GetMouseButtonDown(0))
				world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);

			if (Input.GetMouseButtonDown(1))
			{
				world.GetChunkFromVector3(highlightBlock.position).EditVoxel(placeBlock.position, PLACE_BLOCK_ID);
			}
		}
	}

	private void placeCursorBlocks()
	{
		float step = checkIncrement;
		Vector3 lastPos = new Vector3();

		while (step < reach)
		{
			Vector3 pos = cam.position + (cam.forward * step);

			if (world.CheckForVoxel(pos))
			{

				highlightBlock.position = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));
				placeBlock.position = lastPos;

				highlightBlock.gameObject.SetActive(true);
				placeBlock.gameObject.SetActive(true);

				return;

			}

			lastPos = new Vector3(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.y), Mathf.FloorToInt(pos.z));

			step += checkIncrement;

		}

		highlightBlock.gameObject.SetActive(false);
		placeBlock.gameObject.SetActive(false);
	}

	private float CheckDownwardsVelocity(float velocity)
	{
		if (
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + velocity, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + velocity, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + velocity, transform.position.z + playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + velocity, transform.position.z + playerWidth))
		   )
		{
			isGrounded = true;
			return 0;
		}
		else
		{
			isGrounded = false;
			return velocity;
		}
	}

	private float CheckUpwardsVelocity(float velocity)
	{
		if (
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + velocity, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + velocity, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + velocity, transform.position.z + playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + velocity, transform.position.z + playerWidth))
		   )
		{
			return 0;
		}
		else
		{
			return velocity;
		}
	}

	public bool CheckFrontCollision()
	{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
				)
				return true;
			else				
				return false;
	}

	public bool CheckBackCollision()
	{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
				)
				return true;
			else
				return false;
	}

	public bool CheckLeftCollision()
	{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
				)
				return true;
			else
				return false;
	}

	public bool CheckRightCollision()
	{
			if (
				world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
				)
				return true;
			else
				return false;
	}
}
