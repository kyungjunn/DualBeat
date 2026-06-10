using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

namespace DualBeat.UI
{
    public class RoomLobbyManager : MonoBehaviourPunCallbacks
    {
        [Header("Room Info")]
        [SerializeField] private TMP_Text roomNameText;

        [Header("Player Info Panels")]
        [SerializeField] private TMP_Text masterNicknameText;
        [SerializeField] private TMP_Text guestNicknameText;
        [SerializeField] private TMP_Text guestReadyStatusText;

        [Header("Song Selection")]
        [SerializeField] private Gameplay.SongData[] availableSongs;
        [SerializeField] private Transform songListContainer;
        [SerializeField] private GameObject songListItemPrefab;
        [SerializeField] private TMP_Text selectedSongDetailsText;

        [Header("Live Chat Panel")]
        [SerializeField] private TMP_InputField chatInputField;
        [SerializeField] private TMP_Text chatLogText;
        [SerializeField] private ScrollRect chatScrollRect;

        [Header("Control Buttons")]
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private Button startButton;

        private int selectedSongIndex = 0;
        private bool localGuestReady = false;

        private void Start()
        {
            if (PhotonNetwork.CurrentRoom == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LobbyScene");
                return;
            }

            // Set up room name
            if (roomNameText != null)
            {
                roomNameText.text = $"Room: {PhotonNetwork.CurrentRoom.Name}";
            }

            // Bind buttons
            if (leaveButton != null) leaveButton.onClick.AddListener(OnLeaveClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);

            // Bind chat input submit
            if (chatInputField != null)
            {
                chatInputField.onSubmit.AddListener(delegate { SendChatMessage(); });
            }

            // Initial UI updates
            UpdatePlayerListUI();
            SyncSongSelectionUI();
        }

        private void Update()
        {
            // Lock song selection interface for Guest
            // (Lobby layout can display it but disable raycast / interactions)
        }

        #region Player & State Updates

        private void UpdatePlayerListUI()
        {
            // Identify Master Client and Guest
            Player masterPlayer = PhotonNetwork.MasterClient;
            Player guestPlayer = null;

            foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
            {
                if (kvp.Value != masterPlayer)
                {
                    guestPlayer = kvp.Value;
                    break;
                }
            }

            // Master Nickname Column
            if (masterNicknameText != null)
            {
                masterNicknameText.text = masterPlayer != null ? masterPlayer.NickName : "Waiting for Host...";
            }

            // Guest Nickname Column
            if (guestNicknameText != null)
            {
                guestNicknameText.text = guestPlayer != null ? guestPlayer.NickName : "Waiting for Player...";
            }

            // Guest Ready state evaluation
            bool guestIsReady = false;
            if (guestPlayer != null)
            {
                if (guestPlayer.CustomProperties.TryGetValue("IsReady", out object val))
                {
                    guestIsReady = (bool)val;
                }
            }

            if (guestReadyStatusText != null)
            {
                guestReadyStatusText.text = guestPlayer == null ? "" : (guestIsReady ? "<color=green>READY</color>" : "<color=red>NOT READY</color>");
            }

            // Button Visibility & Interactivity setup
            bool isMaster = PhotonNetwork.IsMasterClient;

            if (startButton != null)
            {
                startButton.gameObject.SetActive(isMaster);
                // Active ONLY if guest is in room AND guest is Ready
                startButton.interactable = isMaster && guestPlayer != null && guestIsReady;
            }

            if (readyButton != null)
            {
                readyButton.gameObject.SetActive(!isMaster);
                
                TMP_Text readyBtnText = readyButton.GetComponentInChildren<TMP_Text>();
                if (readyBtnText != null)
                {
                    readyBtnText.text = localGuestReady ? "Unready" : "Ready";
                }
            }
        }

        #endregion

        #region Song Selection Synchronizer

        private void PopulateSongList()
        {
            // Clear song list container
            if (songListContainer != null)
            {
                foreach (Transform child in songListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (availableSongs == null || availableSongs.Length == 0) return;

            for (int i = 0; i < availableSongs.Length; i++)
            {
                int index = i;
                GameObject itemObj = Instantiate(songListItemPrefab, songListContainer);
                
                // Get TMP_Text child inside itemObj to set Title
                TMP_Text itemText = itemObj.GetComponentInChildren<TMP_Text>();
                if (itemText != null)
                {
                    if (index == selectedSongIndex)
                    {
                        itemText.text = $"> <color=#FFCC00><b>[Selected] {availableSongs[i].songTitle} - {availableSongs[i].artistName}</b></color>";
                    }
                    else
                    {
                        itemText.text = $"{availableSongs[i].songTitle} - {availableSongs[i].artistName}";
                    }
                }

                Button itemBtn = itemObj.GetComponent<Button>();
                if (itemBtn != null)
                {
                    // Disable button clicks for Guest, only Master Client is interactive
                    itemBtn.interactable = PhotonNetwork.IsMasterClient;
                    itemBtn.onClick.AddListener(() => OnSongListItemSelected(index));
                }
            }
        }

        private void OnSongListItemSelected(int index)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            selectedSongIndex = index;
            
            // Sync selected song index to Room Custom Properties
            ExitGames.Client.Photon.Hashtable prop = new ExitGames.Client.Photon.Hashtable();
            prop.Add("SelectedSong", index);
            PhotonNetwork.CurrentRoom.SetCustomProperties(prop);

            SyncSongSelectionUI();
        }

        private void SyncSongSelectionUI()
        {
            // Read Room Properties
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedSong", out object val))
            {
                selectedSongIndex = (int)val;
            }

            // Refresh the song list UI to show the selected highlight
            PopulateSongList();

            if (availableSongs != null && selectedSongIndex < availableSongs.Length)
            {
                Gameplay.SongData song = availableSongs[selectedSongIndex];
                if (selectedSongDetailsText != null)
                {
                    selectedSongDetailsText.text = $"Selected Track:\n<b>{song.songTitle}</b>\nArtist: {song.artistName}\nBPM: {song.bpm}";
                }
            }
        }

        #endregion

        #region Live Text Chat system

        private void SendChatMessage()
        {
            if (chatInputField == null || string.IsNullOrWhiteSpace(chatInputField.text)) return;

            string senderName = PhotonNetwork.NickName;
            string messageText = chatInputField.text;

            // Broadcast message via RPC
            photonView.RPC("ReceiveMessage", RpcTarget.All, senderName, messageText);

            chatInputField.text = "";
            chatInputField.ActivateInputField(); // Keep input focused
        }

        [PunRPC]
        private void ReceiveMessage(string sender, string message)
        {
            if (chatLogText == null) return;

            // Append formatting
            chatLogText.text += $"\n<b>{sender}:</b> {message}";

            // Auto-scroll chat to the bottom
            Canvas.ForceUpdateCanvases();
            if (chatScrollRect != null)
            {
                chatScrollRect.verticalNormalizedPosition = 0f;
            }
        }

        #endregion

        #region UI Click Bindings

        private void OnLeaveClicked()
        {
            // Reset player ready properties before leaving
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("IsReady", false);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            Network.NetworkManager.Instance.LeaveCurrentRoom();
        }

        private void OnReadyClicked()
        {
            localGuestReady = !localGuestReady;

            // Sync Guest's Ready status using Custom Player Properties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("IsReady", localGuestReady);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            UpdatePlayerListUI();
        }

        private void OnStartClicked()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Debug.Log("Master starting the game session...");
            
            // Set room as closed to prevent new players joining during game
            PhotonNetwork.CurrentRoom.IsOpen = false;

            // Load the rhythm gameplay scene
            PhotonNetwork.LoadLevel("InGameScene");
        }

        #endregion

        #region PUN Overrides

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Player entered waiting room: {newPlayer.NickName}");
            UpdatePlayerListUI();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Player left waiting room: {otherPlayer.NickName}");
            UpdatePlayerListUI();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Update UI if ready states change
            if (changedProps.ContainsKey("IsReady"))
            {
                UpdatePlayerListUI();
            }
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            // Update UI if selected song changes
            if (propertiesThatChanged.ContainsKey("SelectedSong"))
            {
                SyncSongSelectionUI();
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            Debug.LogWarning($"Master client switched to: {newMasterClient.NickName}");
            // Return to LobbyScene if room ownership swaps to avoid state issues
            OnLeaveClicked();
        }

        #endregion
    }
}
