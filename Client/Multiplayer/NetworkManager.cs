using Riptide;
using Riptide.Utils;
using System;
using TMPro;
using UnityEngine;

// Create client enum for recieving messages from the server
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

// Create client enum for sending messages to the server
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
	public Client Client { get; private set; }
	public float ClientTimer { get; private set; }
	public float MinTimeBetweenTicks { get; private set; }

	// Create public getter for ClientTick and assign Interpolationtick as well
	private ushort _clientTick;
	public ushort ClientTick
	{
		get => _clientTick;
		private set
		{
			_clientTick = value;
			InterpolationTick = (ushort) (value - TicksBetweenPositionUpdates);
		}
	}

	// Create public getter for InterpolationTick to be used in the "Interpolator" script
	public ushort InterpolationTick { get; private set; }

	// Create public getter for TicksBetweenPositionUpdates that assigns InterpolationTick as well
	private ushort _ticksBetweenPositionUpdates = 1;
	public ushort TicksBetweenPositionUpdates
	{
		get => _ticksBetweenPositionUpdates;
		private set
		{
			_ticksBetweenPositionUpdates = value;
			InterpolationTick = (ushort) (ClientTick - value);
		}
	}

	[Header("References")]
	[SerializeField] private ClientPrediction clientPrediction;
	[SerializeField] private GameObject mainMenu;
	[SerializeField] private TMP_InputField ip;
	[SerializeField] private ushort port;
	[Space(10)]
	[SerializeField] private ushort tickDivergenceTolerance = 1;

	// Private variable to determine if the client has connected or not
	private bool connected;
	private const float SERVER_TICK_RATE = 50;

	// Assign Singleton to the NetworkManager class
	private void Awake()
	{
		Singleton = this;
	}

	private void Start()
	{
		MinTimeBetweenTicks = 1 / SERVER_TICK_RATE;

		// Make the NetworkManager gameObject and the MainMenu gameObject not get destroyed when changing scenes
		DontDestroyOnLoad(gameObject);
		DontDestroyOnLoad(mainMenu);

		// Initialize debug logger
		RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

		// Assign Client and attach methods to Client
		Client = new Client();
		Client.Connected += DidConnect;
		Client.ConnectionFailed += FailedToConnect;
		Client.ClientDisconnected += PlayerLeft;
		Client.Disconnected += DidDisconnect;

		// Clear reference for clientPrediction
		clientPrediction = FindObjectOfType<ClientPrediction>();

		connected = false;
		ClientTick = 1;
	}

	// Increment ClientTick at a fixed rate, and get references
	private void Update()
	{
		ClientTimer += Time.deltaTime;

		if(connected && clientPrediction == null)
			clientPrediction = FindObjectOfType<ClientPrediction>();

		while(ClientTimer >= MinTimeBetweenTicks)
		{
			ClientTimer -= MinTimeBetweenTicks;

			Client.Update();

			if(connected && clientPrediction != null)
				clientPrediction.HandleTick();

			ClientTick++;
		}
	}

	// Disconnect the client from the server when closing the application
	private void OnApplicationQuit()
	{
		Client.Disconnect();
	}

	// Connect the client to the server
	public void Connect()
	{
		Client.Connect($"{ip.text}:{port}");
	}

	// When the client does connect to the server send the player's name to the server and set connected bool to true
	private void DidConnect(object sender, EventArgs e)
	{
		UIManager.Singleton.SendName();
		connected = true;
	}

	// If the client fails to connect to the server go back to the main menu
	private void FailedToConnect(object sender, EventArgs e)
	{
		UIManager.Singleton.BackToMain();
	}

	// If a player leaves the server destroy that player's gameobject
	private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
	{
		if(Player.list.TryGetValue(e.Id, out Player player))
			Destroy(player.gameObject);
	}

	// If the player disconnects from the server, go back to main menu, destroy all the player gameobjects, then set connected bool to false
	private void DidDisconnect(object sender, EventArgs e)
	{
		UIManager.Singleton.BackToMain();
		foreach (Player player in Player.list.Values) 
			Destroy(player.gameObject);
		connected = false;
	}

	// If the ClientTick is off from the ServerTick then change the ClientTick to the ServerTick, same with the ClientTimer
	private void SetTick(ushort serverTick, float serverTimer)
	{
		if(Mathf.Abs(ClientTick - serverTick) > tickDivergenceTolerance)
			Debug.Log($"Client tick: {ClientTick} -> {serverTick}");
			ClientTick = serverTick;

		if(Mathf.Abs(ClientTimer - serverTimer) > 0.001f)
		{
			ClientTimer = serverTimer;
		}
	}

	// Run the SetTick method when we recieve a message from the server to sync ticks
	[MessageHandler((ushort)ServerToClientId.sync)]
	public static void Sync(Message message)
	{
		Singleton.SetTick(message.GetUShort(), message.GetFloat());
	}
}
