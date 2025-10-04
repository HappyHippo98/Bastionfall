using Unity.Entities;

namespace BastionFall.Core.Shared.Input
{
    public struct ChatUiInput : IComponentData
    {
        public float Scroll;      
        public sbyte HistoryDelta;
        public byte Autocomplete; 
        public byte Submit;       
    }
}