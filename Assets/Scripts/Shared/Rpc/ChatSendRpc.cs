using Unity.Collections;
using Unity.NetCode;

namespace Shared.Rpc
{
    public struct ChatSendRpc : IRpcCommand
    {
        public FixedString512Bytes Text;
    }
}