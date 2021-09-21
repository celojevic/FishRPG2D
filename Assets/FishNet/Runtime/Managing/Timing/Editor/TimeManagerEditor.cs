﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FishNet.Managing.Timing.Editing
{


    [CustomEditor(typeof(TimeManager), true)]
    [CanEditMultipleObjects]
    public class TimeManagerEditor : Editor
    {
        private SerializedProperty _automaticPhysics;
        private SerializedProperty _tickRate;

        private SerializedProperty _correctTiming;
        private SerializedProperty _maximumBufferedInputs;
        private SerializedProperty _targetBufferedInputs;
        private SerializedProperty _aggressiveTiming;

        protected virtual void OnEnable()
        {
            _automaticPhysics = serializedObject.FindProperty("_automaticPhysics");
            _tickRate = serializedObject.FindProperty("_tickRate");

            _correctTiming = serializedObject.FindProperty("_correctTiming");
            _maximumBufferedInputs = serializedObject.FindProperty("_maximumBufferedInputs");
            _targetBufferedInputs = serializedObject.FindProperty("_targetBufferedInputs");
            _aggressiveTiming = serializedObject.FindProperty("_aggressiveTiming");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            GUI.enabled = false;
            EditorGUILayout.ObjectField("Script:", MonoScript.FromMonoBehaviour((TimeManager)target), typeof(TimeManager), false);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(_automaticPhysics, new GUIContent("Automatic Physics", "True to let Unity run physics. False to let TimeManager run physics after each tick."));
            EditorGUILayout.PropertyField(_tickRate, new GUIContent("Tick Rate", "How many times per second the server will simulate"));

            EditorGUILayout.PropertyField(_correctTiming, new GUIContent("Correct Timing", "While true the server will ask clients to adjust simulation rate as needed."));
            if (_correctTiming.boolValue == true)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maximumBufferedInputs, new GUIContent("Maximum Buffered Inputs", "Maximum number of excessive input sent from client before entries are dropped. Client is expected to send roughly one input per server tick."));
                EditorGUILayout.PropertyField(_targetBufferedInputs, new GUIContent("Target Buffered Inputs", "Number of inputs server prefers to have buffered from clients."));
                //EditorGUILayout.PropertyField(_aggressiveTiming, new GUIContent("Aggressive Timing", "True to enable more accurate tick synchronization between client and server at the cost of bandwidth."));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif