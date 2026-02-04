using System.Collections.Generic;
using UnityEngine;
using KartGame.Track;

namespace KartGame.Gameplay
{
    public static class RaceUtils
    {
        public static float CalculateProgress(KartController kart, IReadOnlyList<Checkpoint> checkpoints)
        {
            if (kart == null || checkpoints == null || checkpoints.Count == 0)
            {
                return 0f;
            }

            int totalCheckpoints = checkpoints.Count;
            int lastCheckpoint = Mathf.Clamp(kart.LastCheckpoint.Value, 0, totalCheckpoints - 1);
            int nextCheckpoint = (lastCheckpoint + 1) % totalCheckpoints;

            Vector3 lastPos = checkpoints[lastCheckpoint].transform.position;
            Vector3 nextPos = checkpoints[nextCheckpoint].transform.position;
            float segmentLength = Vector3.Distance(lastPos, nextPos);
            float distToNext = Vector3.Distance(kart.transform.position, nextPos);
            float normalized = segmentLength > 0.01f ? 1f - Mathf.Clamp01(distToNext / segmentLength) : 0f;

            return kart.LapsCompleted.Value * totalCheckpoints + lastCheckpoint + normalized;
        }

        public static string FormatTime(float timeSeconds)
        {
            if (timeSeconds < 0f)
            {
                timeSeconds = 0f;
            }

            int minutes = Mathf.FloorToInt(timeSeconds / 60f);
            int seconds = Mathf.FloorToInt(timeSeconds % 60f);
            int millis = Mathf.FloorToInt((timeSeconds - Mathf.Floor(timeSeconds)) * 1000f);
            return $"{minutes:00}:{seconds:00}.{millis:000}";
        }
    }
}
