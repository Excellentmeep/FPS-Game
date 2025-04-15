using Riptide;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Player : MonoBehaviour
{
    // Create a dictionary to hold the players and their respective Id
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();

    // Create public getters for being used in other places where private variables can't be accessed
    public ushort Id { get; private set; }
    public string Username { get; private set; }
    public ClientPrediction Prediction => clientPrediction;

    [Header("References")]
    [SerializeField] private PlayerAnimations animations;
    [SerializeField] private ClientPrediction clientPrediction;
	[SerializeField] private ManagePlayerHealth managePlayerHealth;

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
		if(animations == null)
			animations = GetComponent<PlayerAnimations>();
		if(clientPrediction == null)
			clientPrediction = GetComponent<ClientPrediction>();
		if(managePlayerHealth == null)
			managePlayerHealth = GetComponent<ManagePlayerHealth>();
	}

	// Remove a player from the dictionary when they disconnect from the server
	private void OnDestroy()
	{
		list.Remove(Id);
	}

    // Spawn in the players at the spawnpoint, and add their username & Id to the dictionary
	public static void Spawn(ushort id, string username)
    {
        foreach(Player otherPlayer in list.Values)
            otherPlayer.SendSpawned(id);

        Player player = Instantiate(GameLogic.Singleton.PlayerPrefab, new Vector3(-25f, -10f, -42f), Quaternion.identity).GetComponent<Player>();
        player.name = $"Player {id} ({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        player.Id = id;
        player.Username = string.IsNullOrEmpty(username) ? "Guest" : username;

        player.SendSpawned();
        list.Add(id, player);
    }

	// Accesses the TakeDamage method for a specific player
	public void PlayerHit(float damage, string tag)
	{
		managePlayerHealth.TakeDamage(damage, tag);
	}

	#region Messages

	// Send a message to all the clients connected that a player has been spawned in
	private void SendSpawned()
    {
        NetworkManager.Singleton.Server.SendToAll(AddSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.playerSpawned)));
    }

    // Send a message to the client that spawned in that they have been spawned in
    private void SendSpawned(ushort toClientId)
    {
		NetworkManager.Singleton.Server.Send(AddSpawnData(Message.Create(MessageSendMode.Reliable, ServerToClientId.playerSpawned)), toClientId);
	}

    // Easily create the same message to be used twice
    private Message AddSpawnData(Message message)
    {
		message.AddUShort(Id);
		message.AddString(Username);
		message.AddVector3(transform.position);
        return message;
	}

    // Run the spawn method when the server recieves a message that a player needs to be spawned in
	[MessageHandler((ushort)ClientToServerId.name)]
    private static void Name(ushort fromClientId, Message message)
    {
        Spawn(fromClientId, message.GetString());
    }

    // Run ClientPredictions OnClientInput method when the server recieves a message from a client for client prediction
	[MessageHandler((ushort) ClientToServerId.clientPrediction)]
	private static void InputPrediction(ushort fromClientId, Message message)
	{
        if(list.TryGetValue(fromClientId, out Player player))
            player.Prediction.OnClientInput(message.GetUShort(), message.GetBools(), message.GetBool(), message.GetVector3());
	}

	#endregion
}
