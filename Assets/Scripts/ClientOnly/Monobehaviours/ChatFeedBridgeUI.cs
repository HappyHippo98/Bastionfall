
/*
using Shared.Authoring.Chat;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace ClientOnly.Monobehaviours
{
    
    public class ChatFeedBridgeUI : MonoBehaviour
    {
        [SerializeField] private ChatFeedUI feed;

        [Header("Default Colors")]
        [SerializeField] private Color background = new(0, 0, 0, 0.35f);
        [SerializeField] private Color author     = Color.white;
        [SerializeField] private Color content    = Color.white;

        int _lastSeen;

        void Reset() { if (!feed) feed = GetComponent<ChatFeedUI>(); }

        void Update()
        {
            if (!feed) return;

            var world = ClientServerBootstrap.ClientWorld;
            if (world == null) return;

            var em = world.EntityManager;
            using var q = em.CreateEntityQuery(ComponentType.ReadOnly<ChatBuffer>());
            if (q.IsEmpty) return;

            var es = q.ToEntityArray(Allocator.Temp);
            Entity eWithBuf = Entity.Null;
            foreach (var e in es)
                if (em.HasBuffer<ChatMessageEntry>(e)) { eWithBuf = e; break; }
            if (eWithBuf == Entity.Null) return;

            var buf = em.GetBuffer<ChatMessageEntry>(eWithBuf);

            // Reset, falls Buffer kleiner wurde
            if (buf.Length < _lastSeen) _lastSeen = 0;

            for (int i = _lastSeen; i < buf.Length; i++)
            {
                var m = buf[i];
                feed.AddItem(m.SenderName.ToString(), m.Text.ToString(), background, author, content);
            }

            _lastSeen = buf.Length;
        }
    }
}
*/