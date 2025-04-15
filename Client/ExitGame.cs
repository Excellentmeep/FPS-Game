using UnityEngine;

public class ExitGame : MonoBehaviour
{
    // Close the application if the "Quit" button is pressed on the main menu
    public void DoExitGame()
    {
        Application.Quit();
    }
}
