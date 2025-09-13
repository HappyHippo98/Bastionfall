using Unity.Entities;

namespace ClientOnly.Authoring
{
    public struct AutoConnectState : IComponentData
{
    public double NextTryAt; 
}
}