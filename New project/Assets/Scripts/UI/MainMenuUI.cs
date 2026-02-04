using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace KartGame.UI
{
    [DisallowMultipleComponent]
    public class MainMenuUI : MonoBehaviour
    {
        public string lobbySceneName = "LobbyScene";

        [Header("Panels")]
        public GameObject mainPanel;
        public GameObject settingsPanel;

        [Header("Buttons")]
        public Button playButton;
        public Button settingsButton;
        public Button quitButton;
        public Button settingsBackButton;

        private void Awake()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.AddListener(OnBackFromSettings);
            }

            ShowMain();
        }

        public void OnPlayClicked()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        public void OnSettingsClicked()
        {
            ShowSettings();
        }

        public void OnBackFromSettings()
        {
            ShowMain();
        }

        public void OnQuitClicked()
        {
            Application.Quit();
        }

        private void ShowMain()
        {
            if (mainPanel != null) mainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void ShowSettings()
        {
            if (mainPanel != null) mainPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }
    }
}
