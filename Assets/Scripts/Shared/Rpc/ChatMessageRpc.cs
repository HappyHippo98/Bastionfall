using Unity.Collections;
using Unity.NetCode;

namespace Shared.Rpc
{
    public struct ChatMessageRpc : IRpcCommand
    {
        public int SenderNetworkId;
        public FixedString64Bytes SenderName;
        public FixedString512Bytes Text;
    }
}