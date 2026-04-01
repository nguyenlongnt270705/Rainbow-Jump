using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// MainMenu.cs: điều hướng scene trong menu chính
namespace RainbowJump.Scripts
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "Main";

        // Flow chính: Play load scene game, Quit thoát app

        // Flow: chuyển sang gameplay scene
        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        // Flow: thoát game (hoặc dừng playmode khi chạy trong Editor)
        public void Quit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
