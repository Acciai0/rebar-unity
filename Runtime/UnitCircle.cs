using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rebar.Unity
{
    public struct Circle : IProfile
    {
        private static readonly Circle Unit = new Circle(1);

        public static Vector3 Evaluate(float t) => Unit.Evaluate(t);

        private readonly float _radius;

        public Circle(float radius) => _radius = radius;

        public bool Loop { get => true; set { } }

        public Vector3 Evaluate(float t, Space coordinatesSystem = Space.World)
        {
            float angle = 2 * Mathf.Clamp01(t) * Mathf.PI;
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * _radius;
        }
    }
}
