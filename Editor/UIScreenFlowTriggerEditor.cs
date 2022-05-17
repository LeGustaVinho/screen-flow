using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LegendaryTools.Systems.ScreenFlow.Editor
{
    [CustomEditor(typeof(UIScreenFlowTrigger))]
    [CanEditMultipleObjects]
    public class UIScreenFlowTriggerEditor : UnityEditor.Editor
    {
        private SerializedProperty triggerModeProperty;
        private SerializedProperty uiEntityProperty;
        private SerializedProperty enqueueProperty;

        private bool IsPrefabMode => EditorSceneManager.IsPreviewScene((target as MonoBehaviour).gameObject.scene);

        private void OnEnable()
        {
            triggerModeProperty = serializedObject.FindProperty(nameof(UIScreenFlowTrigger.Mode));
            uiEntityProperty = serializedObject.FindProperty(nameof(UIScreenFlowTrigger.UiEntity));
            enqueueProperty = serializedObject.FindProperty(nameof(UIScreenFlowTrigger.Enqueue));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(triggerModeProperty);

            ScreenFlowTriggerMode currentMode = (ScreenFlowTriggerMode) Enum.ToObject(typeof(ScreenFlowTriggerMode),
                triggerModeProperty.enumValueIndex);

            if (currentMode == ScreenFlowTriggerMode.Trigger)
            {
                EditorGUILayout.PropertyField(uiEntityProperty);
            }

            EditorGUILayout.PropertyField(enqueueProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}