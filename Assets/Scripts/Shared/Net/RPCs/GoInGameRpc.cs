using Unity.Entities;
using Unity.NetCode;

namespace Game.Shared.Net   // <- WICHTIG: exakt dieser Namespace
{
    public struct GoInGameRpc : IRpcCommand
    {
    }
}