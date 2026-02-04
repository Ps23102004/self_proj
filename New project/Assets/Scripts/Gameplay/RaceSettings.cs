using UnityEngine;

namespace KartGame.Gameplay
{
    [CreateAssetMenu(menuName = "Kart/Settings/Race Settings", fileName = "RaceSettings")]
    public class RaceSettings : ScriptableObject
    {
        public int totalLaps = 3;
        public float countdownSeconds = 3f;
        public float respawnDelay = 1.5f;
        public float outOfBoundsY = -3f;
    }
}
