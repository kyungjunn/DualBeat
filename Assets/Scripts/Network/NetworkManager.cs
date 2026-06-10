using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

namespace DualBeat.Network
{
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Scene Names")]
        public string introSceneName = "IntroScene";
        public string lobbySceneName = "LobbyScene";
        public string roomLobbySceneName = "RoomLobbyScene";
        public string inGameSceneName = "InGameScene";

        [Header("Connection Settings")]
        [SerializeField] private byte maxPlayersPerRoom = 2;
        [SerializeField] private string gameVersion = "1.0";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Synchronize loaded levels between Master Client and Guests
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            if (!PhotonNetwork.IsConnected)
            {
                ConnectToPhoton();
            }
        }

        public void ConnectToPhoton()
        {
            Debug.Log("Connecting to Photon...");
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }

        #region PUN Callbacks

        public override void OnConnectedToMaster()
        {
            Debug.Log($"Connected to Photon Master Server. Region: {PhotonNetwork.CloudRegion}");
            // Always join the default lobby upon successful connection
            PhotonNetwork.JoinLobby();
        }

        public override void OnJoinedLobby()
        {
            Debug.Log("Successfully joined the default Photon Lobby.");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarning($"Disconnected from Photon. Cause: {cause}");
            // Handle reconnection or loading intro scene
            if (SceneManager.GetActiveScene().name != introSceneName)
            {
                SceneManager.LoadScene(introSceneName);
            }
        }

        public override void OnJoinedRoom()
        {
            Debug.Log($"Joined room: {PhotonNetwork.CurrentRoom.Name}. Player count: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}");
            
            // Only transition to RoomLobbyScene if we are the local client entering
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(roomLobbySceneName);
            }
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Room creation failed: {message} (Code: {returnCode})");
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            Debug.LogError($"Failed to join room: {message} (Code: {returnCode})");
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.LogWarning($"Random join failed: {message}. Creating a random room instead.");
            CreateRoom(null); // Fallback to creating a new random room
        }

        public override void OnLeftRoom()
        {
            Debug.Log("Left the Photon Room. Returning to LobbyScene.");
            SceneManager.LoadScene(lobbySceneName);
        }

        #endregion

        #region Room API

        /// <summary>
        /// Updates the local player's nickname on Photon.
        /// </summary>
        public void SetNickname(string nickname)
        {
            if (string.IsNullOrWhiteSpace(nickname))
            {
                nickname = $"Player_{Random.Range(1000, 9999)}";
            }
            PhotonNetwork.NickName = nickname;
            Debug.Log($"Photon Nickname set to: {PhotonNetwork.NickName}");
        }

        /// <summary>
        /// Creates a room with a specified name or a random generated name.
        /// </summary>
        public void CreateRoom(string roomName)
        {
            if (!PhotonNetwork.IsConnectedAndReady)
            {
                Debug.LogError("Cannot create room: Not connected or not ready on Photon server.");
                return;
            }

            if (string.IsNullOrWhiteSpace(roomName))
            {
                roomName = $"Room_{Random.Range(1000, 9999)}";
            }

            ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable();
            customProps.Add("MasterName", string.IsNullOrWhiteSpace(PhotonNetwork.NickName) ? "Unknown" : PhotonNetwork.NickName);
            string[] customPropsForLobby = new string[] { "MasterName" };

            RoomOptions options = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsOpen = true,
                IsVisible = true,
                CustomRoomProperties = customProps,
                CustomRoomPropertiesForLobby = customPropsForLobby
            };

            Debug.Log($"Creating room: {roomName}");
            PhotonNetwork.CreateRoom(roomName, options);
        }

        /// <summary>
        /// Joins an existing room by its name.
        /// </summary>
        public void JoinRoom(string roomName)
        {
            if (string.IsNullOrWhiteSpace(roomName))
            {
                Debug.LogError("Cannot join room: Room name is empty.");
                return;
            }

            Debug.Log($"Joining room: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }

        /// <summary>
        /// Joins any open and available room.
        /// </summary>
        public void JoinRandomRoom()
        {
            Debug.Log("Joining random room...");
            PhotonNetwork.JoinRandomRoom();
        }

        /// <summary>
        /// Leaves the current room session.
        /// </summary>
        public void LeaveCurrentRoom()
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("Leaving active room session...");
                PhotonNetwork.LeaveRoom();
            }
            else
            {
                SceneManager.LoadScene(lobbySceneName);
            }
        }

        #endregion
    }
}
