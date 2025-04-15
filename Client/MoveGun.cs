using Unity.VisualScripting;
using UnityEngine;

public class MoveGun : MonoBehaviour
{
	// Create a serialized private reference to PlayerMovement for accessing some methods
    [SerializeField] private PlayerMovement playerMovement;

	// Create serialized private variables for how long it takes for the gun to move and how much the gun moves
	[SerializeField] private float smoothTime = 0.2f;
	[SerializeField] private float posOffset = 0.002f;

	private Vector3 defaultPos;
    private Vector3 desiredPos;
    private Vector3 velocity = Vector3.zero;

	// On start assign the default position for the gun to return to
	private void Start()
	{
		defaultPos = transform.localPosition;
	}

	// Shift the players gun around slightly based off their movement
	private void Update()
	{
		Vector2 Offset = playerMovement.FindVelRelativeToLook() * posOffset;
		float fallSpeed = playerMovement.GetFallSpeed() * posOffset * 0.05f;
		desiredPos = defaultPos - new Vector3(Offset.x, fallSpeed, Offset.y);
		transform.localPosition = Vector3.SmoothDamp(transform.localPosition, desiredPos, ref velocity, smoothTime);
	}
}
