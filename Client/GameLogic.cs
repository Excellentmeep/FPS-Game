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

	// Create public getters for the player prefabs to be used in the "Player" script
	public GameObject LocalPlayerPrefab => localPlayerPrefab;
	public GameObject PlayerPrefab => playerPrefab;

	[Header("Prefabs")]
	[SerializeField] private GameObject localPlayerPrefab;
	[SerializeField] private GameObject playerPrefab;

	// Assign Singleton to the GameLogic class
	private void Awake()
	{
		Singleton = this;
	}
}
