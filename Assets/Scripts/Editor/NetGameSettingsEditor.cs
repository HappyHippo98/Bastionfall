// Assets/Scripts/Editor/NetGameSettingsEditor.cs

using Game.Shared.Config;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(NetGameSettings))]
    public class NetGameSettingsEditor : UnityEditor.Editor
    {
        SerializedProperty playProfile, forceClient, address, port, despawn, runMode, preferListen;

        void OnEnable()
        {
            playProfile  = serializedObject.FindProperty("PlayProfile");
            forceClient  = serializedObject.FindProperty("ForceClientPlayType");
            address      = serializedObject.FindProperty("ConnectAddress");
            port         = serializedObject.FindProperty("Port");
            despawn      = serializedObject.FindProperty("DespawnOnDisconnect");
            runMode      = serializedObject.FindProperty("PlayerRunMode");
            preferListen = serializedObject.FindProperty("PreferListenWhenLocal");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(playProfile, new GUIContent("Play Profile"));
            var profile = (EditorPlayProfile)playProfile.enumValueIndex;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Network", EditorStyles.boldLabel);

            if (profile == EditorPlayProfile.DedicatedServer)
            {
                EditorGUILayout.PropertyField(address, new GUIContent("Address"));
                EditorGUILayout.PropertyField(port, new GUIContent("Port"));
            }
            else if (profile == EditorPlayProfile.DedicatedLocalServer)
            {
                if (address.stringValue != "127.0.0.1") address.stringValue = "127.0.0.1";
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField("Address", "127.0.0.1");
                EditorGUILayout.PropertyField(port, new GUIContent("Port"));
                EditorGUILayout.HelpBox("Lokaler Dedicated Server nutzt immer 127.0.0.1.", MessageType.Info);
            }
            else // Client
            {
                EditorGUILayout.PropertyField(address, new GUIContent("Address"));
                EditorGUILayout.PropertyField(port, new GUIContent("Port"));
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Server Only", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(despawn, new GUIContent("Despawn On Disconnect"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Defaults (non-Editor)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(runMode, new GUIContent("Run Mode (Player Build)"));
            EditorGUILayout.PropertyField(preferListen, new GUIContent("Prefer Listen When Local"));

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Wählst du hier 'Client', setzt der Inspector (wenn 'Force Client PlayType' aktiv ist) die NetCode-PlayTools im Editor auf Client.",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(forceClient, new GUIContent("Force Client PlayType"));
                if (GUILayout.Button("Apply Now", GUILayout.Width(100)))
                {
                    NetCodePlayToolsProxy.TrySetClient();
                    Debug.Log("[NetGameSettings] PlayTools → Client");
                }
            }

            serializedObject.ApplyModifiedProperties();

            // Auto-Override: Profil=Client + Force aktiv → PlayTools: Client
            if (profile == EditorPlayProfile.Client && forceClient.boolValue)
                NetCodePlayToolsProxy.TrySetClient();
        }
    }
}
