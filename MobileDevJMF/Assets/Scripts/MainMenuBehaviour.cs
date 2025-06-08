using UnityEngine;
using UnityEngine.SceneManagement; // LoadScene
using TMPro; // show that score

public class MainMenuBehaviour : MonoBehaviour
{
    /// <summary>
    /// Will load a new scene upon being called
    /// </summary>
    /// <param name="levelName">The name of the level
    /// we want to go to</param>
    /// 
    public TextMeshProUGUI highScoreText;
    public GameObject controlPanel; // use this in chapter 12


    public void LoadLevel(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    public void ResetScore()
    {
        PlayerPrefs.SetInt("score", 0);
        GetAndDisplayScore();
    }

    public void Start()
    {
        // check for a high score and set it to our TMProUGUI
        GetAndDisplayScore();

    }

    private void GetAndDisplayScore()
    {
        highScoreText.text = "High Score: " + PlayerPrefs.GetInt("score").ToString();
    }

}