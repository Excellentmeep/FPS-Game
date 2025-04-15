using UnityEngine;

public class PlayerAnimations : MonoBehaviour
{
	[SerializeField] private Animator animator;

	// Run GetReference when there's a change in the editor
	private void OnValidate()
	{
		GetReference();
	}

	// Run GetReference on start
	private void Start()
	{
		GetReference();
	}

	// Get a reference to the animator if it's null
	private void GetReference()
	{
		if(animator == null)
			animator = GetComponentInChildren<Animator>();
	}

	// Method that gets called in the Player script that changes non-local players animations to what the server wants
	public void ChangeAnimId(int id)
	{
		animator.SetInteger("ID", id);
	}
}
