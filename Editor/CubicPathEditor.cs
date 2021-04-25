using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Rebar.Unity.Editor
{
    [CustomEditor(typeof(CubicPath))]
    public class CubicPathEditor : UnityEditor.Editor
    {
        private const int POINTS_LIST_SPACING = 9;
        private const int POINTS_LABEL_WIDTH = 50;
        private const int MIN_SUBDIVISIONS = 10;
        private const int MAX_SUBDIVISIONS = 300;
        private const float MIN_SINGLE_LINE_SCREEN_WIDTH = 345;
        private const float MOVEMENT_STEP = .001f;
        private const float HANDLE_SIZE_FACTOR = .1f;
        private const string SUBS_KEY = "Subdivisions";
        private const string SHOW_HANDLES_KEY = "ShowHandles";
        private const string SHOW_HANDLES_ICON = "EditCollider";
        private const string EDIT_INTERP_KEY = "EditInterpolants";
        private const string COORDS_SYSTEM_KEY = "CoordinatesSystem";
        private const string POINTS_COLLAPSED_KEY = "PointsListCollapsed";

        private SerializedProperty _loop = null;
        private SerializedProperty _points = null;

        private CubicPath _path = null;
        private EditorState _state = null;
        private Texture _handlesIcon = null;
        private GUIStyle _separatorLineStyle = null;

        private ReorderableList _pointsList;

        private void OnEnable()
        {
            _path = target as CubicPath;
            _state = EditorState.LoadOrCreateFor(target);
            _loop = serializedObject.FindProperty("_loop");
            _points = serializedObject.FindProperty("_points");
            _handlesIcon = EditorGUIUtility.IconContent(SHOW_HANDLES_ICON).image;

            _separatorLineStyle = new GUIStyle();
            _separatorLineStyle.normal.background = EditorGUIUtility.whiteTexture;
            _separatorLineStyle.margin = new RectOffset( 0, 0, 4, 4 );
            _separatorLineStyle.fixedHeight = 1;

            if (!_state.PropertyExists<int>(SUBS_KEY)) _state.SetProperty(SUBS_KEY, MIN_SUBDIVISIONS);
            if (!_state.PropertyExists<bool>(EDIT_INTERP_KEY)) _state.SetProperty(EDIT_INTERP_KEY, false);
            if (!_state.PropertyExists<bool>(SHOW_HANDLES_KEY)) _state.SetProperty(SHOW_HANDLES_KEY, false);
            if (!_state.PropertyExists<bool>(POINTS_COLLAPSED_KEY)) _state.SetProperty(POINTS_COLLAPSED_KEY, false);
            if (!_state.PropertyExists<Space>(COORDS_SYSTEM_KEY)) _state.SetProperty(COORDS_SYSTEM_KEY, Space.Self);

            PreparePointsList();
        }

        private void PreparePointsList()
        {
            _pointsList = new ReorderableList(serializedObject, _points, true, false, true, true);
            _pointsList.onAddCallback = list => 
            {
                Vector3 point = _path.Count > 1 ? 
                        GetPassPointAt(list.serializedProperty.arraySize - 1).P : 
                        Vector3.zero;
                _path.AddPassPoint(point, Space.Self);
            };
            _pointsList.elementHeightCallback = index => 
            {
                int widthExtraLines = 3 * System.Convert.ToInt32(Screen.width < MIN_SINGLE_LINE_SCREEN_WIDTH);
                return POINTS_LIST_SPACING + 
                        EditorGUIUtility.singleLineHeight * 
                        (4 + widthExtraLines);
            };
            _pointsList.drawElementCallback = OnDrawPassPoint;
        }

        private void OnDrawPassPoint(Rect rect, int index, bool isActive, bool isFocus)
        {
            var space = _state.GetProperty<Space>(COORDS_SYSTEM_KEY);

            var passPoint = _pointsList.serializedProperty.GetArrayElementAtIndex(index);

            int linesPerVec3Field = 1 + System.Convert.ToInt32(Screen.width < MIN_SINGLE_LINE_SCREEN_WIDTH);

            var pointRect = new Rect(rect);
            pointRect.height = EditorGUIUtility.singleLineHeight * linesPerVec3Field;
            pointRect.y += 2;
            EditorGUIUtility.labelWidth = POINTS_LABEL_WIDTH;
            Vector3 point = _path.GetPointAt(index, space);
            Vector3 newPoint = EditorGUI.Vector3Field(pointRect, new GUIContent($"Point {index}"), point);
            EditorGUIUtility.labelWidth = 0;
            if (point != newPoint)
                _path.ChangePointAt(index, newPoint, space);

            var lineRect = new Rect(pointRect);
            lineRect.height = 2;
            lineRect.y += pointRect.height + 1;
            GUI.Box(lineRect, GUIContent.none, _separatorLineStyle);

            var continuityRect = new Rect(lineRect);
            continuityRect.height = EditorGUIUtility.singleLineHeight;
            continuityRect.y += lineRect.height;
            var continuity = _path.GetContinuityOfPointAt(index);
            var newContinuity = (CubicPath.JointContinuity)EditorGUI.EnumPopup(continuityRect, "Continuity", continuity);
            if(continuity != newContinuity)
                _path.ChangeContinuityOfPointAt(index, newContinuity);

            EditorGUI.BeginDisabledGroup(!_state.GetProperty<bool>(EDIT_INTERP_KEY));
            {
                var interpolantRect = new Rect(continuityRect);
                interpolantRect.height = EditorGUIUtility.singleLineHeight * linesPerVec3Field;
                interpolantRect.y += continuityRect.height;

                EditorGUIUtility.labelWidth = POINTS_LABEL_WIDTH;
                Vector3 leftValue = _path.GetLeftInterpolantOf(index, space);
                Vector3 newLeftValue = EditorGUI.Vector3Field(interpolantRect, "L. Interp", leftValue);
                interpolantRect.y += interpolantRect.height;
                Vector3 rightValue = _path.GetRightInterpolantOf(index, space);
                Vector3 newRightValue = EditorGUI.Vector3Field(interpolantRect, "R. Interp", _path.GetRightInterpolantOf(index, space));
                EditorGUIUtility.labelWidth = 0;
                if (leftValue != newLeftValue)
                    _path.ChangeLeftInterpolantOf(index, newLeftValue, space);
                if (rightValue != newRightValue)
                    _path.ChangeRightInterpolantOf(index, newRightValue, space);
            }
            EditorGUI.EndDisabledGroup();
        }

        private (Vector3 L, Vector3 P, Vector3 R) GetPassPointAt(int i, Space space = Space.World) =>
            (_path.GetLeftInterpolantOf(i, space), _path.GetPointAt(i, space), _path.GetRightInterpolantOf(i, space));

        private void HandleInputs()
        {
            EditorGUI.BeginChangeCheck();
            Vector3 step = MOVEMENT_STEP * Vector3.one;
            for (int i = 0; i < _points.arraySize; i++)
            {
                var passPoint = GetPassPointAt(i);

                Handles.color = Color.green;
                float size = HandleUtility.GetHandleSize(passPoint.L) * HANDLE_SIZE_FACTOR;
                Vector3 newPoint = Handles.FreeMoveHandle(passPoint.L, _path.transform.rotation, size, step, Handles.SphereHandleCap);
                if (newPoint != passPoint.L) _path.ChangeLeftInterpolantOf(i, newPoint);

                Handles.color = Color.red;
                size = HandleUtility.GetHandleSize(passPoint.R) * HANDLE_SIZE_FACTOR;
                newPoint = Handles.FreeMoveHandle(passPoint.R, _path.transform.rotation, size, step, Handles.SphereHandleCap);
                if (newPoint != passPoint.R) _path.ChangeRightInterpolantOf(i, newPoint);

                Handles.color = Color.white;
                size = HandleUtility.GetHandleSize(passPoint.P) * HANDLE_SIZE_FACTOR;
                newPoint = Handles.FreeMoveHandle(passPoint.P, _path.transform.rotation, size, step, Handles.SphereHandleCap);
                if (newPoint != passPoint.P) _path.ChangePointAt(i, newPoint);
            }
            if (EditorGUI.EndChangeCheck()) Undo.RecordObject(_path, "Path Change");
        }

        private void DrawCurve()
        {
            if (_path.Count == 0) return;
            int subs = _state.GetProperty<int>(SUBS_KEY);

            Vector3 lineStart = Vector3.zero;
            Vector3[] normals = _path.GetNormalsForSubdivision(subs);
            for (int i = 0; i <= subs; i++) {
                float t = i / (float)subs;
                Vector3 lineEnd = _path.Evaluate(t);

                if (i != subs)
                {
                    Vector3 direction = _path.GetDirectionAt(t);
                    Handles.color = Color.blue;
                    Handles.DrawLine(lineEnd, lineEnd + normals[i] * .05f - direction * .05f);
                    Handles.DrawLine(lineEnd, lineEnd - normals[i] * .05f - direction * .05f);
                }

                if (i != 0)
                {
                    Handles.color = Color.white;
                    Handles.DrawLine(lineStart, lineEnd, 2);
                }
                lineStart = lineEnd;
            }

            for (int i = 0; i < _points.arraySize && _state.GetProperty<bool>(SHOW_HANDLES_KEY); i++) 
            {
                var passPoint = GetPassPointAt(i);
                Handles.color = Color.red;
                Handles.DrawLine(passPoint.P, passPoint.R, 2);
                Handles.color = Color.green;
                Handles.DrawLine(passPoint.P, passPoint.L, 2);
            }
        }

        private void OnSceneGUI () 
        {            
            DrawCurve();
            if (_state.GetProperty<bool>(SHOW_HANDLES_KEY)) 
                HandleInputs();
        }

        private void DrawEditorParameters()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            bool showHandles = _state.GetProperty<bool>(SHOW_HANDLES_KEY);
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool newHandles = GUILayout.Toggle(showHandles, new GUIContent("Toggle Handles", _handlesIcon), "Button", GUILayout.Height(25));
            if (showHandles != newHandles) 
            {
                _state.SetProperty(SHOW_HANDLES_KEY, newHandles);
                SceneView.RepaintAll();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            int subs = _state.GetProperty<int>(SUBS_KEY);
            int newSubs = EditorGUILayout.IntSlider("Editor Subdivisions", subs, MIN_SUBDIVISIONS, MAX_SUBDIVISIONS);
            if (newSubs != subs) _state.SetProperty<int>(SUBS_KEY, newSubs);

            Space space = _state.GetProperty<Space>(COORDS_SYSTEM_KEY);
            Space newSpace = (Space)EditorGUILayout.EnumPopup("Space", space);
            if (newSpace != space) _state.SetProperty<Space>(COORDS_SYSTEM_KEY, newSpace);

            bool editInterp = _state.GetProperty<bool>(EDIT_INTERP_KEY);
            bool newEditInterp = EditorGUILayout.Toggle("Edit Interpolants", editInterp);
            if (newEditInterp != editInterp) _state.SetProperty<bool>(EDIT_INTERP_KEY, newEditInterp);

            EditorGUILayout.EndVertical();
        }

        public override void OnInspectorGUI()
        {
            DrawEditorParameters();

            EditorGUILayout.Space();

            serializedObject.Update();
            bool loop = EditorGUILayout.Toggle("Loop Curve", _path.Loop);

            if (loop != _path.Loop) _path.Loop = loop;

            bool foldout = _state.GetProperty<bool>(POINTS_COLLAPSED_KEY);
            bool newFoldout = EditorGUILayout.Foldout(foldout, "Points");
            if (foldout != newFoldout) _state.SetProperty<bool>(POINTS_COLLAPSED_KEY, newFoldout);
            if(newFoldout) _pointsList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
            SceneView.RepaintAll();
        }
    }
}
