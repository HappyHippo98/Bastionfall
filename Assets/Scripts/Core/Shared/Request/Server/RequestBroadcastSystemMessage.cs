using Unity.Collections;
using Unity.Entities;

namespace BastionFall.Core.Shared.Request.Server
{
    public struct RequestBroadcastSystemMessage : IComponentData
    {
        public FixedString512Bytes Text;
    }
}