using Unity.Entities;

namespace ClientOnly.Input
{
    /*
    public partial class InputSystem : SystemBase
    {
        private InputControls _inputControls;


        protected override void OnCreate()
        {
            _inputControls = new InputControls();
            _inputControls.Enable();
            var playerInputSystem = EntityManager.World.CreateSystemManaged<PlayerInputSystem>();
            playerInputSystem.SetPlayerMapActions(_inputControls.InGameMap);
        }

        protected override void OnUpdate()
        {
        }
    }
    */
}