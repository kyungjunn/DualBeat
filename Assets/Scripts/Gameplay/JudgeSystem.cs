using System.Collections.Generic;
using UnityEngine;

namespace DualBeat.Gameplay
{
    public class JudgeSystem : MonoBehaviour
    {
        private ScoreManager scoreManager;

        // Judgment windows in seconds (ms / 1000)
        public const float PERFECT_WINDOW = 0.030f; // 30ms
        public const float GREAT_WINDOW = 0.070f;   // 70ms
        public const float GOOD_WINDOW = 0.120f;    // 120ms

        public void Initialize(ScoreManager scoreManager)
        {
            this.scoreManager = scoreManager;
        }

        public void EvaluateKeyPress(int lane, double currentSongTime, List<NoteView> activeNotes)
        {
            NoteView targetNote = null;
            float smallestDiff = float.MaxValue;

            for (int i = 0; i < activeNotes.Count; i++)
            {
                NoteView note = activeNotes[i];
                if (note.Lane == lane)
                {
                    float diff = Mathf.Abs(note.HitTime - (float)currentSongTime);
                    if (diff < smallestDiff)
                    {
                        smallestDiff = diff;
                        targetNote = note;
                    }
                }
            }

            // Hitting is allowed within the GOOD window (120ms)
            if (targetNote != null && smallestDiff <= GOOD_WINDOW)
            {
                JudgmentType judgment;
                if (smallestDiff <= PERFECT_WINDOW)
                {
                    judgment = JudgmentType.Perfect;
                }
                else if (smallestDiff <= GREAT_WINDOW)
                {
                    judgment = JudgmentType.Great;
                }
                else
                {
                    judgment = JudgmentType.Good;
                }

                scoreManager.ApplyJudgment(judgment);

                activeNotes.Remove(targetNote);
                Destroy(targetNote.gameObject);
            }
        }

        public void ProcessMisses(double currentSongTime, List<NoteView> activeNotes)
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                NoteView note = activeNotes[i];
                float diff = (float)currentSongTime - note.HitTime;

                // If the note has passed the GOOD window, it's a miss
                if (diff > GOOD_WINDOW)
                {
                    scoreManager.ApplyJudgment(JudgmentType.Miss);
                    activeNotes.RemoveAt(i);
                    Destroy(note.gameObject);
                }
            }
        }
    }
}
