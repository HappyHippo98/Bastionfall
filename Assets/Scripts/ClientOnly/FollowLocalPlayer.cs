using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

public sealed class FollowLocalPlayer : MonoBehaviour
{
    World _clientWorld;
    EntityQuery _connQ;          // Query auf die Connection
    bool _connQCreated;

    void OnDestroy()
    {
        if (_connQCreated) _connQ.Dispose();
    }

    void LateUpdate()
    {
        // 1) Client-World nur per Name bestimmen (keine Queries! -> kein Konflikt mit AsyncLoadSceneJob)
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            if (!TryGetClientWorldByName(out _clientWorld))
                return; // noch nicht da
            // Query wird erst im nächsten Block erstellt
        }

        var em = _clientWorld.EntityManager;

        // 2) Query EINMAL anlegen (keine enableable Komponente drin!)
        if (!_connQCreated)
        {
            _connQ = em.CreateEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<NetworkId>(),
                    ComponentType.ReadOnly<NetworkStreamInGame>(),
                    ComponentType.ReadOnly<CommandTarget>()
                }
            });
            _connQCreated = true;
        }

        if (_connQ.IsEmptyIgnoreFilter)
            return; // noch nicht "InGame"

        // 3) Verbindung holen und Ziel-Entity (dein lokaler Player) aus CommandTarget lesen
        var conn = _connQ.GetSingletonEntity();
        var ct   = em.GetComponentData<CommandTarget>(conn);   // Feld heißt i.d.R. "targetEntity"

        if (ct.targetEntity == Entity.Null)                     // ggf. Feldname "TargetEntity" je nach NetCode-Version
            return;
        if (!em.HasComponent<LocalTransform>(ct.targetEntity))
            return;

        var lt = em.GetComponentData<LocalTransform>(ct.targetEntity);

        // 4) Kamera folgen lassen (simple Third-Person-Versatz)
        Vector3 pos = (Vector3)lt.Position;
        Vector3 fwd = (Vector3)lt.Forward();

        transform.position = pos + new Vector3(0f, 3f, -5f);
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }

    static bool TryGetClientWorldByName(out World w)
    {
        var worlds = World.All;
        for (int i = 0; i < worlds.Count; i++)
        {
            var candidate = worlds[i];
            // NetCode benennt die Client-World typischerweise "Client" / "ClientWorld0"
            if (candidate.Name.IndexOf("client", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                w = candidate;
                return true;
            }
        }
        w = null;
        return false;
    }
}
