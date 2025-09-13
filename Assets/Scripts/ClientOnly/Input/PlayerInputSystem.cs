using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClientOnly.Input
{
    [DisableAutoCreation]
    public partial class PlayerInputSystem : SystemBase
    {
        
        private InputControls.InGameMapActions _ingameMapActions;

        public void SetPlayerMapActions(InputControls.InGameMapActions ingameMapActions)
        {
            this._ingameMapActions = ingameMapActions;
            _ingameMapActions.Move.performed += OnMovePerformed;
        }

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            UnityEngine.Debug.Log($" [ECS] Performed move: {context.ReadValue<float>()}");
        }
        
        protected override void OnUpdate()
        
        {
            
        }

        protected override void OnDestroy()
        {
            _ingameMapActions.Move.performed -= OnMovePerformed;
        }
        
    }
}