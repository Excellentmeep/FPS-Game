using Riptide;
using System.Collections;
using UnityEngine;

public class ShootGun : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform camProxy;
	[SerializeField] private Transform gunBarrel;
	[SerializeField] private Animator gunAnimator;
	[SerializeField] private Player playerScript;
	[SerializeField] private LayerMask playerMask;
	[SerializeField] private LayerMask headAndBodyMask;

	[Header("Variables")]
	[SerializeField] private float damage;
	[SerializeField] private float fireRate;
	[SerializeField] private float range;
	[SerializeField] private float handling;
	[SerializeField] private float recoil;

	// Create a private variable to determine the next time to fire
	private float nextTimeToFire;

	private void Start()
	{
		nextTimeToFire = 0;
	}

	// Main method that gets ran every tick to process shooting guns
	public void ProcessGuns(InputPayload inputPayload)
	{
		// Determine when the next time to fire is then shoot if that time is reached and the player is holding down the shoot button
		if(inputPayload.inputs[6] && !inputPayload.pauseMenuState && NetworkManager.Singleton.CurrentTick >= nextTimeToFire)
		{
			nextTimeToFire = NetworkManager.Singleton.CurrentTick + 100 / fireRate;
			Shoot();
		}
	}

	// Handle players shooting each other then send the gunshot to the clients
	private void Shoot()
	{
		gunAnimator.Play("Recoil", 0);

		// Check if both raycasts hit, the first to check what player was hit and the other to determine a head or body shot
		if(Physics.Raycast(camProxy.position, camProxy.forward, out RaycastHit playerHit, range, playerMask) && Physics.Raycast(camProxy.position, camProxy.forward, out RaycastHit hit, range, headAndBodyMask))
		{
			// Go through the player list to determine what player was hit, then run their player hit method
			foreach(Player player in Player.list.Values)
			{
				if(player.name == playerHit.collider.gameObject.name)
					player.PlayerHit(damage, hit.collider.gameObject.tag);
			}
		}

		SendGunshotToClients();
	}

	#region Messages

	// Send the gunshot to all the other clients to have them display the gunshot
	private void SendGunshotToClients()
	{
		Message message = Message.Create(MessageSendMode.Unreliable, ServerToClientId.shootGun);
		message.AddUShort(playerScript.Id);
		NetworkManager.Singleton.Server.SendToAll(message);
	}

	#endregion
}