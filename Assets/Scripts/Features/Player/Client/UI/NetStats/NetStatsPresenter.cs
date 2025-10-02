using BastionFall.Core.Shared.Input;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
// NetcodePlayerInput

namespace BastionFall.Features.Player.Client.UI.NetStats
{
    public class NetStatsPresenter : MonoBehaviour
    {
        [SerializeField] private PlayerNetStatDataUI ui;

        private EntityManager _em;
        private EntityQuery _localInputQ;

        private void Awake()
        {
            if (!ui) ui = FindObjectOfType<PlayerNetStatDataUI>(true);
        }

        private void Start()
        {
            var world = ClientServerBootstrap.ClientWorld;
            if (world == null) return;

            _em = world.EntityManager;
            _localInputQ = _em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly<NetcodePlayerInput>(),
                    ComponentType.ReadOnly<GhostOwnerIsLocal>()
                }
            });
        }

        private void LateUpdate()
        {
            if (_em == default || ui == null) return;
            if (_localInputQ.CalculateEntityCount() == 0) return;

            using var ents = _localInputQ.ToEntityArray(Allocator.Temp);
            var e = ents[0];
            var input = _em.GetComponentData<NetcodePlayerInput>(e);

            ui.SetVisible(input.NetStatsHeld == 1);
            // Die UI liest die Daten selbst in Update() (so ist deine aktuelle UI implementiert). :contentReference[oaicite:2]{index=2}
        }
    }
}