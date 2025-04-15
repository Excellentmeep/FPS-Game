using UnityEngine;

public class GameLogic : MonoBehaviour
{
	// Create a public getter for GameLogic, and if there's a duplicate GameLogic destroy it
	private static GameLogic _singleton;
	public static GameLogic Singleton
	{
		get => _singleton;
		private set
		{
			if(_singleton == null)
				_singleton = value;
			else if(_singleton != value)
			{
				Debug.Log($"{nameof(GameLogic)} instance already exists, destroying duplicate!");
				Destroy(value);
			}
		}
	}

	// Create a public getter for the player prefab to be used in the "Player" script
	public GameObject PlayerPrefab => playerPrefab;

	[Header("Prefabs")]
	[SerializeField] private GameObject playerPrefab;

	// Assign the Singleton to the GameLogic class
	private void Awake()
	{
		Singleton = this;
	}
}
