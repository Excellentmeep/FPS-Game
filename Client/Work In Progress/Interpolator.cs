using System.Collections.Generic;
using UnityEngine;

public class Interpolator : MonoBehaviour
{
	// Create serialized private variables for interpolation
	[Header("Variables")]
    [SerializeField] private float timeToReachTarget;
    [SerializeField] private float movementThreshold;

	// Create private TransformUpdate list with no limit to hold future positions to move the player to
    private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

	// Create private variables to hold positions & ticks associated with them
	private TransformUpdate moveTo;
	private TransformUpdate moveFrom;
	private TransformUpdate previouslyMovedTo;

	private float squareMovementThreshold;
	private float timeSinceLastUpdate;

	// Assign variables on start
	private void Start()
	{
		//timeSinceLastUpdate = 0f;
		squareMovementThreshold = movementThreshold * movementThreshold;

		moveTo = new TransformUpdate(NetworkManager.Singleton.ClientTick, transform.position);
		moveFrom = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform.position);
		previouslyMovedTo = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform.position);
	}

	// WORK IN PROGRESS

	// Loop through futureTransformUpdates figuring out how long to lerp for that index
	/*private void Update()
	{
		for(int i = 0; i < futureTransformUpdates.Count; i++)
		{
			// Check if the player should move to the position tied to a certain tick
			if(NetworkManager.Singleton.ClientTick >= futureTransformUpdates[i].Tick)
			{
				previouslyMovedTo = moveTo;
				moveTo = futureTransformUpdates[i];
				moveFrom = new TransformUpdate(NetworkManager.Singleton.InterpolationTick, transform.position);

				futureTransformUpdates.RemoveAt(i);
				i--;
				timeSinceLastUpdate = 0f;
				Debug.Log($"moveTo tick: {moveTo.Tick}, moveFrom tick: {moveFrom.Tick}");
				timeToReachTarget = (moveTo.Tick - moveFrom.Tick) * Time.fixedDeltaTime;
			}
			timeSinceLastUpdate += Time.deltaTime;
			InterpolationPosition(timeSinceLastUpdate / timeToReachTarget);
		}
	}*/

	// Interpolate the player from their current position to their next position
	private void InterpolationPosition(float lerpAmount)
	{
		// Check if the difference between the last 2 positions for the player to move to is too small
		if((moveTo.Position - previouslyMovedTo.Position).sqrMagnitude < squareMovementThreshold)
		{
			if(moveTo.Position != moveFrom.Position)
				transform.position = Vector3.Lerp(moveFrom.Position, moveTo.Position, lerpAmount);

			return;
		}

		transform.position = Vector3.LerpUnclamped(moveFrom.Position, moveTo.Position, lerpAmount);
	}

	// Add a new TransformUpdate recieved from the server into futureTransformUpdates
	public void NewUpdate(ushort serverTick, Vector3 position)
	{
		if(serverTick <= NetworkManager.Singleton.InterpolationTick)
			return;

		// Check if the current TransformUpdate needs to be inserted somewhere in futureTransformUpdates rather than put at the back
		for(int i = 0; i < futureTransformUpdates.Count; i++)
		{
			if(serverTick < futureTransformUpdates[i].Tick)
			{
				futureTransformUpdates.Insert(i, new TransformUpdate(serverTick, position));
				return;
			}
		}

		futureTransformUpdates.Add(new TransformUpdate(serverTick, position));
	}
}
