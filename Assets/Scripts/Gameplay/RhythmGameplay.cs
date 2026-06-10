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
        [SerializeField] private float scrollSpeed = 5f; // Units per second

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

        // Song timing state
        private SongData activeSong;
        private double songStartTime = -1;
        private bool songPlaying = false;

        // Note tracking structure
        private class ActiveNote
        {
            public float hitTime;
            public int lane;
            public GameObject visualObject;
            public bool isMyNote;
        }

        private List<ActiveNote> activeNotes = new List<ActiveNote>();
        
        // Track the index of the next note to spawn from SongData
        private int myNextNoteIndex = 0;
        private int opponentNextNoteIndex = 0;

        // Local gameplay scoring state
        private int currentScore = 0;
        private int currentCombo = 0;
        private int maxCombo = 0;

        // Key feedback UI
        private UnityEngine.UI.Image[] keyBackgrounds = new UnityEngine.UI.Image[6];
        private TMPro.TMP_Text[] keyLabels = new TMPro.TMP_Text[6];
        private RectTransform playfieldParent;

        // UI note layout settings
        private float actualJudgmentY = 100f;
        private const float uiSpawnY = 900f;
        private const float uiScrollSpeed = 400f;

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
        }

        public void StartSong(SongData song, double networkStartTime)
        {
            activeSong = song;
            songStartTime = networkStartTime;
            songPlaying = true;

            myNextNoteIndex = 0;
            opponentNextNoteIndex = 0;

            if (audioSource != null && song.audioClip != null)
            {
                audioSource.clip = song.audioClip;
                double delay = networkStartTime - PhotonNetwork.Time;
                if (delay > 0)
                {
                    audioSource.PlayScheduled(AudioSettings.dspTime + delay);
                }
                else
                {
                    audioSource.time = (float)(-delay);
                    audioSource.Play();
                }
            }

            Debug.Log($"Initialized song '{song.songTitle}' locally.");
        }

        private void Update()
        {
            if (!songPlaying || activeSong == null) return;

            // 1. Calculate precise current song playback time based on network clock
            double currentSongTime = PhotonNetwork.Time - songStartTime;

            // 2. Handle note spawning (local preview of my side & opponent's side)
            SpawnApproachingNotes(currentSongTime);

            // 3. Update active notes' positions frame-rate independently
            UpdateNotePositions(currentSongTime);

            // 4. Evaluate keyboard inputs for 6 lanes
            HandleKeyboardInput(currentSongTime);

            // 5. Automatic miss processing for notes that passed unhit
            HandleMissProcessing(currentSongTime);

            // 6. Check song completion
            CheckSongCompletion(currentSongTime);
        }

        #region Spawning & Positioning

        private void SpawnApproachingNotes(double currentSongTime)
        {
            // Spawn ahead of time (e.g. 2 seconds before they hit judgment line)
            float lookAheadTime = 2f;

            if (playfieldParent == null) return;
            float fieldWidth = playfieldParent.rect.width;
            float laneWidth = fieldWidth / 6f;

            // Spawn My Notes
            while (myNextNoteIndex < activeSong.hitTimes.Length &&
                   activeSong.hitTimes[myNextNoteIndex] - currentSongTime <= lookAheadTime)
            {
                float hitTime = activeSong.hitTimes[myNextNoteIndex];
                int lane = activeSong.lanes[myNextNoteIndex];

                if (lane >= 0 && lane < 6 && myNotePrefab != null)
                {
                    // Instantiate UI note under playfieldParent (Player1Field in Screen Space - Overlay Canvas)
                    GameObject visual = Instantiate(myNotePrefab, playfieldParent);
                    
                    float targetX = -fieldWidth / 2f + (lane + 0.5f) * laneWidth;
                    RectTransform noteRect = visual.GetComponent<RectTransform>();
                    if (noteRect != null)
                    {
                        // Align note's anchors to bottom-center (same coordinate system as JudgmentLine's Y anchor)
                        noteRect.anchorMin = new Vector2(0.5f, 0f);
                        noteRect.anchorMax = new Vector2(0.5f, 0f);
                        noteRect.pivot = new Vector2(0.5f, 0.5f);
                        noteRect.anchoredPosition = new Vector2(targetX, uiSpawnY);
                    }
                    visual.transform.localScale = Vector3.one;

                    activeNotes.Add(new ActiveNote
                    {
                        hitTime = hitTime,
                        lane = lane,
                        visualObject = visual,
                        isMyNote = true
                    });
                }

                myNextNoteIndex++;
            }

            // Spawn Opponent Notes (simulated side-by-side visuals) - disabled for local-only view
            while (opponentNextNoteIndex < activeSong.hitTimes.Length &&
                   activeSong.hitTimes[opponentNextNoteIndex] - currentSongTime <= lookAheadTime)
            {
                opponentNextNoteIndex++;
            }
        }

        private void UpdateNotePositions(double currentSongTime)
        {
            if (playfieldParent == null) return;

            float fieldWidth = playfieldParent.rect.width;
            float laneWidth = fieldWidth / 6f;

            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                ActiveNote note = activeNotes[i];
                if (note.visualObject == null) continue;

                // Position based on math: y = judgmentPos + (hitTime - currentSongTime) * scrollSpeed
                float timeOffset = note.hitTime - (float)currentSongTime;
                
                // UI Coordinate mapping using layout constants
                float newY = actualJudgmentY + (timeOffset * uiScrollSpeed);

                // Align X coordinate inside the centered playfieldParent RectTransform
                float targetX = -fieldWidth / 2f + (note.lane + 0.5f) * laneWidth;

                RectTransform noteRect = note.visualObject.GetComponent<RectTransform>();
                if (noteRect != null)
                {
                    noteRect.anchoredPosition = new Vector2(targetX, newY);
                }
            }
        }

        #endregion

        #region Keyboard Inputs

        private void HandleKeyboardInput(double currentSongTime)
        {
            for (int lane = 0; lane < 6; lane++)
            {
                if (Input.GetKeyDown(inputKeys[lane]))
                {
                    EvaluateHit(lane, currentSongTime);
                    AnimateKeyPress(lane, true);
                }

                if (Input.GetKeyUp(inputKeys[lane]))
                {
                    AnimateKeyPress(lane, false);
                }
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

        private void EvaluateHit(int lane, double currentSongTime)
        {
            // Find the oldest unhit note in my lane
            ActiveNote targetNote = null;
            float smallestDiff = float.MaxValue;

            for (int i = 0; i < activeNotes.Count; i++)
            {
                ActiveNote note = activeNotes[i];
                if (note.isMyNote && note.lane == lane)
                {
                    float diff = Mathf.Abs(note.hitTime - (float)currentSongTime);
                    if (diff < smallestDiff)
                    {
                        smallestDiff = diff;
                        targetNote = note;
                    }
                }
            }

            if (targetNote != null && smallestDiff <= 0.18f)
            {
                // Assign Rating based on precision windows
                string rating = "";
                int scoreGain = 0;

                if (smallestDiff < 0.05f)
                {
                    rating = "<color=blue>PERFECT</color>";
                    scoreGain = 1000;
                    currentCombo++;
                }
                else if (smallestDiff < 0.09f)
                {
                    rating = "<color=green>GOOD</color>";
                    scoreGain = 600;
                    currentCombo++;
                }
                else if (smallestDiff < 0.13f)
                {
                    rating = "<color=yellow>NORMAL</color>";
                    scoreGain = 300;
                    currentCombo++;
                }
                else if (smallestDiff < 0.18f)
                {
                    rating = "<color=orange>BAD</color>";
                    scoreGain = 100;
                    currentCombo = 0; // Bad breaks combo or keeps it? Typically breaks.
                }

                currentScore += scoreGain;
                if (currentCombo > maxCombo) maxCombo = currentCombo;

                ShowHitFeedback(rating);
                
                // Tell GameSyncManager about new score
                GameSyncManager.Instance.UpdateLocalScore(currentScore);

                // Destroy note
                activeNotes.Remove(targetNote);
                Destroy(targetNote.visualObject);
            }
        }

        private void HandleMissProcessing(double currentSongTime)
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                ActiveNote note = activeNotes[i];
                
                // Miss occurs if note goes beyond the -0.18s window
                float diff = (float)currentSongTime - note.hitTime;
                
                if (diff > 0.18f)
                {
                    if (note.isMyNote)
                    {
                        currentCombo = 0;
                        ShowHitFeedback("<color=red>MISS</color>");
                    }

                    activeNotes.RemoveAt(i);
                    if (note.visualObject != null)
                    {
                        Destroy(note.visualObject);
                    }
                }
            }
        }

        private void ShowHitFeedback(string rating)
        {
            if (ratingText != null) ratingText.text = rating;
            if (comboText != null)
            {
                comboText.text = currentCombo > 0 ? $"{currentCombo} COMBO" : "";
            }
        }

        #endregion

        #region Completion

        private void CheckSongCompletion(double currentSongTime)
        {
            // Verify if all notes have been spawned and evaluated AND the audio track finished
            bool allNotesProcessed = myNextNoteIndex >= activeSong.hitTimes.Length &&
                                     opponentNextNoteIndex >= activeSong.hitTimes.Length &&
                                     activeNotes.Count == 0;

            bool audioFinished = audioSource != null && !audioSource.isPlaying && currentSongTime > 3.0;

            if ((allNotesProcessed || audioFinished) && songPlaying)
            {
                songPlaying = false;
                Debug.Log("Local gameplay track completed.");
                GameSyncManager.Instance.SetLocalFinished();
            }
        }

        #endregion
    }
}
