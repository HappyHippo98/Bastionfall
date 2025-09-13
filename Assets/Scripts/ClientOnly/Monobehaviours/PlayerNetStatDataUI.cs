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
            gameObject.SetActive(false);
        }

        private void Update()
        {
            Refresh();
            /*
            if (Input.GetKeyDown(KeyCode.T))
            {
                gameObject.SetActive(true);
                Refresh();
            }
            if (Input.GetKeyUp(KeyCode.T))
            {
                ClearAll();
                gameObject.SetActive(false);
            }

            if (gameObject.activeSelf)
                Refresh();
        */
        }

        void Refresh()
        {
            var em = ClientServerBootstrap.ClientWorld.EntityManager;
            var q  = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerNetStatsData>());

            using var ents  = q.ToEntityArray(Allocator.Temp);
            using var stats = q.ToComponentDataArray<PlayerNetStatsData>(Allocator.Temp);

            var seen = new HashSet<Entity>();
            for (int i = 0; i < ents.Length; i++)
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
                ui.Setup(stats[i]);
            }

            var toRemove = new List<Entity>();
            foreach (var kv in _rowsByEntity)
                if (!seen.Contains(kv.Key)) { Destroy(kv.Value.gameObject); toRemove.Add(kv.Key); }
            foreach (var e in toRemove) _rowsByEntity.Remove(e);
        }

        void ClearAll()
        {
            foreach (var tr in _rowsByEntity.Values) Destroy(tr.gameObject);
            _rowsByEntity.Clear();
        }
    }
}
