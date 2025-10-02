using Unity.Collections;
using Unity.NetCode;

namespace BastionFall.Features.Player.Shared.RPC
{
    public struct IdentifyPlayerRpc : IRpcCommand
    {
        public FixedString128Bytes Guid;
        public FixedString64Bytes DisplayName;
        public bool UsedPersistedName;
    }
}