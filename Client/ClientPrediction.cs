using Riptide;
using UnityEngine;

public struct InputPayload
{
    public ushort tick;
	public bool[] inputs;
	public bool pauseMenuState;
	public Vector3 camRotation;
}

public struct StatePayload
{
	public ushort tick;
	public Vector3 playerPosition;
	public Vector3 camRotation;
	public Vector3 camPosition;
}

public class ClientPrediction : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerMovement playerMovement;
	[SerializeField] private PlayerController playerController;
	[SerializeField] private ShootGun gunScript;
	[SerializeField] private CollisionChecks collisionChecks;
	[SerializeField] private PauseMenu pauseMenu;
	[SerializeField] private Transform camTransform;

	// Create private variables for handling comparisons between client and server states
	private const int BUFFER_SIZE = 1024;
	private StatePayload[] stateBuffer;
	private InputPayload[] inputBuffer;
	private StatePayload latestServerState;
	private StatePayload lastProcessedState;

	// Run GetReferences when there's a change in the editor
	private void OnValidate()
	{
		GetReferences();
	}

	// Run GetReferences, then assign stateBuffer & inputBuffer to be capped to BUFFER_SIZE
	private void Start()
	{
		GetReferences();

		stateBuffer = new StatePayload[BUFFER_SIZE];
		inputBuffer = new InputPayload[BUFFER_SIZE];
	}

	// Get script references if their null
	private void GetReferences()
	{
		if(playerMovement == null)
			playerMovement = GetComponent<PlayerMovement>();
		if(playerController == null)
			playerController = GetComponent<PlayerController>();
		if(collisionChecks == null)
			collisionChecks = GetComponent<CollisionChecks>();
		if(pauseMenu == null)
			pauseMenu = GetComponent<PauseMenu>();
	}

	// Save the most recent StatePayload from the server into latestServerState
	public void OnServerMovementState(ushort tick, Vector3 playerPosition, Vector3 camRotation, Vector3 camPosition)
	{
		latestServerState.tick = tick;
		latestServerState.playerPosition = playerPosition;
		latestServerState.camRotation = camRotation;
		latestServerState.camPosition = camPosition;
	}

	public void HandleTick()
	{
		// Check if latestServerState isn't empty and lastProcessedState is either empty or not equal to latestServerState
		if(!latestServerState.Equals(default(StatePayload)) &&
			(lastProcessedState.Equals(default(StatePayload)) ||
			!latestServerState.Equals(lastProcessedState)))
		{
			HandleServerReconciliation();
		}

		int bufferIndex = NetworkManager.Singleton.ClientTick % BUFFER_SIZE;

		// Add payload to inputBuffer
		InputPayload inputPayload = new InputPayload();
		inputPayload.tick = NetworkManager.Singleton.ClientTick;
		inputPayload.inputs = playerController.GetInputs();
		inputPayload.pauseMenuState = pauseMenu.PauseMenuState;
		inputPayload.camRotation = camTransform.forward;
		inputBuffer[bufferIndex] = inputPayload;

		// Add payload to stateBuffer
		stateBuffer[bufferIndex] = playerMovement.ProcessMovement(inputPayload);

		// Run other scripts every tick
		gunScript.ProcessGuns(inputPayload);
		collisionChecks.CheckCollisions();

		SendPrediction(inputPayload);
	}

	private void HandleServerReconciliation()
	{
		lastProcessedState = latestServerState;

		int serverStateBufferIndex = latestServerState.tick % BUFFER_SIZE;
		float positionError = Vector3.Distance(latestServerState.playerPosition, stateBuffer[serverStateBufferIndex].playerPosition);

		if(positionError > 0.001f)
		{
			Debug.Log("Reconciling Server and Client. Position Error: " + positionError);

			// Rewind & Replay
			transform.position = latestServerState.playerPosition;
			Physics.SyncTransforms();

			// Update buffer at index of latest server state
			stateBuffer[serverStateBufferIndex] = latestServerState;

			// Now re-simulate the rest of the ticks up to the current tick on the client
			int tickToProcess = latestServerState.tick + 1;

			while(tickToProcess < NetworkManager.Singleton.ClientTick)
			{
				int bufferIndex = tickToProcess % BUFFER_SIZE;

				// Process new movement with reconciled state
				StatePayload statePayload = playerMovement.ProcessMovement(inputBuffer[bufferIndex]);

				// Update buffer with recalculated state
				stateBuffer[bufferIndex] = statePayload;

				tickToProcess++;
			}
		}
	}

	#region Messages

	private void SendPrediction(InputPayload inputPayload)
	{
		Message message = Message.Create(MessageSendMode.Unreliable, ClientToServerId.clientPrediction);
		message.AddUShort(inputPayload.tick);
		message.AddBools(inputPayload.inputs, true);
		message.AddBool(inputPayload.pauseMenuState);
		message.AddVector3(inputPayload.camRotation);
		NetworkManager.Singleton.Client.Send(message);
	}

	#endregion
}
