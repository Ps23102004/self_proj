using System;
using Unity.Collections;
using Unity.Netcode;

namespace KartGame.Networking
{
    public struct LobbyPlayerState : INetworkSerializable, IEquatable<LobbyPlayerState>
    {
        public ulong ClientId;
        public FixedString32Bytes Name;
        public bool Ready;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Ready);
        }

        public bool Equals(LobbyPlayerState other)
        {
            return ClientId == other.ClientId && Ready == other.Ready && Name.Equals(other.Name);
        }
    }
}
