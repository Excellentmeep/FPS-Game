using Riptide;
using UnityEngine;

public class PlayerRotation : MonoBehaviour
{
	[Header("References")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform body;
    [SerializeField] private Transform arms;
    [SerializeField] private Transform legs;
    [SerializeField] private Transform camProxy;
    [SerializeField] private Transform orientation;
	[SerializeField] private Player player;
	[SerializeField] private RotateHips rotateHips;
	[SerializeField] private Rigidbody rb;

	// Run GetReferences when there's a change made in the editor
	private void OnValidate()
	{
		GetReferences();
	}

	// Run GetReferences on start
	private void Start()
	{
		GetReferences();
	}

	// Get references if their null
	private void GetReferences()
	{
		if(player == null)
			player = GetComponent<Player>();
		if(rotateHips == null)
			rotateHips = GetComponentInChildren<RotateHips>();
		if(rb == null)
			rb = GetComponent<Rigidbody>();
	}

	// Rotate the bodyparts of the player to the correct place then send that rotation to the clients
	public void ProcessRotation()
	{
		orientation.eulerAngles = new Vector3(0, camProxy.eulerAngles.y, 0);
		head.eulerAngles = new Vector3(0, head.eulerAngles.y, camProxy.eulerAngles.x);
		arms.eulerAngles = new Vector3(0, arms.eulerAngles.y, camProxy.eulerAngles.x);
		gameObject.transform.eulerAngles = orientation.eulerAngles;

		SendRotation();
	}

	// Sends the rotation to all the clients to rotate their non-local players every other tick
	private void SendRotation()
	{
		if(NetworkManager.Singleton.CurrentTick % 2 != 0)
			return;

		Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.playerRotation);
		message.AddUShort(player.Id);
		message.AddVector3(head.eulerAngles);
		message.AddVector3(body.eulerAngles);
		message.AddVector3(arms.eulerAngles);
		message.AddVector3(legs.eulerAngles);
		NetworkManager.Singleton.Server.SendToAll(message);
	}
}
