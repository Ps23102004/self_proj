using System;
using Unity.Netcode;

namespace KartGame.Gameplay
{
    public struct RaceResult : INetworkSerializable, IEquatable<RaceResult>
    {
        public ulong ClientId;
        public float Time;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Time);
        }

        public bool Equals(RaceResult other)
        {
            return ClientId == other.ClientId && Math.Abs(Time - other.Time) < 0.001f;
        }
    }
}
