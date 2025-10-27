using UnityEngine.SceneManagement;
using UnityEngine;

public class menu : MonoBehaviour
{
    public void Game()
    {
        SceneManager.LoadScene("HUB");
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("QUIT");
    }
}
