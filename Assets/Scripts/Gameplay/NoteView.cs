using UnityEngine;

namespace DualBeat.Gameplay
{
    public class NoteView : MonoBehaviour
    {
        private float hitTime;
        private int lane;
        private float judgmentY;
        private float scrollSpeed;
        private float targetX;
        private RectTransform rectTransform;

        public float HitTime => hitTime;
        public int Lane => lane;

        public void Initialize(float hitTime, int lane, float targetX, float judgmentY, float scrollSpeed)
        {
            this.hitTime = hitTime;
            this.lane = lane;
            this.targetX = targetX;
            this.judgmentY = judgmentY;
            this.scrollSpeed = scrollSpeed;
            rectTransform = GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(targetX, rectTransform.anchoredPosition.y);
            }
            transform.localScale = Vector3.one;
        }

        public void UpdatePosition(double currentSongTime)
        {
            if (rectTransform == null) return;

            float timeRemaining = hitTime - (float)currentSongTime;
            float positionY = judgmentY + (timeRemaining * scrollSpeed);
            rectTransform.anchoredPosition = new Vector2(targetX, positionY);
        }
    }
}
