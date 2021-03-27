using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rebar.Unity
{
    public class CubicPath : MonoBehaviour, IEnumerable<Vector3>
    {
        [System.Serializable]
        private struct PassPoint 
        {
            [SerializeField]
            private Vector3 _leftInterpolant;
            [SerializeField]
            private Vector3 _point;
            [SerializeField]
            private Vector3 _rightInterpolant;
            [SerializeField]
            private JointContinuity _continuity;

            public Vector3 Point => _point;

            public Vector3 LeftInterpolant => _leftInterpolant;

            public Vector3 RightInterpolant => _rightInterpolant;

            public JointContinuity Continuity => _continuity;

            private PassPoint(PassPoint point)
            {
                _point = point.Point;
                _leftInterpolant = point.LeftInterpolant;
                _rightInterpolant = point.RightInterpolant;
                _continuity = point.Continuity;
            }

            public PassPoint(Vector3 point, JointContinuity continuity) 
            {
                _point = point;
                _leftInterpolant = point + .1f * Vector3.right;
                _rightInterpolant = point + .1f * Vector3.left;
                _continuity = continuity;
                AdjustPointsForContinuity(ref _leftInterpolant, ref _rightInterpolant);
            }

            public PassPoint(Vector3 point, Vector3 leftInterpolant, bool isC2)
            {
                _point = point;
                _leftInterpolant = leftInterpolant;
                _rightInterpolant = Vector3.zero;
                _continuity = (JointContinuity)(1 + System.Convert.ToInt32(isC2));
                AdjustPointsForContinuity(ref _leftInterpolant, ref _rightInterpolant);
            }

            public PassPoint(Vector3 point, Vector3 leftInterpolant, Vector3 rightInterpolant)
            {
                _point = point;
                _leftInterpolant = leftInterpolant;
                _rightInterpolant = rightInterpolant;
                _continuity = 0;
            }

            private void AdjustPointsForContinuity(ref Vector3 reference, ref Vector3 toChange)
            {
                if (_continuity != JointContinuity.C0)
                {
                    Vector3 distanceReference = _continuity == JointContinuity.C2 ? reference : toChange;
                    float distance = Vector3.Distance(_point, distanceReference);
                    Vector3 direction = (_point - reference).normalized;
                    toChange = _point + direction * distance;
                }
            }

            public PassPoint WithRightInterpolant(Vector3 value)
            {

                _rightInterpolant = value;
                AdjustPointsForContinuity(ref _rightInterpolant, ref _leftInterpolant);
                return new PassPoint(this);
            }

            public PassPoint WithLeftInterpolant(Vector3 value)
            {

                _leftInterpolant = value;
                AdjustPointsForContinuity(ref _leftInterpolant, ref _rightInterpolant);
                return new PassPoint(this);
            }

            public PassPoint WithValue(Vector3 value)
            {
                Vector3 offset = value - _point;
                _point = value;
                _leftInterpolant += offset;
                _rightInterpolant += offset;
                return new PassPoint(this);
            }

            public PassPoint WithContinuity(JointContinuity value)
            {
                _continuity = value;
                AdjustPointsForContinuity(ref _leftInterpolant, ref _rightInterpolant);
                return new PassPoint(this);
            }
        }

        public enum JointContinuity : byte { C0, C1, C2 }

        [SerializeField]
        private bool _loop = false;
        [SerializeField]
        private List<PassPoint> _points = new List<PassPoint>();

        public Vector3 this[float t] => Evaluate(t);

        public Vector3 this[int index] => GetPointAt(index);

        public int Count => _points.Count;

        public bool Loop 
        {
            get => _loop;
            set => _loop = value;
        }

        private void IndexCheckOrException(int index)
        {
            if (index < 0 || index >= _points.Count) 
                throw new System.IndexOutOfRangeException($"Expected index to be non-negative and less than {_points.Count}, but was {index}");
        }

        private (int Start, int End, float T) ShiftT(float t)
        {
            t = Mathf.Clamp01(t);

            int loopCurve = System.Convert.ToInt32(!_loop);
            float fractionPerCurve = 1f / (_points.Count - loopCurve);
            int startIndex =  Mathf.Clamp((int)(t / fractionPerCurve), 0, _points.Count - 1);
            int endIndex = startIndex + 1;
            if (endIndex >= _points.Count) endIndex = 0;

            t = (t - fractionPerCurve * startIndex) / fractionPerCurve;

            return (startIndex, endIndex, t);
        }

        public void Setup(CubicPath path)
        {
            _points = path._points.ToList();
            _loop = path._loop;
        }

        public void Setup(IEnumerable<Vector3> passPoints, JointContinuity continuityAtPoints, bool loop, Space coordinatesSystem = Space.World)
        {
            if (coordinatesSystem == Space.World) passPoints = passPoints.Select(p => transform.InverseTransformPoint(p));
            _points = passPoints.Select(p => new PassPoint(p, continuityAtPoints)).ToList();
            _loop = loop;
        }

        public void Setup(IEnumerable<Vector3> passPoints, IEnumerable<JointContinuity> continuities, bool loop, Space coordinatesSystem = Space.World)
        {
            if (coordinatesSystem == Space.World) passPoints = passPoints.Select(p => transform.InverseTransformPoint(p));
            _points = passPoints.Zip(continuities, (v, c) => new PassPoint(v, c)).ToList();
            _loop = loop;
        }

        public void Clear() => _points.Clear();

        public void AddPassPoint(Vector3 value, Space coordinatesSystem = Space.World) 
        {
            if (coordinatesSystem == Space.World) value = transform.InverseTransformPoint(value);

            _points.Add(new PassPoint(value, JointContinuity.C1));
        }
        
        public void ChangePointAt(int index, Vector3 value, Space coordinatesSystem = Space.World) 
        {
            IndexCheckOrException(index);

            if (coordinatesSystem == Space.World) value = transform.InverseTransformPoint(value);

            _points[index] = _points[index].WithValue(value);
        }

        public Vector3 GetPointAt(int index, Space coordinatesSystem = Space.World) 
        {
            IndexCheckOrException(index);

            Vector3 point = _points[index].Point;
            if (coordinatesSystem == Space.World) 
                point = transform.TransformPoint(point);
            return point;
        }

        public JointContinuity GetContinuityOfPointAt(int index)
        {
            IndexCheckOrException(index);
            return _points[index].Continuity;
        }

        public void ChangeContinuityForAllPoints(JointContinuity continuity)
        {
            if(!Renum.Values<JointContinuity>().Contains(continuity))
                throw new System.InvalidCastException($"Invalid continuity value {continuity}");
            for(int i = 0; i < _points.Count; i++)
                _points[i] = _points[i].WithContinuity(continuity);
        }

        public void ChangeContinuityOfPointAt(int index, JointContinuity continuity) 
        {
            IndexCheckOrException(index);
            if(!Renum.Values<JointContinuity>().Contains(continuity))
                throw new System.InvalidCastException($"Invalid continuity value {continuity}");

            _points[index] = _points[index].WithContinuity(continuity);
        }

        public Vector3 GetLeftInterpolantOf(int index, Space coordinatesSystem = Space.World)
        {
            IndexCheckOrException(index);

            Vector3 interp = _points[index].LeftInterpolant;
            if (coordinatesSystem == Space.World) 
                interp = transform.TransformPoint(interp);
            return interp;
        }

        public void ChangeLeftInterpolantOf(int index, Vector3 value, Space coordinatesSystem = Space.World) 
        {
            IndexCheckOrException(index);

            if (coordinatesSystem == Space.World) value = transform.InverseTransformPoint(value);

            _points[index] = _points[index].WithLeftInterpolant(value);
        }

        public Vector3 GetRightInterpolantOf(int index, Space coordinatesSystem = Space.World)
        {
            IndexCheckOrException(index);

            Vector3 interp = _points[index].RightInterpolant;
            if (coordinatesSystem == Space.World) 
                interp = transform.TransformPoint(interp);
            return interp;
        }

        public void ChangeRightInterpolantOf(int index, Vector3 value, Space coordinatesSystem = Space.World) 
        {
            IndexCheckOrException(index);

            if (coordinatesSystem == Space.World) value = transform.InverseTransformPoint(value);

            _points[index] = _points[index].WithRightInterpolant(value);
        }

        public void RemovePointAt(int index) => _points.RemoveAt(index);

        public Vector3 Evaluate(float t, Space coordinatesSystem = Space.World)
        {
            (int startIndex, int endIndex, float shiftedT) = ShiftT(t);

            t = shiftedT;
            var tSquared = t * t;
            var oneMinusT = 1 - t;
            var oneMinusTSquared = oneMinusT * oneMinusT;

            Vector3 local = oneMinusT * oneMinusTSquared * _points[startIndex].Point +
                    3 * oneMinusTSquared * t * _points[startIndex].RightInterpolant +
                    3 * oneMinusT * tSquared * _points[endIndex].LeftInterpolant +
                    tSquared * t * _points[endIndex].Point;
            if (coordinatesSystem == Space.World) return transform.TransformPoint(local);
            return local;
        }

        public Vector3 GetVelocityAt(float t, Space coordinatesSystem = Space.World)
        {
            (int startIndex, int endIndex, float shiftedT) = ShiftT(t);

            t = shiftedT;
            var oneMinusT = 1 - t;

            Vector3[] p = new [] {
                _points[startIndex].Point,
                _points[startIndex].RightInterpolant,
                _points[endIndex].LeftInterpolant,
                _points[endIndex].Point
            };

            Vector3 local = 3 * oneMinusT * oneMinusT * (p[1] - p[0]) +
                    6 * oneMinusT * t * (p[2] - p[1]) + 
                    3 * t * t * (p[3] - p[2]);
            if (coordinatesSystem == Space.World) return transform.TransformPoint(local);
            return local;
        }

        public Vector3 GetDirectionAt(float t, Space coordinatesSystem = Space.World) => 
            GetVelocityAt(t, coordinatesSystem).normalized;

        public IEnumerator<Vector3> GetEnumerator() => 
            _points.Select(p => transform.TransformPoint(p.Point)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
