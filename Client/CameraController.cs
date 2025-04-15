using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Player player;
	[SerializeField] private float sensitivity = 100f;

	// Create private variables for rotating the players camera
	private float verticalRotation;
	private float horizontalRotation;
	private float clampAngle;

	// Run GetReference when there's a change in the editor
	private void OnValidate()
	{
		GetReference();
	}

	// Run GetReference & assign variables
	private void Start()
	{
		GetReference();

		clampAngle = 85;
		verticalRotation = transform.localEulerAngles.x;
		horizontalRotation = player.transform.eulerAngles.y;
	}

	// Get "Player" script reference if it's null
	private void GetReference()
	{
		if(player == null)
			player = GetComponentInParent<Player>();
	}

	// Run the "Look" method if the players cursor is locked & draw a green ray forward from the players camera for debugging
	private void Update()
	{
		if(Cursor.lockState == CursorLockMode.Locked)
			Look();

		Debug.DrawRay(transform.position, transform.forward * 2f, Color.green);
	}

	// Move the players camera based off their mouse movement
	private void Look()
	{
		float mouseVertical = -Input.GetAxis("Mouse Y");
		float mouseHorizontal = Input.GetAxis("Mouse X");

		verticalRotation += mouseVertical * sensitivity * Time.deltaTime;
		horizontalRotation += mouseHorizontal * sensitivity * Time.deltaTime;

		verticalRotation = Mathf.Clamp(verticalRotation, -clampAngle, clampAngle);

		transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
		player.transform.rotation = Quaternion.Euler(0f, horizontalRotation, 0f);
	}
}