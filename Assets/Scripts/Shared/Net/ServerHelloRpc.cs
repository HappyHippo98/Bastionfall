using Unity.Entities;
using Unity.NetCode;

namespace Game.Shared.Net
{
    // Muss ein struct (unmanaged) sein
    public struct ServerHelloRpc : IRpcCommand {}
}