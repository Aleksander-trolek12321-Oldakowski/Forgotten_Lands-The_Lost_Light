using UnityEngine.SceneManagement;
using UnityEngine;

namespace Menu
{
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
}
