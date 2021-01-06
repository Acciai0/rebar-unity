using System;
using Rebar.Events;
using UnityEngine;

namespace Rebar.Unity.Events
{
    /// <summary>
    /// Unity Behaviour that wraps a Rebar.Events.PubSubBoard.
    /// </summary>
    public class UnityBoard : MonoBehaviour 
    {
        private PubSubBoard _board;

        internal PubSubBoard Board => _board ?? (_board = new PubSubBoard());

        public bool IsActive 
        {
            get => enabled;
            set => enabled = value;
        }

        public void Subscribe<T>(string eventName, Action<T> subscription)
        {
            Board.Subscribe<T>(eventName, subscription);
        }
        
        public void Subscribe(string eventName, Action subscription)
        {
            Board.Subscribe(eventName, subscription);
        }

        public void Trigger<T>(string eventName, T args)
        {
            if (!enabled) return;
            Board.Trigger<T>(eventName, args);
        }

        public void Trigger(string eventName)
        {
            if (!enabled) return;
            Board.Trigger(eventName);
        }

        public bool Unsubscribe<T>(string eventName, Action<T> subscription) => 
                Board.Unsubscribe<T>(eventName, subscription);

        public void Unsubscribe(string eventName, Action subscription) => 
                Board.Unsubscribe(eventName, subscription);
    }
}
