using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

namespace DualBeat.UI
{
    public class ResultManager : MonoBehaviourPunCallbacks
    {
        [Header("Score Displays")]
        [SerializeField] private TMP_Text myFinalScoreText;
        [SerializeField] private TMP_Text opponentFinalScoreText;
        [SerializeField] private TMP_Text victoryHeaderText;

        [Header("Rematch Feedback")]
        [SerializeField] private TMP_Text myRevengeStatusText;
        [SerializeField] private TMP_Text opponentRevengeStatusText;

        [Header("Buttons")]
        [SerializeField] private Button revengeButton;
        [SerializeField] private Button exitButton;

        private bool localRevengeRequested = false;

        private void Start()
        {
            if (revengeButton != null) revengeButton.onClick.AddListener(OnRevengeClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);

            UpdateRevengeUI();
        }

        public void SetupResult(int myScore, int opponentScore, string opponentName)
        {
            if (myFinalScoreText != null)
            {
                myFinalScoreText.text = $"{PhotonNetwork.NickName}\n<b>{myScore:N0}</b>";
            }

            if (opponentFinalScoreText != null)
            {
                opponentFinalScoreText.text = $"{opponentName}\n<b>{opponentScore:N0}</b>";
            }

            if (victoryHeaderText != null)
            {
                if (myScore > opponentScore)
                {
                    victoryHeaderText.text = "<color=cyan>YOU WIN</color>";
                }
                else if (myScore < opponentScore)
                {
                    victoryHeaderText.text = "<color=red>YOU LOSE</color>";
                }
                else
                {
                    victoryHeaderText.text = "<color=yellow>DRAW</color>";
                }
            }
        }

        private void OnRevengeClicked()
        {
            localRevengeRequested = !localRevengeRequested;

            // Sync local revenge request status to other player via custom player properties
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("Revenge", localRevengeRequested);
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);

            UpdateRevengeUI();
            CheckRematchHandshake();
        }

        private void OnExitClicked()
        {
            if (Gameplay.GameSyncManager.Instance != null)
            {
                Gameplay.GameSyncManager.Instance.RequestExitToLobby();
            }
            else
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    PhotonNetwork.LoadLevel("RoomLobbyScene");
                }
            }
        }

        private void CheckRematchHandshake()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            Player opponent = GetOpponentPlayer();
            bool opponentRevenge = false;

            if (opponent != null)
            {
                if (opponent.CustomProperties.TryGetValue("Revenge", out object val))
                {
                    opponentRevenge = (bool)val;
                }
            }

            if (localRevengeRequested && opponentRevenge)
            {
                Debug.Log("Rematch handshake successful! Restarting gameplay scene.");
                
                // Master loads the InGameScene again
                PhotonNetwork.LoadLevel("InGameScene");
            }
        }

        private void UpdateRevengeUI()
        {
            if (myRevengeStatusText != null)
            {
                myRevengeStatusText.text = localRevengeRequested ? "<color=green>READY FOR REMATCH</color>" : "Wants Rematch?";
            }

            Player opponent = GetOpponentPlayer();
            bool opponentRevenge = false;

            if (opponent != null)
            {
                if (opponent.CustomProperties.TryGetValue("Revenge", out object val))
                {
                    opponentRevenge = (bool)val;
                }
            }

            if (opponentRevengeStatusText != null)
            {
                opponentRevengeStatusText.text = opponent == null ? "" : (opponentRevenge ? "<color=green>READY FOR REMATCH</color>" : "Wants Rematch?");
            }

            if (revengeButton != null)
            {
                TMP_Text btnText = revengeButton.GetComponentInChildren<TMP_Text>();
                if (btnText != null)
                {
                    btnText.text = localRevengeRequested ? "Cancel Revenge" : "Request Revenge";
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

        #region PUN Overrides

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (changedProps.ContainsKey("Revenge"))
            {
                UpdateRevengeUI();
                CheckRematchHandshake();
            }
        }

        #endregion
    }
}
