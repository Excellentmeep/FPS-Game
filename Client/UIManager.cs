using Riptide;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
	// Create a public getter for UIManager, and if there's a duplicate UIManager destroy it
	private static UIManager _singleton;
	public static UIManager Singleton
	{
		get => _singleton;
		private set
		{
			if(_singleton == null)
				_singleton = value;
			else if(_singleton != value)
			{
				Debug.Log($"{nameof(UIManager)} instance already exists, destroying duplicate!");
				Destroy(value);
			}
		}
	}

	[Header("Connect")]
	[SerializeField] private GameObject connectUI;
	[SerializeField] private TMP_InputField usernameField;

	// Assign Singleton to the UIManager class
	private void Awake()
	{
		Singleton = this;
	}

	/* When the "Connect" button on the main menu is clicked,
	 * disable & hide the main menu,
	 * hide & lock the players cursor,
	 * and run NetworkManagers Connect Method */
	public void ConnectClicked()
	{
		usernameField.interactable = false;
		connectUI.SetActive(false);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;

		NetworkManager.Singleton.Connect();
	}

	// Enable and show the main menu when the player goes back to the main menu, and return back to the main menu scene
	public void BackToMain()
	{
		SceneManager.LoadScene(0);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		usernameField.interactable = true;
		connectUI.SetActive(true);
	}

	// Send the players username to the server when they connect
	public void SendName()
	{
		Message message = Message.Create(MessageSendMode.Reliable, ClientToServerId.name);
		message.AddString(usernameField.text);
		NetworkManager.Singleton.Client.Send(message);
	}
}
