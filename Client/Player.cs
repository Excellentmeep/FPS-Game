using Riptide;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
	// Create dictionary to hold the players and their respective Id
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

	// Create public getters for being used where private variables can't be accessed
    public ushort Id { get; private set; }
    public bool IsLocal { get; private set; }
	public PlayerAnimations Animations => playerAnimations;
	public ShootGun GunScript => shootGun;

	[Header("References")]
	[SerializeField] private Transform camTransform;
	[SerializeField] private Interpolator interpolator;
	[SerializeField] private PlayerAnimations playerAnimations;
	[SerializeField] private ClientPrediction clientPrediction;
	[SerializeField] private ShootGun shootGun;

	[Header("Limbs To Rotate")]
	[SerializeField] private Transform head;
	[SerializeField] private Transform body;
	[SerializeField] private Transform arms;
	[SerializeField] private Transform legs;

	private string username;

	// Run GetReferences when there's a change in the editor
	private void OnValidate()
	{
		GetReferences();
	}

	// Run GetReferences on start
	private void Start()
	{
		GetReferences();
	}

	// Get script references if their null
	private void GetReferences()
	{
		if(interpolator == null)
			interpolator = GetComponent<Interpolator>();
		if(playerAnimations == null)
			playerAnimations = GetComponent<PlayerAnimations>();
		if(clientPrediction == null)
			clientPrediction = GetComponent<ClientPrediction>();
	}

	// Remove a player from the dictionary when they disconnect from the server
	private void OnDestroy()
	{
		list.Remove(Id);
	}

	// Move the non-local players to where the server wants them
	private void Move(ushort tick, Vector3 playerPosition, Vector3 camRotation, Vector3 camPosition)
	{
		if(!IsLocal)
		{
			camTransform.forward = camRotation;
			transform.position = playerPosition;
			Physics.SyncTransforms();
		}

		if(IsLocal)
			clientPrediction.OnServerMovementState(tick, playerPosition, camRotation, camPosition);
	}

	// Rotate the non-local players to the direction the server wants them to face
	private void Rotate(Vector3 headRotation, Vector3 bodyRotation, Vector3 armRotation, Vector3 legRotation)
	{
		if(!IsLocal)
		{
			head.eulerAngles = headRotation;
			body.eulerAngles = bodyRotation;
			arms.eulerAngles = armRotation;
			legs.eulerAngles = legRotation;
		}
	}

	// Spawn in the players checking if their local or not and add that player with it's Id to the dictionary
	public static void Spawn(ushort id, string username, Vector3 position)
	{
		Player player;
		if(id == NetworkManager.Singleton.Client.Id)
		{
			player = Instantiate(GameLogic.Singleton.LocalPlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
			player.IsLocal = true;
		}
		else
		{
			player = Instantiate(GameLogic.Singleton.PlayerPrefab, position, Quaternion.identity).GetComponent<Player>();
			player.IsLocal = false;
		}

		player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
		player.Id = id;
		player.username = username;

		list.Add(id, player);
	}

	// What to do when we recieve messages from the server
	#region Messages

	// Run the Spawn Method with the players Id, Username, and position to spawn them at
	[MessageHandler((ushort)ServerToClientId.playerSpawned)]
	private static void SpawnPlayer(Message message)
	{
		Spawn(message.GetUShort(), message.GetString(), message.GetVector3());
	}

	// Run the Move method with the Id of what player to move and the tick, new position, and the direction that's forward
	[MessageHandler((ushort)ServerToClientId.clientPrediction)]
	private static void PlayerMovement(Message message)
	{
		if(list.TryGetValue(message.GetUShort(), out Player player))
			player.Move(message.GetUShort(), message.GetVector3(), message.GetVector3(), message.GetVector3());
	}

	// Run the Rotate method with the Id of what player to rotate and the head, body, arm, and leg rotation
	[MessageHandler((ushort)ServerToClientId.playerRotation)]
	private static void PlayerRotation(Message message)
	{
		if(list.TryGetValue(message.GetUShort(), out Player player))
			player.Rotate(message.GetVector3(), message.GetVector3(), message.GetVector3(), message.GetVector3());
	}

	// Change the animation of the non-local players to the animationId that the server is using
	[MessageHandler((ushort) ServerToClientId.animationId)]
	private static void AnimationId(Message message)
	{
		if(list.TryGetValue(message.GetUShort(), out Player player) && !player.IsLocal)
			player.Animations.ChangeAnimId(message.GetInt());
	}

	// If the player that corresponds to the Id of the message isn't local then run that players shoot method for all the other clients to see
	[MessageHandler((ushort) ServerToClientId.shootGun)]
	private static void ShootGun(Message message)
	{
		if(list.TryGetValue(message.GetUShort(), out Player player) && !player.IsLocal)
			player.GunScript.Shoot();
	}

	// Currently disconnects the player when they die, will do something else in the future
	[MessageHandler((ushort) ServerToClientId.playerHealth)]
	private static void PlayerHealth(Message message)
	{
		if(list.TryGetValue(message.GetUShort(), out Player player) && !message.GetBool() && player.IsLocal)
			NetworkManager.Singleton.Client.Disconnect();
	}

	#endregion
}
