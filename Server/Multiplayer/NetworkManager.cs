using Riptide;
using Riptide.Utils;
using System.Threading;
using UnityEngine;

// Create server enum for sending messages to the clients
public enum ServerToClientId : ushort
{
	sync = 1,
	activeScene,
	playerSpawned,
	playerMovement,
	playerRotation,
	animationId,
	clientPrediction,
	shootGun,
	playerHealth,
}

// Create server enum for recieving messages from the clients
public enum ClientToServerId : ushort
{
	name = 1,
	input,
	clientPrediction,
}

public class NetworkManager : MonoBehaviour
{
	// Create a public getter for NetworkManager, and if there's a duplicate NetworkManager destroy it
	private static NetworkManager _singleton;
	public static NetworkManager Singleton
	{
		get => _singleton;
		private set
		{
			if(_singleton == null)
			   _singleton = value;
			else if(_singleton != value)
			{
				Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
				Destroy(value);
			}
		}
	}

	// Create public getters to be accessed in other scripts
	public Server Server { get; private set; }
	public ushort CurrentTick { get; private set; } = 0;
	public float MinTimeBetweenTicks { get; private set; }

	private float serverTimer;
	private const float SERVER_TICK_RATE = 50;

	[SerializeField] private ClientPrediction[] clientPrediction;
	[SerializeField] private ushort port;
	[SerializeField] private ushort maxClientCount;

	// Assign Singleton to the NetworkManager class
	private void Awake()
	{
		Singleton = this;
	}

	private void Start()
	{
		MinTimeBetweenTicks = 1 / SERVER_TICK_RATE;

		clientPrediction = new ClientPrediction[maxClientCount];

		Application.targetFrameRate = 60;

		// Initialize debug logger
		RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

		// Assign the server and attached methods to the server
		Server = new Server();
		Server.Start(port, maxClientCount);
		Server.ClientDisconnected += PlayerLeft;
		Server.ClientConnected += PlayerConnected;
	}

	// Update the server & the current tick at a fixed rate, and send a sync to the clients every 200 ticks
	private void Update()
	{
		serverTimer += Time.deltaTime;

		// Get every player connected to the server their own reference to ClientPrediction
		for(int j = 0;  j < Player.list.Count; j++)
		{
			while(clientPrediction[j] == null)
				clientPrediction[j] = FindObjectOfType<ClientPrediction>();
		}

		while(serverTimer >= MinTimeBetweenTicks)
		{
			serverTimer -= MinTimeBetweenTicks;

			Server.Update();

			if(CurrentTick % 200 == 0)
			{
				SendSync();
			}

			// Run HandleTick for every player connected to the server
			for(int i = 0; i < clientPrediction.Length; i++)
			{
				if(clientPrediction[i] != null)
					clientPrediction[i].HandleTick();
			}

			CurrentTick++;
		}
	}

	private void OnApplicationQuit()
	{
		Server.Stop();
	}

	// Destroy the respective player GameObject when a player disconnects
	private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
	{
		if(Player.list.TryGetValue(e.Client.Id, out Player player))
			Destroy(player.gameObject);
	}

	// Send a sync as soon as a player connects to the server
	private void PlayerConnected(object sender, ServerConnectedEventArgs e)
	{
		SendSync();
	}

	// Sends the CurrentTick & the current serverTimer to all of the clients connected to the server for syncing
	private void SendSync()
	{
		Message message = Message.Create(MessageSendMode.Unreliable, (ushort) ServerToClientId.sync);
		message.Add(CurrentTick);
		message.Add(serverTimer);
		Server.SendToAll(message);
	}
}
