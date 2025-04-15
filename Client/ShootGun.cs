using System.Collections;
using UnityEngine;

public class ShootGun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Transform gunBarrel;
	[SerializeField] private ParticleSystem muzzleFlash;
	[SerializeField] private GameObject hitEffect;
    [SerializeField] private TrailRenderer bulletTrail;
    [SerializeField] private Animator gunAnimator;

    [Header("Variables")]
    [SerializeField] private float damage;
    [SerializeField] private float fireRate;
    [SerializeField] private float range;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float handling;
    [SerializeField] private float recoil;

    // Create private varibles for a particle to show when the player shoots something, and to determine the next time to fire
    private GameObject hitParticle;
    private float nextTimeToFire;

	private void Start()
	{
        nextTimeToFire = 0;
	}

    // Main method that gets ran every tick to process shooting guns
	public void ProcessGuns(InputPayload inputPayload)
	{
        // Determine when the next time to fire is then shoot if that time is reached and the player is holding down the shoot button
        if(inputPayload.inputs[6] && !inputPayload.pauseMenuState && NetworkManager.Singleton.ClientTick >= nextTimeToFire)
        {
            nextTimeToFire = NetworkManager.Singleton.ClientTick + 100 / fireRate;
            Shoot();
        }
	}

    // Plays some visuals for shooting and shoots out a raycast to act as the bullet
    public void Shoot()
    {
        RaycastHit hit;
        muzzleFlash.Play();
        gunAnimator.Play("Recoil", 0);

        // Determine if the raycast hits something then run the SpawnTrail method to show the bulletTrail
        if(Physics.Raycast(playerCamera.position, playerCamera.forward, out hit, range))
        {
            TrailRenderer trail = Instantiate(bulletTrail, gunBarrel.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, true));
        }
		else
		{
			TrailRenderer trail = Instantiate(bulletTrail, gunBarrel.position, Quaternion.identity);
            StartCoroutine(SpawnTrail(trail, hit.point, hit.normal, false));
		}
	}

    // Interpolate the bullet trail from the guntip to the bullets destination
    private IEnumerator SpawnTrail(TrailRenderer trail, Vector3 hitPoint, Vector3 hitNormal, bool madeImpact)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(trail.transform.position, hitPoint);
        float remainingDistance = distance;

        while(remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= bulletSpeed * Time.fixedDeltaTime;

            yield return null;
        }

        // If the raycast hit something then instantiate the hit particle where it hit
        trail.transform.position = hitPoint;
        if(madeImpact)
            hitParticle = Instantiate(hitEffect, hitPoint, Quaternion.LookRotation(hitNormal));

        // Delete the hit particle and trail after long enough
        Destroy(hitParticle, 0.4f);
        Destroy(trail.gameObject, trail.time);
    }
}
