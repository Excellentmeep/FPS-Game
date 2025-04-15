using UnityEngine;

public class CollisionChecks : MonoBehaviour
{
	[Header("References")]
	[SerializeField] Transform wallCheckTop;
	[SerializeField] Transform wallCheckBottom;
	[SerializeField] Transform wallCheckRightTop;
	[SerializeField] Transform wallCheckLeftTop;
	[SerializeField] Transform camProxy;
	[SerializeField] LayerMask groundMask;
	[SerializeField] LayerMask wallMask;

	// Create a serialized private variable for the steepest angle a slope can be while still being considered a floor
	[Header("Variables")]
	[SerializeField] float maxSlopeAngle;

	// Create public getters to be used in PlayerMovement & PlayerAnimations
	public bool Grounded { get; private set; }
	public bool OnWall { get; private set; }
	public bool OnWallLeft { get; private set; }
	public bool OnWallRight { get; private set; }

	// Create private variables for physics checks with walls & floors
	private bool cancellingGrounded;
	private Vector3 normalVector;
	private Vector3 offset;

	// Assign normalVector & offset to determine collision checks for walls & floors
	private void Start()
	{
		normalVector = Vector3.up;
		offset = new Vector3(0f, 1.25f, 0f);
	}

	// Determine if the player is touching a wall and if the wall is left or right of the player
	public void CheckCollisions()
	{
		OnWall = Physics.CheckCapsule(wallCheckTop.transform.position, wallCheckBottom.transform.position, 0.575f, wallMask);
		OnWallLeft = Physics.CheckCapsule(wallCheckLeftTop.transform.position, wallCheckLeftTop.transform.position - offset, 0.3f, wallMask);
		OnWallRight = Physics.CheckCapsule(wallCheckRightTop.transform.position, wallCheckRightTop.transform.position - offset, 0.3f, wallMask);
	}

	// Determine if a surface the player is standing on is at a shallow enough angle to be considered a floor
	private bool IsFloor(Vector3 v)
	{
		float angle = Vector3.Angle(Vector3.up, v);
		return angle < maxSlopeAngle;
	}

	private void OnCollisionStay(Collision other)
	{
		// Make sure we are only checking for walkable layers
		int layer = other.gameObject.layer;
		if(groundMask != (groundMask | (1 << layer))) return;

		// Loop through every collision in a physics update
		for(int i = 0; i < other.contactCount; i++)
		{
			Vector3 normal = other.contacts[i].normal;
			// FLOOR
			if(IsFloor(normal))
			{
				Grounded = true;
				cancellingGrounded = false;
				normalVector = normal;
				CancelInvoke(nameof(StopGrounded));
			}
		}

		// Invoke ground/wall cancel, since we can't check normals with CollisionExit
		float delay = 3f;
		if(!cancellingGrounded)
		{
			cancellingGrounded = true;
			Invoke(nameof(StopGrounded), Time.deltaTime * delay);
		}
	}

	private void StopGrounded()
	{
		Grounded = false;
	}
}
