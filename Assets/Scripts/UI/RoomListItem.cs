using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DualBeat.UI
{
    public class RoomListItem : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text roomNameText;
        [SerializeField] private TMP_Text masterNameText;
        [SerializeField] private TMP_Text playerCountText;
        [SerializeField] private Button joinButton;

        private string roomName;
        private System.Action<string> onJoinCallback;

        private void Start()
        {
            if (joinButton != null)
            {
                joinButton.onClick.AddListener(OnJoinClicked);
            }
        }

        public void Setup(string name, string masterName, int currentPlayers, int maxPlayers, System.Action<string> onJoinClick)
        {
            roomName = name;
            onJoinCallback = onJoinClick;

            if (roomNameText != null) roomNameText.text = name;
            if (masterNameText != null) masterNameText.text = $"Host: {masterName}";
            if (playerCountText != null) playerCountText.text = $"{currentPlayers}/{maxPlayers}";

            // Disable joining if the room is full
            if (joinButton != null)
            {
                joinButton.interactable = currentPlayers < maxPlayers;
            }
        }

        private void OnJoinClicked()
        {
            if (!string.IsNullOrEmpty(roomName))
            {
                onJoinCallback?.Invoke(roomName);
            }
        }
    }
}
