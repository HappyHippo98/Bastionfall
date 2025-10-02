using BastionFall.Features.Chat.Shared.RPC;
using Unity.NetCode;

namespace BastionFall.Features.Chat.Client.Systems
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