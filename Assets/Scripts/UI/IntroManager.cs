using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace DualBeat.UI
{
    public class IntroManager : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Buttons")]
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button exitButton;

        [Header("Settings Options")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        private void Start()
        {
            // Set up button listeners
            if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
            if (settingsButton != null) settingsButton.onClick.AddListener(OnSettingsClicked);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(OnCloseSettingsClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            // Set up Settings defaults
            InitializeSettings();

            // Ensure only the main panel is open initially
            if (mainPanel != null) mainPanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        private void OnStartGameClicked()
        {
            Debug.Log("Transitioning to LobbyScene...");
            SceneManager.LoadScene("LobbyScene");
        }

        private void OnSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }
        }

        private void OnCloseSettingsClicked()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        private void OnExitClicked()
        {
            Debug.Log("Quitting application...");
            Application.Quit();
        }

        private void InitializeSettings()
        {
            // Sound Volume
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            // Window Resolution
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                
                var options = new System.Collections.Generic.List<string>
                {
                    "1920 x 1080",
                    "1600 x 900",
                    "1280 x 720"
                };
                resolutionDropdown.AddOptions(options);

                // Default selection to 1920x1080
                resolutionDropdown.value = 0;
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }
        }

        private void OnVolumeChanged(float val)
        {
            AudioListener.volume = val;
            Debug.Log($"Sound volume adjusted to: {val * 100:F0}%");
        }

        private void OnResolutionChanged(int index)
        {
            switch (index)
            {
                case 0:
                    Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                    break;
                case 1:
                    Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
                    break;
                case 2:
                    Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                    break;
            }
            Debug.Log($"Screen resolution changed to option {index}");
        }
    }
}
