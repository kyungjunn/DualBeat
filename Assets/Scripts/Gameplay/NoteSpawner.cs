using System.Collections.Generic;
using UnityEngine;

namespace DualBeat.Gameplay
{
    public class NoteSpawner : MonoBehaviour
    {
        private GameObject myNotePrefab;
        private float lookAheadTime = 2f;
        private float scrollSpeed = 400f; // UI Scroll speed (pixels per second)

        private RectTransform playfieldParent;
        private float actualJudgmentY = 100f;

        public float ScrollSpeed => scrollSpeed;
        public float ActualJudgmentY => actualJudgmentY;

        public void Initialize(RectTransform playfieldParent, float actualJudgmentY, GameObject notePrefab, float scrollSpeed = 400f, float lookAheadTime = 2f)
        {
            this.playfieldParent = playfieldParent;
            this.actualJudgmentY = actualJudgmentY;
            this.myNotePrefab = notePrefab;
            this.scrollSpeed = scrollSpeed;
            this.lookAheadTime = lookAheadTime;
        }

        public void SpawnNotes(double currentSongTime, ChartData chart, ref int nextNoteIndex, List<NoteView> activeNotes)
        {
            if (playfieldParent == null || chart == null) return;

            float fieldWidth = playfieldParent.rect.width;
            float laneWidth = fieldWidth / 6f;

            while (nextNoteIndex < chart.notes.Count &&
                   chart.notes[nextNoteIndex].hitTime - currentSongTime <= lookAheadTime)
            {
                var noteInfo = chart.notes[nextNoteIndex];
                
                if (noteInfo.lane >= 0 && noteInfo.lane < 6 && myNotePrefab != null)
                {
                    GameObject visual = Instantiate(myNotePrefab, playfieldParent);
                    
                    float targetX = -fieldWidth / 2f + (noteInfo.lane + 0.5f) * laneWidth;
                    NoteView noteView = visual.GetComponent<NoteView>();
                    if (noteView == null)
                    {
                        noteView = visual.AddComponent<NoteView>();
                    }

                    noteView.Initialize(noteInfo.hitTime, noteInfo.lane, targetX, actualJudgmentY, scrollSpeed);
                    
                    // Immediately calculate and set the initial Y position to prevent 1-frame rendering delay
                    noteView.UpdatePosition(currentSongTime);

                    // Print timing telemetry log for debugging
                    float timeRemaining = noteInfo.hitTime - (float)currentSongTime;
                    float positionY = actualJudgmentY + (timeRemaining * scrollSpeed);
                    Debug.Log($"[Note Spawner] Note Spawned at Lane {noteInfo.lane} | " +
                              $"SongTime: {currentSongTime:F4} | " +
                              $"hitTime: {noteInfo.hitTime:F4} | " +
                              $"timeRemaining: {timeRemaining:F4} | " +
                              $"positionY: {positionY:F4}");

                    activeNotes.Add(noteView);
                }

                nextNoteIndex++;
            }
        }
    }
}
