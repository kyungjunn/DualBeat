using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

namespace DualBeat.Gameplay
{
    public class GameSyncManager : MonoBehaviourPunCallbacks
    {
        public static GameSyncManager Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private TMP_Text myScoreText;
        [SerializeField] private TMP_Text opponentScoreText;
        [SerializeField] private Slider scoreComparisonSlider; // Visual bar showing who's ahead
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private GameObject resultPanel; // Result UI overlay

        [Header("Song Reference")]
        [SerializeField] private SongData[] songDatabase;

        private SongData activeSong;
        private double songStartTime = -1;
        private bool gameStarted = false;

        private int localScore = 0;
        private int opponentScore = 0;
        private bool localFinished = false;
        private Coroutine countdownCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (resultPanel != null) resultPanel.SetActive(false);
            if (countdownText != null) countdownText.gameObject.SetActive(true);

            // Fetch the selected song from Room Custom Properties
            int selectedSongIndex = 0;
            if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SelectedSong", out object indexVal))
            {
                selectedSongIndex = (int)indexVal;
            }

            if (songDatabase != null && selectedSongIndex < songDatabase.Length)
            {
                activeSong = songDatabase[selectedSongIndex];
            }
            else
            {
                Debug.LogWarning("No song selected or songDatabase is empty. Using a blank fallback.");
            }

            // Sync start time
            if (PhotonNetwork.IsMasterClient)
            {
                // Set start time 3 seconds into the future to allow loading/preparation
                double startTime = PhotonNetwork.Time + 3.0;
                songStartTime = startTime;
                
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props.Add("SongStartTime", startTime);
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                StartCountdown();
            }
            else
            {
                // Check if already set
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("SongStartTime", out object timeVal))
                {
                    songStartTime = (double)timeVal;
                    StartCountdown();
                }
            }

            // Reset player properties for gameplay
            ExitGames.Client.Photon.Hashtable playerProps = new ExitGames.Client.Photon.Hashtable();
            playerProps.Add("Score", 0);
            playerProps.Add("IsFinished", false);
            playerProps.Add("Revenge", false); // Reset revenge flag
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);
        }

        private void Update()
        {
            if (!gameStarted && songStartTime > 0)
            {
                double timeRemaining = songStartTime - PhotonNetwork.Time;
                if (timeRemaining <= 0)
                {
                    StartGameplay();
                }
                else
                {
                    if (countdownText != null)
                    {
                        countdownText.text = $"{Mathf.CeilToInt((float)timeRemaining)}";
                    }
                }
            }

            UpdateScoreUI();
        }

        private void StartGameplay()
        {
            gameStarted = true;
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            Debug.Log($"Gameplay started at network time: {PhotonNetwork.Time}. Start offset: {songStartTime}");

            // Notify local RhythmGameplay system
            if (RhythmGameplay.Instance != null && activeSong != null)
            {
                RhythmGameplay.Instance.StartSong(activeSong, songStartTime);
            }
        }

        #region Score Sync API

        /// <summary>
        /// Called by RhythmGameplay to report score increments.
        /// </summary>
        public void UpdateLocalScore(int score)
        {
            localScore = score;
            
            // Sync to network via Player Custom Properties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("Score", score);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        /// <summary>
        /// Called by RhythmGameplay when the local track completes.
        /// </summary>
        public void SetLocalFinished()
        {
            localFinished = true;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("IsFinished", true);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            CheckGameFinished();
        }

        private void UpdateScoreUI()
        {
            // My Score
            if (myScoreText != null)
            {
                myScoreText.text = $"{PhotonNetwork.NickName}\n<b>{localScore:N0}</b>";
            }

            // Find Opponent Player to read score
            Player opponent = GetOpponentPlayer();
            if (opponent != null)
            {
                if (opponent.CustomProperties.TryGetValue("Score", out object val))
                {
                    opponentScore = (int)val;
                }
                if (opponentScoreText != null)
                {
                    opponentScoreText.text = $"{opponent.NickName}\n<b>{opponentScore:N0}</b>";
                }
            }
            else
            {
                if (opponentScoreText != null)
                {
                    opponentScoreText.text = "Waiting for Opponent...\n<b>0</b>";
                }
            }

            // Sync Comparison Slider (P1 vs P2)
            if (scoreComparisonSlider != null)
            {
                float total = localScore + opponentScore;
                if (total > 0)
                {
                    scoreComparisonSlider.value = localScore / total;
                }
                else
                {
                    scoreComparisonSlider.value = 0.5f; // Even distribution
                }
            }
        }

        private void CheckGameFinished()
        {
            Player opponent = GetOpponentPlayer();
            bool opponentFinished = false;

            if (opponent != null)
            {
                if (opponent.CustomProperties.TryGetValue("IsFinished", out object val))
                {
                    opponentFinished = (bool)val;
                }
            }
            else
            {
                // Solo play or opponent disconnected
                opponentFinished = true;
            }

            if (localFinished && opponentFinished)
            {
                TriggerResultPanel();
            }
        }

        private void TriggerResultPanel()
        {
            Debug.Log("Both players finished. Activating result screen.");
            if (resultPanel != null)
            {
                resultPanel.SetActive(true);
                
                // Initialize Result Panel component
                UI.ResultManager resultMgr = resultPanel.GetComponent<UI.ResultManager>();
                if (resultMgr != null)
                {
                    Player opponent = GetOpponentPlayer();
                    string opponentName = opponent != null ? opponent.NickName : "Opponent";
                    resultMgr.SetupResult(localScore, opponentScore, opponentName);
                }
            }
        }

        private Player GetOpponentPlayer()
        {
            if (PhotonNetwork.CurrentRoom == null) return null;
            
            foreach (var kvp in PhotonNetwork.CurrentRoom.Players)
            {
                if (kvp.Value != PhotonNetwork.LocalPlayer)
                {
                    return kvp.Value;
                }
            }
            return null;
        }

        #endregion

        #region PUN Overrides

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey("SongStartTime"))
            {
                songStartTime = (double)propertiesThatChanged["SongStartTime"];
                StartCountdown();
            }
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("Score"))
            {
                UpdateScoreUI();
            }
            
            if (changedProps.ContainsKey("IsFinished"))
            {
                CheckGameFinished();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.LogWarning($"Opponent left the game: {otherPlayer.NickName}");
            // Treat opponent as finished with score 0 to prevent softlock
            CheckGameFinished();
        }

        #endregion

        private void StartCountdown()
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            while (!gameStarted)
            {
                double remaining = songStartTime - PhotonNetwork.Time;
                if (remaining <= 0)
                {
                    StartGameplay();
                    yield break;
                }
                yield return null;
            }
        }

        public void RequestExitToLobby()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("RoomLobbyScene");
            }
            else
            {
                photonView.RPC("RequestExitToLobbyRPC", RpcTarget.MasterClient);
            }
        }

        [PunRPC]
        private void RequestExitToLobbyRPC()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("RoomLobbyScene");
            }
        }
    }
}
