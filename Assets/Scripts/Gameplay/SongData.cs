using UnityEngine;

namespace DualBeat.Gameplay
{
    [CreateAssetMenu(fileName = "NewSongData", menuName = "Rhythm Game/Song Data", order = 1)]
    public class SongData : ScriptableObject
    {
        [Header("Song Info")]
        public string songTitle;
        public string artistName;
        public float bpm;
        public AudioClip audioClip;

        [Header("Beatmap Chart")]
        [Tooltip("The time in seconds when each note should hit the judgment line.")]
        public float[] hitTimes;

        [Tooltip("The lane index (0 to 5) for each note corresponding to the hitTime index.")]
        public int[] lanes;

        // Simple check to ensure chart arrays are equal in length
        public bool IsChartValid()
        {
            return hitTimes != null && lanes != null && hitTimes.Length == lanes.Length;
        }
    }
}
