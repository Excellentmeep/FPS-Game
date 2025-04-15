using Riptide;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
	[SerializeField] private PlayerRotation playerRotation;
	[SerializeField] private PlayerAnimations playerAnimations;
	[SerializeField] private CollisionChecks collisionChecks;
    [SerializeField] private Player player;
    [SerializeField] private ShootGun gunScript;

    private const int BUFFER_SIZE = 1024;
    private StatePayload[] stateBuffer;
    private Queue<InputPayload> inputQueue;

    // Run GetReferences when there's a change made in the editor
	private void OnValidate()
	{
		GetReferences();
	}

    // Run GetReferences, then assign stateBuffer & inputQueue
	private void Start()
	{
		GetReferences();

		stateBuffer = new StatePayload[BUFFER_SIZE];
        inputQueue = new Queue<InputPayload>();
	}

    // Gets references to scripts if their null
    private void GetReferences()
    {
		if(playerMovement == null)
			playerMovement = GetComponent<PlayerMovement>();
		if(playerRotation == null)
			playerRotation = GetComponent<PlayerRotation>();
		if(playerAnimations == null)
			playerAnimations = GetComponent<PlayerAnimations>();
		if(collisionChecks == null)
			collisionChecks = GetComponent<CollisionChecks>();
		if(player == null)
			player = GetComponent<Player>();
	}

    // Takes an InputPayload from the client and enqueues it into inputQueue
    public void OnClientInput(ushort tick, bool[] inputs, bool pauseMenuState, Vector3 forward)
    {
        InputPayload inputPayload;
        inputPayload.tick = tick;
        inputPayload.inputs = inputs;
        inputPayload.pauseMenuState = pauseMenuState;
        inputPayload.camRotation = forward;
        inputQueue.Enqueue(inputPayload);
    }

    // Goes through all of inputQueue, processing the movement from the inputs then saving them into stateBuffer
    public void HandleTick()
    {
        int bufferIndex = -1;
        while(inputQueue.Count > 0)
        {
            InputPayload inputPayload = inputQueue.Dequeue();

            bufferIndex = inputPayload.tick % BUFFER_SIZE;

            StatePayload statePayload = playerMovement.ProcessMovement(inputPayload);

            // Run other scripts every tick
            playerRotation.ProcessRotation();
            gunScript.ProcessGuns(inputPayload);
            playerAnimations.ProcessAnimations(inputPayload.inputs);
            collisionChecks.CheckCollisions();

            stateBuffer[bufferIndex] = statePayload;
        }

		// Sends the StatePayload with the same tick as the inputPayload to all the clients connected
		if(bufferIndex != -1)
        {
			SendStateToClient(stateBuffer[bufferIndex]);
		}
    }

	#region Messages

    // Send a state from stateBuffer to all the clients connected
    private void SendStateToClient(StatePayload statePayload)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.clientPrediction);
        message.AddUShort(player.Id);
        message.AddUShort(statePayload.tick);
        message.AddVector3(statePayload.playerPosition);
        message.AddVector3(statePayload.camRotation);
        message.AddVector3(statePayload.camPosition);
		NetworkManager.Singleton.Server.SendToAll(message);
	}

	#endregion
}
