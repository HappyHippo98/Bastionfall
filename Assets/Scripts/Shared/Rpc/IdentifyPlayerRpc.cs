using Unity.Collections;
using Unity.NetCode;

namespace Shared.Rpc
{
    public struct IdentifyPlayerRpc : IRpcCommand
    {
        public FixedString128Bytes Guid;
        public FixedString64Bytes Name;
    }
}