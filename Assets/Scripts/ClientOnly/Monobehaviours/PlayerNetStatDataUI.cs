using System.Collections.Generic;
using Shared.Authoring.Monitoring;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly.Monobehaviours
{
    public class PlayerNetStatDataUI : MonoBehaviour
    {
        [SerializeField] private Transform playerNetStatDataContainer;
        [SerializeField] private Transform playerNetStatDataTemplate;

        private readonly Dictionary<Entity, Transform> _rowsByEntity = new();

        private void Awake()
        {
            playerNetStatDataTemplate.gameObject.SetActive(false);
            playerNetStatDataContainer.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Tab))
            {
                playerNetStatDataContainer.gameObject.SetActive(true);
                Refresh();
            }
            if (UnityEngine.Input.GetKeyUp(KeyCode.Tab))
            {
                ClearAll();
                playerNetStatDataContainer.gameObject.SetActive(false);
            }

            if (playerNetStatDataContainer.gameObject.activeSelf)
                Refresh();
        
        }

        private void Refresh()
        {
            var em = ClientServerBootstrap.ClientWorld.EntityManager;
            var q = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerNetStatsData>(),ComponentType.ReadOnly<PlayerIdentity>());

            using var ents = q.ToEntityArray(Allocator.Temp);
            using var stats = q.ToComponentDataArray<PlayerNetStatsData>(Allocator.Temp);
            using var identities = q.ToComponentDataArray<PlayerIdentity>(Allocator.Temp);

            var seen = new HashSet<Entity>();
            for (var i = 0; i < ents.Length; i++)
            {
                var e = ents[i];
                seen.Add(e);

                if (!_rowsByEntity.TryGetValue(e, out var row))
                {
                    row = Instantiate(playerNetStatDataTemplate, playerNetStatDataContainer);
                    row.gameObject.SetActive(true);
                    _rowsByEntity[e] = row;
                }

                var ui = row.GetComponent<PlayerNetStatDataSingleUI>();
                ui.Setup(stats[i],identities[i]);
            }

            var toRemove = new List<Entity>();
            foreach (var kv in _rowsByEntity)
                if (!seen.Contains(kv.Key))
                {
                    Destroy(kv.Value.gameObject);
                    toRemove.Add(kv.Key);
                }

            foreach (var e in toRemove) _rowsByEntity.Remove(e);
        }

        private void ClearAll()
        {
            foreach (var tr in _rowsByEntity.Values) Destroy(tr.gameObject);
            _rowsByEntity.Clear();
        }
    }
}