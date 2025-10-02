using Unity.Collections;
using Unity.NetCode;

namespace BastionFall.Features.Chat.Shared.RPC
{
    public struct ChatSendRpc : IRpcCommand
    {
        public FixedString512Bytes Text;
    }
}