using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace BastionFall.Features.Chat.Shared.Authoring
{
    public class ChatConfigAuthoring : MonoBehaviour
    {
        [Tooltip("Wie viele Messages lokal puffern (nur beim Client, keine Persistenz).")] [Min(1)]
        public int maxBufferedMessages = 25;

        private class Baker : Baker<ChatConfigAuthoring>
        {
            public override void Bake(ChatConfigAuthoring a)
            {
                var e = GetEntity(TransformUsageFlags.None);
                AddComponent(e, new ChatBuffer { MaxMessages = a.maxBufferedMessages });
                AddBuffer<ChatMessageEntry>(e);
            }
        }
    }

    public struct ChatBuffer : IComponentData
    {
        public int MaxMessages;
    }

    public struct ChatMessageEntry : IBufferElementData
    {
        public int SenderNetworkId;
        public FixedString64Bytes SenderName;
        public FixedString512Bytes Text;
        public uint ServerTick;
    }
}