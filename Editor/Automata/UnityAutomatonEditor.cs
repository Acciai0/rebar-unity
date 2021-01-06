using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace Rebar.Unity.Automata.Editor
{
    [UnityEditor.CustomEditor(typeof(UnityAutomaton))]
    public class UnityAutomatonEditor : UnityEditor.Editor
    {
        private const int ELEMENT_PADDING = 5;
        private const string BACKING_FIELD_FORMAT = "<{0}>k__BackingField";
        private const string TICK_MESSAGE = "This automaton ticks only when the Tick() method is explicitly called and PreventTicking is false.";
        private const string NULL_PSB_MESSAGE = "Null UnityBoard reference. This automaton publishes state change events on the GlobalBoard.";
        private const string REQUIRED_ENTRY_STATE_MESSAGE = "In order to start ticking since frame 1, a valid entry state is required." + 
                "Other states can still be added at runtime.";

        private SerializedProperty _startTickingAtAwake = null;
        private SerializedProperty _entryState = null;
        private SerializedProperty _publishStateChangedEvents = null;   
        private SerializedProperty _pubSubBoard = null;
        private SerializedProperty _tickTime = null;
        private SerializedProperty _tickPeriod = null;
        private SerializedProperty _onStateChange = null;

        private void OnEnable()
        {
            GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(fi => fi.FieldType == typeof(SerializedProperty))
                    .ToList()
                    .ForEach(fi => fi.SetValue(this, serializedObject.FindProperty(fi.Name)));
        }

        private string SpaceCapitalLetters(string str, bool substituteUnderscore, bool preserveAcronyms)
        {
            if (str.ToUpper() == str) return str;

            List<char> seq = new List<char>();
            bool lastWasCapital = false;
            for (int i = 0; i < str.Length; i++) {
                if (str[i] == '_' && substituteUnderscore)
                {
                    seq.Add(' ');
                    lastWasCapital = true;
                }
                else
                {
                    bool currentIsCapital = char.IsUpper(str[i]);
                    if (currentIsCapital && i != 0 && (!preserveAcronyms || !lastWasCapital))
                        seq.Add(' ');
                    seq.Add(str[i]);
                    lastWasCapital = currentIsCapital;
                }
            }
            return new string(seq.ToArray());
        }

        private string[] GetTickTimeValidValues(bool hideExternalOption) 
        {
            IEnumerable<string> names = Enum.GetNames(typeof(UnityAutomaton.TickTime))
                    .Select(n => SpaceCapitalLetters(n, true, false));
            if (hideExternalOption) 
                names = names.Take(names.Count() - 1);
            return names.ToArray();
        }

        private void DrawAwakeSettings()
        {
            EditorGUILayout.PropertyField(_startTickingAtAwake, new GUIContent("Start Ticking On Awake"));

            if (_startTickingAtAwake.boolValue)
            {
                EditorGUILayout.PropertyField(_entryState, new GUIContent("Entry State"));
                if (_entryState.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(REQUIRED_ENTRY_STATE_MESSAGE, MessageType.Error);
            }
            else _entryState.objectReferenceValue = null;
        }

        private void DrawEvents()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_publishStateChangedEvents, new GUIContent("Publish Events"));
            if (_publishStateChangedEvents.boolValue)
            {
                EditorGUILayout.PropertyField(_pubSubBoard, GUIContent.none);
                EditorGUILayout.EndHorizontal();
                if (_pubSubBoard.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(NULL_PSB_MESSAGE, MessageType.Info);
            }
            else 
            {
                EditorGUILayout.EndHorizontal();
                _pubSubBoard.objectReferenceValue = null;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_onStateChange);
        }

        private void DrawTickPeriod()
        {
            var hideExternalOption = _startTickingAtAwake.boolValue;

            _tickTime.intValue = EditorGUILayout.Popup("Tick on", _tickTime.intValue, GetTickTimeValidValues(hideExternalOption));
            var tickTime = (UnityAutomaton.TickTime)_tickTime.intValue;

            if (hideExternalOption && tickTime == UnityAutomaton.TickTime.ExternalRequest) _tickTime.intValue = 0;

            if (tickTime != UnityAutomaton.TickTime.ExternalRequest)
            {
                if (_tickPeriod.floatValue != 0)
                    _tickPeriod.floatValue = EditorGUILayout.Slider("Tick Period", _tickPeriod.floatValue, 0, 5);
                else
                {
                    bool periodFlag = EditorGUILayout.Toggle("Tick Each Frame", _tickPeriod.floatValue == 0);
                    if (!periodFlag) _tickPeriod.floatValue = .1f;
                }
            } 
            else  
            {
                _tickPeriod.floatValue = 0;
                EditorGUILayout.HelpBox(TICK_MESSAGE, MessageType.Info);
            }
        }

        private void DrawEditor()
        {
            serializedObject.Update();

            DrawAwakeSettings();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawTickPeriod();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            DrawEvents();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawInfo()
        {
            var automaton = (UnityAutomaton)target;

            if (automaton.PreventTicking && GUILayout.Button("Start Ticking")) 
                automaton.PreventTicking = false;
            else if (!automaton.PreventTicking && GUILayout.Button("Stop Ticking"))
                automaton.PreventTicking = true;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(new Rect(0, 0, EditorGUIUtility.singleLineHeight, EditorGUIUtility.singleLineHeight), Color.green);
            if (!automaton.PreventTicking)
            {
                UnityAutomaton.TickTime tickTime = (UnityAutomaton.TickTime)_tickTime.intValue;
                string tickTimeString = SpaceCapitalLetters(tickTime.ToString(), true, false);
                string addition = "";
                if (tickTime != UnityAutomaton.TickTime.ExternalRequest)
                {
                    float value = _tickPeriod.floatValue;
                    string what = value > 0 ? $"{value}s" : "frame";
                    addition = $"every {what}";
                }
                EditorGUILayout.LabelField($"Ticking on {tickTimeString} {addition}");
            }
            else EditorGUILayout.LabelField($"Not ticking");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Entry state:\t{automaton.EntryState?.Name}");
            EditorGUILayout.LabelField($"Current state:\t{automaton.CurrentState?.Name}");
            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            if (Application.isPlaying) DrawInfo();
            else DrawEditor();
        }
    }
}
