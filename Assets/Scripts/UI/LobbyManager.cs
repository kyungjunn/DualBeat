using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using DualBeat.Network;

namespace DualBeat.UI
{
    public class LobbyManager : MonoBehaviourPunCallbacks
    {
        [Header("Profile / Nickname Setup")]
        [SerializeField] private TMP_InputField nicknameInputField;

        [Header("Room Creation")]
        [SerializeField] private TMP_InputField roomNameInputField;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button randomJoinButton;

        [Header("Room List View")]
        [SerializeField] private Transform roomListContainer;
        [SerializeField] private GameObject roomListItemPrefab;
        [SerializeField] private TMP_Text emptyListText;

        [Header("Navigation")]
        [SerializeField] private Button backToMenuButton;

        // Cache of active rooms listed in the lobby
        private Dictionary<string, RoomInfo> activeRooms = new Dictionary<string, RoomInfo>();

        private void Start()
        {
            // Setup Input Fields with existing nickname if already configured
            if (nicknameInputField != null)
            {
                nicknameInputField.text = string.IsNullOrWhiteSpace(PhotonNetwork.NickName) 
                    ? "" 
                    : PhotonNetwork.NickName;
            }

            // Setup buttons
            if (createRoomButton != null) createRoomButton.onClick.AddListener(OnCreateRoomClicked);
            if (randomJoinButton != null) randomJoinButton.onClick.AddListener(OnRandomJoinClicked);
            if (backToMenuButton != null) backToMenuButton.onClick.AddListener(OnBackToMenuClicked);

            // Handle Photon connection and lobby state to ensure we get the room list
            if (PhotonNetwork.IsConnectedAndReady)
            {
                if (PhotonNetwork.InLobby)
                {
                    Debug.Log("Already in lobby. Rejoining lobby to force room list update.");
                    PhotonNetwork.LeaveLobby();
                }
                else
                {
                    Debug.Log("Connected to Master. Joining lobby...");
                    PhotonNetwork.JoinLobby();
                }
            }
            else
            {
                Debug.Log("Not connected to Photon. Initiating connection...");
                NetworkManager.Instance.ConnectToPhoton();
            }

            // Initial refresh of room list
            UpdateRoomListUI();
        }

        private void Update()
        {
            // Optional: check connection status to enable/disable buttons
            bool ready = PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;
            if (createRoomButton != null) createRoomButton.interactable = ready;
            if (randomJoinButton != null) randomJoinButton.interactable = ready;
        }

        #region UI Button Handlers

        private void OnCreateRoomClicked()
        {
            string nickname = GetValidatedNickname();
            NetworkManager.Instance.SetNickname(nickname);

            string roomName = roomNameInputField != null ? roomNameInputField.text : "";
            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = $"Room_{Random.Range(1000, 9999)}";
            }

            NetworkManager.Instance.CreateRoom(roomName);
        }

        private void OnRandomJoinClicked()
        {
            string nickname = GetValidatedNickname();
            NetworkManager.Instance.SetNickname(nickname);

            NetworkManager.Instance.JoinRandomRoom();
        }

        private void OnBackToMenuClicked()
        {
            Debug.Log("Returning to IntroScene...");
            PhotonNetwork.Disconnect(); // Disconnect to clean up state
            UnityEngine.SceneManagement.SceneManager.LoadScene("IntroScene");
        }

        private string GetValidatedNickname()
        {
            string nickname = nicknameInputField != null ? nicknameInputField.text : "";
            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname = $"Player_{Random.Range(1000, 9999)}";
            }
            return nickname;
        }

        #endregion

        #region PUN Callbacks

        public override void OnJoinedLobby()
        {
            Debug.Log("LobbyManager: Joined lobby. Clearing active rooms cache.");
            activeRooms.Clear();
            UpdateRoomListUI();
        }

        public override void OnLeftLobby()
        {
            Debug.Log("Left lobby. Rejoining to fetch room list...");
            PhotonNetwork.JoinLobby();
        }

        public override void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            Debug.Log($"Room list update received. Count: {roomList.Count}");
            
            foreach (RoomInfo room in roomList)
            {
                if (room.RemovedFromList || !room.IsOpen || !room.IsVisible)
                {
                    activeRooms.Remove(room.Name);
                }
                else
                {
                    activeRooms[room.Name] = room;
                }
            }

            UpdateRoomListUI();
        }

        #endregion

        #region Room List UI Logic

        private void UpdateRoomListUI()
        {
            // Clear existing list items
            if (roomListContainer != null)
            {
                foreach (Transform child in roomListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            if (activeRooms.Count == 0)
            {
                if (emptyListText != null) emptyListText.gameObject.SetActive(true);
                return;
            }

            if (emptyListText != null) emptyListText.gameObject.SetActive(false);

            if (roomListItemPrefab == null || roomListContainer == null)
            {
                Debug.LogWarning("Room list prefab or container is not assigned.");
                return;
            }

            foreach (var kvp in activeRooms)
            {
                RoomInfo room = kvp.Value;

                GameObject itemObj = Instantiate(roomListItemPrefab, roomListContainer);
                RoomListItem item = itemObj.GetComponent<RoomListItem>();

                if (item != null)
                {
                    // Read MasterClient Name from Custom Properties
                    string masterName = "Unknown";
                    if (room.CustomProperties.TryGetValue("MasterName", out object val))
                    {
                        masterName = val.ToString();
                    }

                    item.Setup(
                        room.Name, 
                        masterName, 
                        room.PlayerCount, 
                        room.MaxPlayers, 
                        OnRoomSelected
                    );
                }
            }
        }

        private void OnRoomSelected(string roomName)
        {
            string nickname = GetValidatedNickname();
            NetworkManager.Instance.SetNickname(nickname);

            NetworkManager.Instance.JoinRoom(roomName);
        }

        #endregion
    }
}
