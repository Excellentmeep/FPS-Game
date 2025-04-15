using Riptide;
using Unity.VisualScripting;
using UnityEngine;

public class ManagePlayerHealth : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Player player;

	private bool isPlayerAlive;
	private float playerHealth;

	// Get a reference to the "Player" script if player is null, then assign isPlayerAlive & playerHealth
	private void Start()
	{
		if(player == null)
			player = GetComponent<Player>();

		isPlayerAlive = true;
		playerHealth = 100;
	}

	// Damage the player when their shot based off if it's a headshot or not, and check if the player is dead, then send variables to the clients
	public void TakeDamage(float damage, string tag)
	{
		if(tag == "Player Head")
			damage *= 2;

		if(isPlayerAlive)
			playerHealth -= damage;

		if(playerHealth <= 0)
			Die();

		SendHealthVariables();
	}

	// Currently only sets isPlayerAlive to false, but will do more later
	private void Die()
	{
		isPlayerAlive = false;
	}

	// Reliably send the health variables to the clients
	private void SendHealthVariables()
	{
		Message message = Message.Create(MessageSendMode.Reliable, ServerToClientId.playerHealth);
		message.Add(player.Id);
		message.Add(isPlayerAlive);
		message.Add(playerHealth);
		NetworkManager.Singleton.Server.SendToAll(message);
	}
}
