using UnityEngine;

public class RotateHips : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform body;

	private float bodyRotation;
	private float legRotation;
	private float difference;
	private float distanceToLoop;
	private float whatToRotateTo;

	public void LegRotation()
	{
		// Get upperbody rotation and leg rotation then figure out the difference as a positive number
		bodyRotation = body.transform.eulerAngles.y;
		legRotation = transform.eulerAngles.y;

		if(legRotation <= 180 && legRotation >= 0)
			difference = Mathf.Abs(bodyRotation - legRotation - 180);

		if(legRotation >= 180 && legRotation <= 360)
			difference = Mathf.Abs(bodyRotation - legRotation + 180);

		/* Check if the players leg rotation is close enough to 360 where rotating right
           means that the players upperbody rotation will jump from 360 to 0          */
		if(legRotation >= 325 && legRotation <= 360)
		{
			/* Get the amount of degrees away from looping back to 0 legRotation is 
               and figure out how far past 0 to rotate the legs                  */
			distanceToLoop = 360 - legRotation;
			whatToRotateTo = 35 - distanceToLoop;

			// Check if the players upperbody is rotated too far from the legs
			if(bodyRotation >= 180 + whatToRotateTo && bodyRotation <= 215)
				RotateLegs(true);
		}

		// Same as the first if statement but checking for the legs going the other way
		else if(legRotation >= 0 && legRotation <= 35)
		{
			distanceToLoop = legRotation;
			whatToRotateTo = 35 - distanceToLoop;

			if(bodyRotation >= 145 && bodyRotation <= 180 - whatToRotateTo)
				RotateLegs(false);
		}

		// Check if the players upperbody is rotated too far from the legs
		if(difference >= 35)
		{
			if(bodyRotation > legRotation)
				RotateLegs(true);
			else if(bodyRotation < legRotation)
				RotateLegs(false);
		}
	}

	// Rotate legs in the correct direction based of bool, true == right, false == left
	private void RotateLegs(bool directionToRotate)
	{
		if(directionToRotate)
		{
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, bodyRotation - 180, transform.eulerAngles.z);
		}
		else if(!directionToRotate)
		{
			transform.eulerAngles = new Vector3(transform.eulerAngles.x, bodyRotation - 180, transform.eulerAngles.z);
		}
	}
}