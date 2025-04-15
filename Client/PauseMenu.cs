using UnityEngine;

public class PauseMenu : MonoBehaviour
{
	// Create references to the pause menu and the players crosshair to enable or disable them
	[Header("References")]
	[SerializeField] private GameObject PauseScreen;
	[SerializeField] private GameObject Crosshair;

	// Create a public getter for pauseMenuState to be used in other scripts
	public bool PauseMenuState { get; private set; }

	private void Start()
	{
		PauseMenuState = false;
	}

	// Bring the pause menu up/down & unlock/lock the cursor when the player hits the escape key
	private void Update()
    {
		if(Input.GetKeyDown(KeyCode.Escape))
		{
			if(!PauseMenuState)
			{
				Crosshair.SetActive(false);
				PauseScreen.SetActive(true);
				PauseMenuState = true;
				Cursor.visible = true;
				Cursor.lockState = CursorLockMode.None;
			}
			else
			{
				Crosshair.SetActive(true);
				PauseScreen.SetActive(false);
				PauseMenuState = false;
				Cursor.visible = false;
				Cursor.lockState = CursorLockMode.Locked;
			}
		}
	}

	// Bring down the pause menu when the player disconnects
	public void Disconnect()
	{
		NetworkManager.Singleton.Client.Disconnect();
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	// Bring down the pause menu if the player hits the "Resume" button instead of hitting the escape key
	public void UpdatePauseMenuState(bool state)
    {
		PauseMenuState = state;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}
}
