using UnityEngine;

namespace DualBeat.Gameplay
{
    public class RhythmClock : MonoBehaviour
    {
        public double DspSongStartTime { get; private set; } = -1;
        public bool IsRunning { get; private set; } = false;

        public double SongTime
        {
            get
            {
                if (!IsRunning) return 0;
                return AudioSettings.dspTime - DspSongStartTime;
            }
        }

        public void StartClock(double dspStartTime)
        {
            DspSongStartTime = dspStartTime;
            IsRunning = true;
        }

        public void StopClock()
        {
            IsRunning = false;
        }
    }
}
