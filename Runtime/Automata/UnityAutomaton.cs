using System;
using Rebar.Automata;
using Rebar.Events;
using Rebar.Unity.Events;
using UnityEngine;
using UnityEngine.Events;

namespace Rebar.Unity.Automata
{
    /// <summary>
    /// Unity Behaviour that wraps a Rebar.Automata.StateAutomaton.
    /// </summary>
    public class UnityAutomaton : MonoBehaviour
    {
        public enum TickTime
        {
            Update = 0,
            LateUpdate = 1,
            ExternalRequest = 2
        }

        [SerializeField]
        private bool _startTickingAtAwake = false;
        [SerializeField]
        private UnityState _entryState = null;
        [SerializeField]
        private bool _publishStateChangedEvents = false;
        [SerializeField]
        private UnityBoard _pubSubBoard = null;
        [SerializeField]
        private TickTime _tickTime = TickTime.Update;
        [SerializeField]
        private float _tickPeriod = 0;
        [SerializeField]
        private UnityEvent<string> _onStateChange = null;

        private StateAutomaton _automaton = null;
        private float tickTimer_ = 0;

        private StateAutomaton Automaton
        {
            get
            {
                if (_automaton == null) 
                {
                    _automaton = new StateAutomaton(_entryState, _publishStateChangedEvents);
                    if (_pubSubBoard != null)
                        _automaton.PublishStatesEventsIn(_pubSubBoard.Board);
                    _automaton.OnStateChange += s => _onStateChange.Invoke(s);
                }
                return _automaton;
            }
        }

        public IState EntryState
        {
            get => Automaton.EntryState;
            set
            {
                Automaton.EntryState = value;
                if (value is UnityState) _entryState = (UnityState)value;
            }
        }

        public IState CurrentState => Automaton.CurrentState;

        public bool PreventTicking { get; set; } = true;

        public TickTime TickAt 
        {
            get => _tickTime;
            set => _tickTime = value;
        }

        private void Awake() 
        {
            if (_startTickingAtAwake)
                StartTicking();
        }

        private void Update() => InternalTick(TickTime.Update);

        private void LateUpdate() => InternalTick(TickTime.LateUpdate);

        private void InternalTick(TickTime caller)
        {
            if (PreventTicking || _tickTime != caller) return;

            if (_tickTime == TickTime.ExternalRequest)
            {
                Automaton.Tick();
                return;
            }

            if (_tickPeriod != 0)
                tickTimer_ += Time.deltaTime;

            if (tickTimer_ >= _tickPeriod)
            {
                Automaton.Tick();
                tickTimer_ -= _tickPeriod;
            }
        }

        public void SetSequentialTransition(IState from, IState to) => 
                Automaton.SetSequentialTransition(from, to);

        public void AddGeneralTransition(IState to, Func<bool> predicate, int priority = 0) => 
                Automaton.AddGeneralTransition(to, predicate, priority);

        public void AddBackwardTransition(IState from, Func<bool> predicate, int priority = 0) => 
                Automaton.AddBackwardTransition(from, predicate, priority);

        public void AddConditionalTransition(IState from, IState to, Func<bool> predicate, int priority = 0) =>
                Automaton.AddConditionalTransition(from, to, predicate, priority);

        public void PublishStateEventsIn(PubSubBoard psb) 
        {
            if (_pubSubBoard != null && _pubSubBoard.Board != psb)
                _pubSubBoard = null;
            
            Automaton.PublishStatesEventsIn(psb);
        }

        public void PublishStateEventsIn(UnityBoard board) 
        {
            _pubSubBoard = board;
            Automaton.PublishStatesEventsIn(board.Board);
        }

        public void PublishStateEventsInGlobal()
        {
            _publishStateChangedEvents = true;
            _pubSubBoard = null;
            Automaton.PublishStatesEventsInGlobal();
        }

        public void StopPublishingStatesEvents()
        {
            _publishStateChangedEvents = false;
            _pubSubBoard = null;
            Automaton.StopPublishingStatesEvents();
        }

        public void Tick() => InternalTick(TickTime.ExternalRequest);

        /// <summary>
        /// It has the same effect of setting PreventTicking to false, but has a more meaningful 
        /// name when starting the automaton the first time. Consecutive calls to this method
        /// without setting PreventTicking to true prior have no effect.
        /// </summary>
        public void StartTicking()
        {
            if (!PreventTicking) 
            {
                Debug.LogWarning($"{name}({GetType().Name}) is already ticking (PreventTicking = false)");
                return;
            }
            PreventTicking = false;
        }

        public void Reset()
        {
            Automaton.Reset(); 
            PreventTicking = true;
        }
    }
}
