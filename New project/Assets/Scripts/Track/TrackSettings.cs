using UnityEngine;

namespace KartGame.Track
{
    [CreateAssetMenu(menuName = "Kart/Settings/Track Settings", fileName = "TrackSettings")]
    public class TrackSettings : ScriptableObject
    {
        public float trackWidth = 7f;
        public float wallHeight = 1.2f;
        public float wallThickness = 0.4f;
        public float checkpointWidth = 8f;
        public float checkpointDepth = 2f;
        public Vector3[] waypoints;
    }
}
