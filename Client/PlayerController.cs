using Riptide;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	// Create a private array to hold the players inputs
    private bool[] inputs;

	// Assign the inputs array to have a cap
	private void Start()
	{
		inputs = new bool[7];
	}

	// Get the players inputs then store them in the inputs array
	public bool[] GetInputs()
	{
		for(int i = 0; i < inputs.Length; i++)
		{
			inputs[i] = false;
		}

		if(Input.GetKey(KeyCode.W))
			inputs[0] = true;

		if(Input.GetKey(KeyCode.S))
			inputs[1] = true;

		if(Input.GetKey(KeyCode.A))
			inputs[2] = true;

		if(Input.GetKey(KeyCode.D))
			inputs[3] = true;

		if(Input.GetButton("Jump"))
			inputs[4] = true;

		if(Input.GetKey(KeyCode.LeftControl))
			inputs[5] = true;

		if(Input.GetButton("Fire1"))
			inputs[6] = true;

		return inputs;
	}
}
