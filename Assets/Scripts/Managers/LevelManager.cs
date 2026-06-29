using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene(0);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }    
}
