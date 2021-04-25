using System;
using UnityEngine;

namespace Rebar.Unity
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(CubicPath))]
    [ExecuteInEditMode]
    public class PathExtruder : MonoBehaviour
    {


        [SerializeField]
        private int _resolution = 4;
        [SerializeField]
        private int _subdivisions = 10;
        [SerializeField]
        private float _uniformScale = 10;

        private CubicPath _path;
        private MeshFilter _filter;

        private MeshFilter Filter => _filter ?? (_filter = GetComponent<MeshFilter>());

        public CubicPath Path => _path ?? (_path = GetComponent<CubicPath>());

        public int Resolution
        {
            get => _resolution;
            set 
            {
                _resolution = Mathf.Max(0, value);
                UpdateMesh();
            }
        }

        public int Subdivisions
        {
            get => _subdivisions;
            set 
            {
                _subdivisions = Mathf.Max(0, value);
                UpdateMesh();
            }
        }

        public float UniformScale
        {
            get => _uniformScale;
            set
            {
                _uniformScale = Mathf.Max(0, value);
                UpdateMesh();
            }
        }

        private void OnEnable()
        {
            UpdateMesh();
            Path.OnChange += OnPathChange;
        }

        private void OnDisable() 
        {
            Filter.sharedMesh.Clear();
            Path.OnChange -= OnPathChange;
        }

        private void OnPathChange(CubicPath path) => UpdateMesh();

        private (Vector3[] Profile, bool Loops) ComputeProfile()
        {
            IProfile profile = new Circle(UniformScale);
            //TODO use bezier instead if present

            var result = new Vector3[Resolution + Convert.ToInt32(!profile.Loop)];

            for (int i = 0; i < result.Length; i++)
                result[i] = profile.Evaluate(i / (float)(result.Length - 1), Space.Self);

            return (result, profile.Loop);
        }

        private Vector3[] TransformProfileAt(Vector3[] profile, float t, Vector3 right)
        {
            Vector3 forward = Path.GetDirectionAt(t, Space.Self);
            Vector3 up = Vector3.Cross(right, forward);
            Vector3 point = Path.Evaluate(t, Space.Self);

            Matrix4x4 changeMatrix = new Matrix4x4(
                (Vector4)right,
                (Vector4)up,
                (Vector4)forward,
                new Vector4(0, 0, 0, 1)
            );

            var result = new Vector3[profile.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = changeMatrix.MultiplyPoint(profile[i]) + point;

            return result;
        }

        private void LinkSections(int[] triangles, int start, int end, bool loops)
        {
            for(int i = 0; i < Resolution - System.Convert.ToInt32(!loops); i++)
            {
                int trianglesOffset = (start + i) * 6;
                int vertexOffset = (i + 1) % Resolution;
                triangles[trianglesOffset] = start + vertexOffset;
                triangles[trianglesOffset + 1] = start + i;
                triangles[trianglesOffset + 2] = end + i;
                triangles[trianglesOffset + 3] = end + i;
                triangles[trianglesOffset + 4] = end + vertexOffset;
                triangles[trianglesOffset + 5] = start + vertexOffset;
            }
        }

        public void UpdateMesh()
        {
            if (Filter.sharedMesh == null)
                Filter.sharedMesh = new Mesh();

            if (!enabled) 
            {
                Debug.LogWarning("Updating mesh of a disabled PathExtruder. Remember to re-enable it.");
                return;
            }

            Filter.sharedMesh.Clear();

            (Vector3[] profilePoints, bool loops) = ComputeProfile();

            int profilesNumber = Subdivisions + Convert.ToInt32(!Path.Loop);
            int profileVerticesNumber = Resolution + Convert.ToInt32(!loops);

            Vector3[] normals = Path.GetNormalsForSubdivision(profilesNumber, Space.Self);
            Vector3[] startProfile = TransformProfileAt(profilePoints, 0, normals[0]);


            int verticesNum = profilesNumber * profileVerticesNumber;
            var meshVertices = new Vector3[verticesNum];
            var triangles = new int[6 * verticesNum];

            //Filling vertices array
            for(int i = 0; i < profilesNumber; i++)
            {
                float t = i / (float)(profilesNumber - Convert.ToInt32(!Path.Loop));
                Vector3[] profile = TransformProfileAt(profilePoints, t, normals[i]);
                Array.Copy(profile, 0, meshVertices, i * profileVerticesNumber, profileVerticesNumber);
            }

            //Filling triangles indices array
            for(int i = 0; i < Subdivisions; i++) {
                int start = i * profileVerticesNumber;
                int end = (i + 1) % profilesNumber * profileVerticesNumber;
                LinkSections(triangles, start, end, loops);
            }

            Filter.sharedMesh.SetVertices(meshVertices);
            Filter.sharedMesh.SetTriangles(triangles, 0);
            Filter.sharedMesh.Optimize();
            Filter.sharedMesh.RecalculateNormals();
        }
    }
}
