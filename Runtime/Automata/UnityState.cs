using System.Collections;
using System.Collections.Generic;
using Rebar.Automata;
using UnityEngine;

namespace Rebar.Unity.Automata
{
    public abstract class UnityState : MonoBehaviour, IState
    {
        public virtual string Name => gameObject.name;
        public virtual bool IsActive 
        { 
            get => gameObject.activeSelf; 
            set => gameObject.SetActive(value); 
        }

        public abstract bool HasEnded { get; }

        public abstract void OnEnter();

        public abstract void OnExit();

        public abstract void OnTick();
    }
}
