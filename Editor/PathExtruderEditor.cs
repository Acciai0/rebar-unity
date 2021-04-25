using System;
using UnityEditor;
using UnityEngine;

namespace Rebar.Unity.Editor
{
    [CustomEditor(typeof(PathExtruder))]
    public class PathExtruderEditor : UnityEditor.Editor
    {
        private const int MIN_SUBDIVISIONS = 10;
        private const int MAX_SUBDIVISIONS = 300;
        private const int MIN_RESOLUTION = 3;
        private const int MAX_RESOLUTION = 200;
        private const int MIN_UNIFORM_SCALE = 0;

        public override void OnInspectorGUI()
        {
            var extruder = target as PathExtruder;

            int subdivisions = EditorGUILayout.IntSlider("Subdivisions", extruder.Subdivisions, MIN_SUBDIVISIONS, MAX_SUBDIVISIONS);
            int resolution = EditorGUILayout.IntSlider("Resolution", extruder.Resolution, MIN_RESOLUTION, MAX_RESOLUTION);
            float uniformScale = Mathf.Max(MIN_UNIFORM_SCALE, EditorGUILayout.FloatField("Uniform Scale", extruder.UniformScale));

            serializedObject.Update();
            if (subdivisions != extruder.Subdivisions) extruder.Subdivisions = subdivisions;
            if (resolution != extruder.Resolution) extruder.Resolution = resolution;
            if (uniformScale != extruder.UniformScale) extruder.UniformScale = uniformScale;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
