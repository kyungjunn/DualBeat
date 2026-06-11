using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace DualBeat.Gameplay
{
    public class RhythmGameplay : MonoBehaviour
    {
        public static RhythmGameplay Instance { get; private set; }

        [Header("Note Setup")]
        [SerializeField] private GameObject myNotePrefab;
        [SerializeField] private GameObject opponentNotePrefab;

        [Header("Visual Lanes (X Positions)")]
        [Tooltip("Local X positions for My 6 lanes (0 to 5)")]
        [SerializeField] private float[] myLaneXPositions = new float[6] { -6f, -4.8f, -3.6f, -2.4f, -1.2f, 0f };
        
        [Tooltip("Local X positions for Opponent's 6 lanes (0 to 5)")]
        [SerializeField] private float[] opponentLaneXPositions = new float[6] { 1.2f, 2.4f, 3.6f, 4.8f, 6f, 7.2f };

        [SerializeField] private float spawnYPosition = 8f;
        [SerializeField] private float judgmentYPosition = -4f;
        [SerializeField] private float scrollSpeed = 5f; // UI Scroll speed (pixels per second)

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("UI Feedback")]
        [SerializeField] private TMPro.TMP_Text comboText;
        [SerializeField] private TMPro.TMP_Text ratingText;

        // Key bindings
        private readonly KeyCode[] inputKeys = new KeyCode[6]
        {
            KeyCode.Q, KeyCode.W, KeyCode.E,
            KeyCode.I, KeyCode.O, KeyCode.P
        };

        // Decoupled sub-systems
        private RhythmClock rhythmClock;
        private NoteSpawner noteSpawner;
        private JudgeSystem judgeSystem;
        private ScoreManager scoreManager;
        private ChartData chartData;

        // Runtime states
        private List<NoteView> activeNotes = new List<NoteView>();
        private int myNextNoteIndex = 0;
        private bool songPlaying = false;

        // Key feedback UI
        private UnityEngine.UI.Image[] keyBackgrounds = new UnityEngine.UI.Image[6];
        private TMPro.TMP_Text[] keyLabels = new TMPro.TMP_Text[6];
        private RectTransform playfieldParent;

        // UI note layout settings
        private float actualJudgmentY = 100f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Get or Add sub-systems automatically
            rhythmClock = GetComponent<RhythmClock>();
            if (rhythmClock == null) rhythmClock = gameObject.AddComponent<RhythmClock>();

            noteSpawner = GetComponent<NoteSpawner>();
            if (noteSpawner == null) noteSpawner = gameObject.AddComponent<NoteSpawner>();

            judgeSystem = GetComponent<JudgeSystem>();
            if (judgeSystem == null) judgeSystem = gameObject.AddComponent<JudgeSystem>();

            scoreManager = GetComponent<ScoreManager>();
            if (scoreManager == null) scoreManager = gameObject.AddComponent<ScoreManager>();
        }

        private void OnValidate()
        {
            if (scrollSpeed == 5f)
            {
                scrollSpeed = 400f;
                Debug.Log("[RhythmGameplay] Automatically migrated scrollSpeed from legacy 5f to 400f.");
            }
        }

        private void Start()
        {
            if (comboText != null) comboText.text = "";
            if (ratingText != null) ratingText.text = "";

            // 1. Hide Opponent Playfield and Center Local Playfield
            GameObject p2Field = GameObject.Find("Player2Field");
            if (p2Field != null)
            {
                p2Field.SetActive(false);
            }

            GameObject p1Field = GameObject.Find("Player1Field");
            if (p1Field != null)
            {
                playfieldParent = p1Field.GetComponent<RectTransform>();
                if (playfieldParent != null)
                {
                    // Center the field in the middle 50% of the screen
                    playfieldParent.anchorMin = new Vector2(0.25f, 0f);
                    playfieldParent.anchorMax = new Vector2(0.75f, 1f);
                    playfieldParent.anchoredPosition = Vector2.zero;
                    playfieldParent.sizeDelta = new Vector2(-40f, -100f);
                }

                // 2. Create keypress indicators (Q, W, E, I, O, P) at the bottom of the centered field
                CreateKeyIndicators(p1Field);

                // 3. Configure JudgmentLine to be red and read its Y position
                Transform judgmentLineTrans = p1Field.transform.Find("JudgmentLine");
                if (judgmentLineTrans != null)
                {
                    UnityEngine.UI.Image img = judgmentLineTrans.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.color = Color.red;
                    }
                    RectTransform judgmentRect = judgmentLineTrans.GetComponent<RectTransform>();
                    if (judgmentRect != null)
                    {
                        actualJudgmentY = judgmentRect.anchoredPosition.y;
                    }
                }
            }

            // Initialize sub-systems
            noteSpawner.Initialize(playfieldParent, actualJudgmentY, myNotePrefab, scrollSpeed, 2.0f);
            scoreManager.Initialize(comboText, ratingText);
            judgeSystem.Initialize(scoreManager);
        }

        public void StartSong(SongData song, double networkStartTime)
        {
            chartData = new ChartData(song);
            myNextNoteIndex = 0;
            activeNotes.Clear();
            scoreManager.ResetScore();

            if (audioSource != null && song.audioClip != null)
            {
                audioSource.clip = song.audioClip;
                double delay = networkStartTime - PhotonNetwork.Time;
                
                if (delay > 0)
                {
                    double dspStartTime = AudioSettings.dspTime + delay;
                    audioSource.PlayScheduled(dspStartTime);
                    rhythmClock.StartClock(dspStartTime);
                }
                else
                {
                    audioSource.Play();
                    audioSource.time = (float)(-delay);
                    
                    double dspStartTime = AudioSettings.dspTime + delay;
                    rhythmClock.StartClock(dspStartTime);
                }
            }

            songPlaying = true;
            Debug.Log($"Initialized song '{song.songTitle}' locally.");
        }

        private void Update()
        {
            if (!songPlaying || chartData == null) return;

            double currentSongTime = rhythmClock.SongTime;

            // 1. Spawning
            noteSpawner.SpawnNotes(currentSongTime, chartData, ref myNextNoteIndex, activeNotes);

            // 2. Note Position updates
            for (int i = 0; i < activeNotes.Count; i++)
            {
                if (activeNotes[i] != null)
                {
                    activeNotes[i].UpdatePosition(currentSongTime);
                }
            }

            // 3. Inputs
            HandleKeyboardInput(currentSongTime);

            // 4. Auto-miss processing
            judgeSystem.ProcessMisses(currentSongTime, activeNotes);

            // 5. Completion check
            CheckSongCompletion(currentSongTime);
        }

        private void HandleKeyboardInput(double currentSongTime)
        {
            for (int lane = 0; lane < 6; lane++)
            {
                if (Input.GetKeyDown(inputKeys[lane]))
                {
                    judgeSystem.EvaluateKeyPress(lane, currentSongTime, activeNotes);
                    AnimateKeyPress(lane, true);
                }

                if (Input.GetKeyUp(inputKeys[lane]))
                {
                    AnimateKeyPress(lane, false);
                }
            }
        }

        private void CheckSongCompletion(double currentSongTime)
        {
            bool allNotesSpawned = myNextNoteIndex >= chartData.notes.Count;
            bool allNotesProcessed = allNotesSpawned && activeNotes.Count == 0;
            bool audioFinished = audioSource != null && !audioSource.isPlaying && currentSongTime > 3.0;

            if ((allNotesProcessed || audioFinished) && songPlaying)
            {
                songPlaying = false;
                rhythmClock.StopClock();
                Debug.Log("Local gameplay track completed.");
                GameSyncManager.Instance.SetLocalFinished();
            }
        }

        private void CreateKeyIndicators(GameObject parentField)
        {
            if (parentField == null) return;

            string[] keyNames = new string[6] { "Q", "W", "E", "I", "O", "P" };

            for (int i = 0; i < 6; i++)
            {
                // Create a container for each keycap
                GameObject keycapGo = new GameObject($"Keycap_{keyNames[i]}", typeof(RectTransform));
                keycapGo.transform.SetParent(parentField.transform, false);

                RectTransform rectTrans = keycapGo.GetComponent<RectTransform>();
                
                // Position at the bottom of the lane (each lane occupies 1/6th of width)
                float leftAnchor = i / 6.0f;
                float rightAnchor = (i + 1) / 6.0f;

                rectTrans.anchorMin = new Vector2(leftAnchor, 0f);
                rectTrans.anchorMax = new Vector2(rightAnchor, 0f);
                rectTrans.pivot = new Vector2(0.5f, 0f);
                rectTrans.anchoredPosition = new Vector2(0f, 15f);
                rectTrans.sizeDelta = new Vector2(-10f, 40f); // 5px padding on left/right

                // Add an Image background (dark semi-transparent keycap)
                UnityEngine.UI.Image bgImage = keycapGo.AddComponent<UnityEngine.UI.Image>();
                bgImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                keyBackgrounds[i] = bgImage;

                // Add TextMeshProUGUI child
                GameObject textGo = new GameObject("Text", typeof(RectTransform));
                textGo.transform.SetParent(keycapGo.transform, false);

                RectTransform textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.sizeDelta = Vector2.zero;

                TMPro.TMP_Text textComp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
                textComp.text = keyNames[i];
                textComp.alignment = TMPro.TextAlignmentOptions.Center;
                textComp.fontSize = 20f;
                textComp.color = Color.white;
                textComp.fontStyle = TMPro.FontStyles.Bold;
                
                keyLabels[i] = textComp;
            }
        }

        private void AnimateKeyPress(int lane, bool isPressed)
        {
            if (lane < 0 || lane >= 6) return;

            if (isPressed)
            {
                // Highlight color when pressed (Cyan for QWE, Orange/Yellow for IOP)
                Color pressColor = (lane < 3) ? new Color(0f, 0.8f, 1f, 1f) : new Color(1f, 0.6f, 0f, 1f);
                
                if (keyBackgrounds[lane] != null)
                {
                    keyBackgrounds[lane].color = pressColor;
                }
                if (keyLabels[lane] != null)
                {
                    keyLabels[lane].color = Color.black;
                    keyLabels[lane].transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                }
            }
            else
            {
                // Restore default color
                if (keyBackgrounds[lane] != null)
                {
                    keyBackgrounds[lane].color = new Color(0.15f, 0.15f, 0.2f, 0.8f);
                }
                if (keyLabels[lane] != null)
                {
                    keyLabels[lane].color = Color.white;
                    keyLabels[lane].transform.localScale = Vector3.one;
                }
            }
        }
    }
}
