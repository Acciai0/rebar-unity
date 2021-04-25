using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Rebar.Unity
{
    public interface IProfile
    {
        bool Loop { get; set; }

        Vector3 Evaluate(float t, Space coordinatesSystem = Space.World);
    }
}
