using UnityEngine;

// Require the players rigidbody component to run
[RequireComponent(typeof(Rigidbody))]

public class PlayerMovement : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CollisionChecks collisionCheck;
	[SerializeField] private Player player;
	[SerializeField] private Transform camTransform;
	[SerializeField] private Rigidbody rb;
	[SerializeField] private BoxCollider[] hitboxes;

	[Header("Variables")]
	[SerializeField] private float airSpeedCap;
	[SerializeField] private float groundSpeedCap;
	[SerializeField] private float jumpForce;

	private Vector3 slideDirection;
	private bool[] wallsForPush;
	private bool readyToJump;
	private bool sliding;
	private float wallSpeedCap;
	private float pushBackSpeed;
	private float moveSpeed;
	private float maxSpeed;
	private float counterMovement;
	private float threshold;
	private float groundedCounter;
	private float wallJumpCounter;

	// Assign references & variables when there's a change in the editor
	private void OnValidate()
	{
		AssignReferencesAndVariables();
	}

	// Assign references & variables on start
	private void Start()
	{
		AssignReferencesAndVariables();
	}

	// Get references & assign variables
	private void AssignReferencesAndVariables()
	{
		// References
		if(collisionCheck == null)
			collisionCheck = GetComponent<CollisionChecks>();
		if(rb == null)
			rb = GetComponent<Rigidbody>();
		if(player == null)
			player = GetComponent<Player>();

		// Variables
		readyToJump = true;
		moveSpeed = 4000; 
		pushBackSpeed = moveSpeed;
		maxSpeed = groundSpeedCap;
		wallSpeedCap = airSpeedCap + 5;
		counterMovement = 0.175f;
		threshold = 0.1f;
	}

	// Processes all the movements then returns a new StatePayload to save into stateBuffer inside of ClientPrediction
	public StatePayload ProcessMovement(InputPayload inputPayload)
	{
		SpeedChanges();

		Vector2 velocity = FindVelRelativeToLook();

		Vector2 inputDirection = Inputs(velocity.x, velocity.y, inputPayload.inputs);

		CounterMovement(inputDirection.x, inputDirection.y, velocity);

		Move(inputDirection);

		return new StatePayload()
		{
			tick = inputPayload.tick,
			playerPosition = transform.position,
			camPosition = camTransform.position,
			camRotation = camTransform.forward,
		};
	}

	// Move the player in the correct direction or slide them if their sliding
	private void Move(Vector2 inputDirection)
	{
		Vector3 moveDirection = Vector3.Normalize(camTransform.right * inputDirection.x + Vector3.Normalize(FlattenVector3(camTransform.forward)) * inputDirection.y);
		moveDirection *= moveSpeed * Time.fixedDeltaTime;

		if(!sliding)
			rb.AddForce(moveDirection);

		if(sliding)
			rb.AddForce(slideDirection * maxSpeed * 10f, ForceMode.VelocityChange);
	}

	private void Jump()
	{
		// Check if the player is in position to jump
		if(collisionCheck.Grounded && readyToJump || collisionCheck.OnWall && readyToJump && wallJumpCounter < 5)
		{
			if(sliding) StopSlide();

			readyToJump = false;
			Vector3 vel = rb.velocity;

			// If the player's just grounded do a normal jump
			if(collisionCheck.Grounded) rb.AddForce(Vector2.up * jumpForce);

			// Jump off a wall to the players left
			if(collisionCheck.OnWall && collisionCheck.OnWallLeft && !collisionCheck.OnWallRight && !collisionCheck.Grounded)
			{
				rb.AddForce(camTransform.transform.right * 600);
				rb.AddForce(Vector2.up * jumpForce);
				wallJumpCounter++;
			}

			// Jump off a wall to the players right
			if(collisionCheck.OnWall && collisionCheck.OnWallRight && !collisionCheck.OnWallLeft && !collisionCheck.Grounded)
			{
				rb.AddForce(-camTransform.transform.right * 600);
				rb.AddForce(Vector2.up * jumpForce);
				wallJumpCounter++;
			}

			// Jump off a wall that's neither to the players left or right and push off in the direction their facing
			if(collisionCheck.OnWall && !collisionCheck.OnWallRight && !collisionCheck.OnWallLeft && !collisionCheck.Grounded)
			{
				rb.AddForce(FlattenVector3(camTransform.forward) * 600);
				rb.AddForce(Vector2.up * jumpForce);
				wallJumpCounter++;
			}

			//If jumping while falling, reset y velocity
			if(rb.velocity.y < 0.5f)
			{
				rb.velocity = new Vector3(vel.x, 0, vel.z);
			}
			else if(rb.velocity.y > 0)
			{
				rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);
			}

			// Reset the jump cooldown depending on what jump the player did
			if(!collisionCheck.OnWall) Invoke(nameof(ResetJump), 0.2f);
			if(collisionCheck.OnWall && collisionCheck.Grounded) Invoke(nameof(ResetJump), 0.2f);
			if(collisionCheck.OnWall && !collisionCheck.Grounded) Invoke(nameof(ResetJump), 0.4f);
		}
	}

	private void ResetJump()
	{
		readyToJump = true;
	}

	// Stores the players inputs into a Vector2, runs Jump(), runs StartSlide()/StopSlide(), and limits the players max speed
	private Vector2 Inputs(float xVelocity, float zVelocity, bool[] inputs)
	{
		Vector2 inputDirection = Vector2.zero;

		if(inputs[0] && zVelocity <= maxSpeed)
			inputDirection.y += 1;

		if(inputs[1] && zVelocity >= -maxSpeed)
			inputDirection.y -= 1;

		if(inputs[2] && xVelocity >= -maxSpeed)
			inputDirection.x -= 1;

		if(inputs[3] && xVelocity <= maxSpeed)
			inputDirection.x += 1;

		if(inputs[4])
			Jump();

		if(inputs[5] && collisionCheck.Grounded && !sliding && readyToJump)
			StartSlide();

		if(!inputs[5] && sliding)
			StopSlide();

		return inputDirection;
	}

	// When the player starts sliding store slideDirection, change the hitboxes, and move the player down quickly for the slide
	private void StartSlide()
	{
		sliding = true;
		groundedCounter = 0;
		slideDirection = FlattenVector3(camTransform.forward) * Time.fixedDeltaTime;
		hitboxes[0].enabled = false;
		hitboxes[1].enabled = true;
		hitboxes[2].enabled = true;
		transform.position = new Vector3(transform.position.x, transform.position.y - 1.17085f, transform.position.z);
		Physics.SyncTransforms();
	}

	// When the player stops sliding change the hitboxes back to normal, and move the player back up quickly
	private void StopSlide()
	{
		sliding = false;
		hitboxes[0].enabled = true;
		hitboxes[1].enabled = false;
		hitboxes[2].enabled = false;
		transform.position = new Vector3(transform.position.x, transform.position.y + 1.17085f, transform.position.z);
		Physics.SyncTransforms();
	}

	// Change different things with speed depending on different variables
	private void SpeedChanges()
	{
		// Extra gravity so the player stays grounded
		rb.AddForce(Vector3.down * Time.fixedDeltaTime * 10);

		// Reduce maxSpeed while the player is sliding and over the groundSpeedCap
		if(sliding && maxSpeed > groundSpeedCap)
			maxSpeed -= 0.25f;
		if(sliding && maxSpeed < groundSpeedCap)
			maxSpeed = groundSpeedCap;

		// If the player is airborne, increase their maxSpeed up to airSpeedCap
		if(!collisionCheck.Grounded && !collisionCheck.OnWall && maxSpeed < airSpeedCap)
			maxSpeed += 0.2f;
		if(!collisionCheck.Grounded && !collisionCheck.OnWall && maxSpeed > airSpeedCap)
			maxSpeed = airSpeedCap;

		// If the player is airborne and on a wall, increase their maxSpeed x2 faster up to wallSpeedCap, (airSpeedCap + 5)
		if(!collisionCheck.Grounded && collisionCheck.OnWall && maxSpeed < wallSpeedCap)
			maxSpeed += 0.4f;
		if(!collisionCheck.Grounded && collisionCheck.OnWall && maxSpeed > wallSpeedCap)
			maxSpeed = wallSpeedCap;

		// Reset the wall jump counter when you land
		if(collisionCheck.Grounded) wallJumpCounter = 0;

		// Give the player a slight upward force when on a wall so they stay on it longer
		if(collisionCheck.OnWall && !collisionCheck.Grounded) 
			rb.AddForce(Vector2.up * 750 * Time.fixedDeltaTime);

		// Reset the players max speed to groundSpeedCap if grounded for too long
		if(collisionCheck.Grounded && !sliding)
		{
			groundedCounter += 1 * Time.fixedDeltaTime;
			if(groundedCounter > 6 * Time.fixedDeltaTime)
				maxSpeed = this.groundSpeedCap;
		}

		if(!collisionCheck.Grounded) groundedCounter = 0;
	}

	// Reset the y component of the given vector to 0
	private Vector3 FlattenVector3(Vector3 vector)
	{
		vector.y = 0;
		return vector;
	}

	// Get the x & y velocity of the player relative to the direction their facing
	public Vector2 FindVelRelativeToLook()
	{
		float lookAngle = camTransform.transform.eulerAngles.y;
		float moveAngle = Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg;

		float u = Mathf.DeltaAngle(lookAngle, moveAngle);
		float v = 90 - u;

		float magnitude = rb.velocity.magnitude;
		float zMag = magnitude * Mathf.Cos(u * Mathf.Deg2Rad);
		float xMag = magnitude * Mathf.Cos(v * Mathf.Deg2Rad);

		return new Vector2(xMag, zMag);
	}

	// Get the players fall speed, used in the "MoveGun" script
	public float GetFallSpeed()
	{
		return rb.velocity.y * rb.velocity.magnitude;
	}

	// Give the player some friction when moving
	private void CounterMovement(float x, float y, Vector2 velocity)
	{
		// Limit diagonal running.
		if(Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed)
		{
			float fallspeed = rb.velocity.y;
			Vector3 n = rb.velocity.normalized * maxSpeed;
			rb.velocity = new Vector3(n.x, fallspeed, n.z);
		}

		if(!collisionCheck.Grounded || !readyToJump || sliding)
			return;

		// Counter movement
		if(Mathf.Abs(velocity.x) > threshold && Mathf.Abs(x) < 0.05f || (velocity.x < -threshold && x > 0) || (velocity.x > threshold && x < 0))
			rb.AddForce(moveSpeed * FlattenVector3(camTransform.transform.right) * Time.fixedDeltaTime * -velocity.x * counterMovement);

		if(Mathf.Abs(velocity.y) > threshold && Mathf.Abs(y) < 0.05f || (velocity.y < -threshold && y > 0) || (velocity.y > threshold && y < 0))
			rb.AddForce(moveSpeed * FlattenVector3(camTransform.transform.forward) * Time.fixedDeltaTime * -velocity.y * counterMovement);
	}
}
