using UnityEngine;

namespace DualBeat.Gameplay
{
    public enum JudgmentType
    {
        Perfect,
        Great,
        Good,
        Miss
    }

    public class ScoreManager : MonoBehaviour
    {
        private TMPro.TMP_Text comboText;
        private TMPro.TMP_Text ratingText;

        public int CurrentScore { get; private set; }
        public int CurrentCombo { get; private set; }
        public int MaxCombo { get; private set; }

        public void Initialize(TMPro.TMP_Text comboText, TMPro.TMP_Text ratingText)
        {
            this.comboText = comboText;
            this.ratingText = ratingText;
            ResetScore();
        }

        public void ResetScore()
        {
            CurrentScore = 0;
            CurrentCombo = 0;
            MaxCombo = 0;
            UpdateUI("");
        }

        public void ApplyJudgment(JudgmentType judgment)
        {
            string rating = "";
            int scoreGain = 0;

            switch (judgment)
            {
                case JudgmentType.Perfect:
                    rating = "<color=blue>PERFECT</color>";
                    scoreGain = 1000;
                    CurrentCombo++;
                    break;
                case JudgmentType.Great:
                    rating = "<color=green>GREAT</color>";
                    scoreGain = 600;
                    CurrentCombo++;
                    break;
                case JudgmentType.Good:
                    rating = "<color=yellow>GOOD</color>";
                    scoreGain = 300;
                    CurrentCombo++;
                    break;
                case JudgmentType.Miss:
                    rating = "<color=red>MISS</color>";
                    scoreGain = 0;
                    CurrentCombo = 0;
                    break;
            }

            CurrentScore += scoreGain;
            if (CurrentCombo > MaxCombo)
            {
                MaxCombo = CurrentCombo;
            }

            UpdateUI(rating);

            if (GameSyncManager.Instance != null)
            {
                GameSyncManager.Instance.UpdateLocalScore(CurrentScore);
            }
        }

        private void UpdateUI(string rating)
        {
            if (ratingText != null) ratingText.text = rating;
            if (comboText != null)
            {
                comboText.text = CurrentCombo > 0 ? $"{CurrentCombo} COMBO" : "";
            }
        }
    }
}
