using UnityEngine;
using System.Collections.Generic;

namespace DualBeat.Gameplay
{
    [CreateAssetMenu(fileName = "NewSongData", menuName = "Rhythm Game/Song Data", order = 1)]
    public class SongData : ScriptableObject
    {
        [System.Serializable]
        public struct NoteInfo
        {
            public float beat;
            public int lane;
        }

        [Header("Song Info")]
        public string songTitle;
        public string artistName;
        public float bpm;
        public AudioClip audioClip;

        [Header("BPM & Beat Chart")]
        public List<NoteInfo> notes = new List<NoteInfo>();

        [Header("Legacy Chart (Deprecated)")]
        [System.Obsolete("Use notes instead of hitTimes.")]
        [HideInInspector]
        public float[] hitTimes;

        [System.Obsolete("Use notes instead of lanes.")]
        [HideInInspector]
        public int[] lanes;

        public bool IsChartValid()
        {
            return notes != null && notes.Count > 0;
        }

        private void OnValidate()
        {
            // Auto-migrate old hitTimes & lanes to the new BPM/Beat-based notes list
            #if UNITY_EDITOR
            if ((notes == null || notes.Count == 0) && hitTimes != null && hitTimes.Length > 0 && bpm > 0)
            {
                notes = new List<NoteInfo>();
                int length = Mathf.Min(hitTimes.Length, lanes != null ? lanes.Length : 0);
                for (int i = 0; i < length; i++)
                {
                    float beat = hitTimes[i] * (bpm / 60f);
                    notes.Add(new NoteInfo { beat = beat, lane = lanes[i] });
                }
                Debug.Log($"[SongData] Migrated {notes.Count} notes from hitTimes to Beat-based notes in SongData '{name}'.");
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #endif
        }
    }
}
