using System.Collections.Generic;
using UnityEngine;

namespace DualBeat.Gameplay
{
    public class ChartData
    {
        public float bpm { get; private set; }
        public List<NoteRuntimeInfo> notes { get; private set; } = new List<NoteRuntimeInfo>();

        public ChartData(SongData songData)
        {
            if (songData == null) return;

            bpm = songData.bpm;
            float secPerBeat = 60f / bpm;

            if (songData.notes != null)
            {
                foreach (var noteInfo in songData.notes)
                {
                    notes.Add(new NoteRuntimeInfo
                    {
                        beat = noteInfo.beat,
                        lane = noteInfo.lane,
                        hitTime = noteInfo.beat * secPerBeat
                    });
                }
            }

            // Sort notes chronologically by hitTime
            notes.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }
    }

    public class NoteRuntimeInfo
    {
        public float beat;
        public int lane;
        public float hitTime;
    }
}
