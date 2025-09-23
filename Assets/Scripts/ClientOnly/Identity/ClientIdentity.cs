using System;
using Shared.Util.Identity;
using Unity.Collections;
using UnityEngine;

namespace ClientOnly.Identity
{
    /// <summary>
    /// Verwaltet Editor/Player-gescopte PlayerPrefs-Keys + Name/Guid.
    /// Editor und Build auf demselben Rechner bekommen so unterschiedliche Identitäten.
    /// </summary>
    public static class ClientIdentity
    {
        private const string GUID_KEY_BASE = "BF_GUID";
        private const string NAME_KEY_BASE = "BF_NAME";

#if UNITY_EDITOR
        private const string SCOPE = "EDITOR_BF_";
#else
        private const string SCOPE = "PLAYER_BF_";
#endif

        private static string GuidKey => SCOPE + GUID_KEY_BASE;
        private static string NameKey => SCOPE + NAME_KEY_BASE;

        public static FixedString128Bytes GetOrCreateGuid()
        {
            string guid;
            if (PlayerPrefs.HasKey(GuidKey))
            {
                guid = PlayerPrefs.GetString(GuidKey);
                if (string.IsNullOrEmpty(guid))
                {
                    guid = System.Guid.NewGuid().ToString("N");
                    PlayerPrefs.SetString(GuidKey, guid);
                    PlayerPrefs.Save();
                }
            }
            else
            {
                guid = System.Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(GuidKey, guid);
                PlayerPrefs.Save();
            }

            return new FixedString128Bytes(guid);
        }

        public static FixedString64Bytes GetDisplayName(bool usePersistedName)
        {
            if (usePersistedName)
            {
                string name;
                if (PlayerPrefs.HasKey(NameKey))
                {
                    name = PlayerPrefs.GetString(NameKey);
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = NameBank.GetRandomName();
                        PlayerPrefs.SetString(NameKey, name);
                        PlayerPrefs.Save();
                    }
                }
                else
                {
                    name = NameBank.GetRandomName();
                    PlayerPrefs.SetString(NameKey, name);
                    PlayerPrefs.Save();
                }

                return new FixedString64Bytes(name);
            }
            else
            {
                // Session-Name (nicht gespeichert)
                return new FixedString64Bytes(NameBank.GetRandomName());
            }
        }

        public static void ClearStoredName()
        {
            if (PlayerPrefs.HasKey(NameKey))
            {
                PlayerPrefs.DeleteKey(NameKey);
                PlayerPrefs.Save();
            }
        }

        public static void ClearAll()
        {
            if (PlayerPrefs.HasKey(NameKey)) PlayerPrefs.DeleteKey(NameKey);
            if (PlayerPrefs.HasKey(GuidKey)) PlayerPrefs.DeleteKey(GuidKey);
            PlayerPrefs.Save();
        }
    }
}
