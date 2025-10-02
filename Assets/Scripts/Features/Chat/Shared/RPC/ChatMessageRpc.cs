using Unity.Collections;
using Unity.NetCode;

namespace BastionFall.Features.Chat.Shared.RPC
{
    public struct ChatMessageRpc : IRpcCommand
    {
        public int SenderNetworkId;
        public FixedString64Bytes SenderName;
        public FixedString512Bytes Text;
    }
}