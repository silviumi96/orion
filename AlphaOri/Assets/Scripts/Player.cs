﻿using UnityEngine;

public class Player : MonoBehaviour
{
	public bool isGrounded;

	private Transform cam;
	private World world;

	public float walkSpeed = 10f;
	public float jumpForce = 5f;
	public float gravity = -9.8f;

	public float playerWidth = 0.15f;
	public float boundsTolerance = 0.1f;

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
		// Affect vertical momentum with gravity.
		if (verticalMomentum > gravity)
			verticalMomentum += Time.deltaTime * gravity;

		velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.deltaTime * walkSpeed;

		// Apply vertical momentum (falling/jumping).
		velocity += Vector3.up * verticalMomentum * Time.deltaTime;

		if ((velocity.z > 0 && front) || (velocity.z < 0 && back))
			velocity.z = 0;
		if ((velocity.x > 0 && right) || (velocity.x < 0 && left))
			velocity.x = 0;

		if (velocity.y < 0)
			velocity.y = checkDownSpeed(velocity.y);
		else if (velocity.y > 0)
			velocity.y = checkUpSpeed(velocity.y);
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
			// Destroy block.
			if (Input.GetMouseButtonDown(0))
				world.GetChunkFromVector3(highlightBlock.position).EditVoxel(highlightBlock.position, 0);

			// Place block.
			if (Input.GetMouseButtonDown(1))
			{
				//ToDo
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

	private float checkDownSpeed(float downSpeed)
	{
		if (
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth))
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
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth)) ||
			world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth))
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
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth))
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
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
				world.CheckForVoxel(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth))
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
				world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z))
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
				world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
				world.CheckForVoxel(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z))
				)
				return true;
			else
				return false;
		}
	}
}
