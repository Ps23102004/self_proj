using UnityEngine;

namespace KartGame.Gameplay
{
    [CreateAssetMenu(menuName = "Kart/Settings/Kart Settings", fileName = "KartSettings")]
    public class KartSettings : ScriptableObject
    {
        [Header("Speed")]
        public float maxSpeed = 18f;
        public float maxReverseSpeed = 6f;
        public float acceleration = 18f;
        public float brakeAcceleration = 28f;

        [Header("Steering")]
        public float steerStrength = 140f;
        public float steerSpeedFactor = 0.6f;

        [Header("Grip")]
        [Range(0f, 1f)] public float lateralFriction = 0.85f;
        [Range(0f, 1f)] public float driftLateralFriction = 0.6f;

        [Header("Drift")]
        public float driftChargeRate = 1.1f;
        public float maxDriftCharge = 2.5f;
        public float minDriftForBoost = 0.7f;
        public float driftBoostForce = 8f;
        public float driftSteerBonus = 1.2f;
        public float minDriftSpeed = 4f;

        [Header("Physics")]
        public float extraGravity = 20f;
    }
}
