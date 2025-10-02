using Shared.Rpc;
using Unity.Entities;
using Unity.NetCode;

namespace ClientOnly.Chat
{
    public static class ChatClientApi
    {
        public static void Send(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            var world = ClientServerBootstrap.ClientWorld;
            if (world == null) return;

            var em = world.EntityManager;
            var e = em.CreateEntity();
            em.AddComponentData(e, new ChatSendRpc { Text = text });
            em.AddComponentData(e, new SendRpcCommandRequest());
        }
    }
}