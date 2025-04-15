using Riptide;
using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Animator animator;
	[SerializeField] private Rigidbody rb;
	[SerializeField] private Player player;
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private CollisionChecks collisionChecks;

	// Create a private variable for determining which animation to use between idle, fall, and jump
	private float yVelocity;

	// Run GetReferences when there's a change in the editor
	private void OnValidate()
	{
		GetReferences();
	}

	// Run GetReferences & assign inputs to have a cap on start
	private void Start()
	{
		GetReferences();
	}

	// Get references if any of them are null
	private void GetReferences()
	{
		if(collisionChecks == null)
			collisionChecks = GetComponent<CollisionChecks>();
		if(playerMovement == null)
			playerMovement = GetComponent<PlayerMovement>();
		if(animator == null)
			animator = GetComponentInChildren<Animator>();
		if(player == null)
			player = GetComponent<Player>();
		if(rb == null)
			rb = GetComponent<Rigidbody>();
	}

	// Check to see what animation to use then change the animation Id, then send that Id to the clients
	public void ProcessAnimations(bool[] inputs)
	{
		yVelocity = rb.velocity.y * rb.velocity.magnitude;

		// Change Id between idle, jump and fall depending on velocity
		if(rb.velocity.magnitude > -0.5f && rb.velocity.magnitude < 0.5f)
			ChangeAnimID(0);

		if(yVelocity > 0.5f)
			ChangeAnimID(9);

		if(yVelocity < -0.5f)
			ChangeAnimID(10);

		// Diagnal Animations, Order is Frontright, Backleft, Forwardleft, Backright
		if(inputs[0] && !inputs[1] && !inputs[2] && inputs[3])
			ChangeAnimID(5);

		if(!inputs[0] && inputs[1] && inputs[2] && !inputs[3])
			ChangeAnimID(6);

		if(inputs[0] && !inputs[1] && inputs[2] && !inputs[3])
			ChangeAnimID(7);

		if(!inputs[0] && inputs[1] && !inputs[2] && inputs[3])
			ChangeAnimID(8);

		// Straight Animations, Order is Forward, Backward, Left, Right
		if(inputs[0] && !inputs[1] && !inputs[2] && !inputs[3])
			ChangeAnimID(1);

		if(!inputs[0] && inputs[1] && !inputs[2] && !inputs[3])
			ChangeAnimID(2);

		if(!inputs[0] && !inputs[1] && inputs[2] && !inputs[3])
			ChangeAnimID(3);

		if(!inputs[0] && !inputs[1] && !inputs[2] && inputs[3])
			ChangeAnimID(4);

		// Change animation to slide if inputting slide button
		if(inputs[5] && collisionChecks.Grounded)
			ChangeAnimID(11);

		SendAnimationId();
	}

	// Method to change the animation Id
	public void ChangeAnimID(int id)
	{
		animator.SetInteger("ID", id);
	}

	// Send the animation Id to all the clients every other tick for them to update their animations
	private void SendAnimationId()
	{
		if(NetworkManager.Singleton.CurrentTick % 2 != 0)
			return;

		Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.animationId);
		message.AddUShort(player.Id);
		message.AddInt(animator.GetInteger("ID"));
		NetworkManager.Singleton.Server.SendToAll(message);
	}

}
