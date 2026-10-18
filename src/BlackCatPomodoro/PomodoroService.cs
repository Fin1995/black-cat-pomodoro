using System;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Pomodoro phase.
    /// </summary>
    public enum PomodoroPhase
    {
        Idle,
        Focusing,
        Resting
    }

    /// <summary>
    /// Wall-clock based Pomodoro state machine.
    ///
    /// The service does not own a timer. The UI calls <see cref="Update"/> on a
    /// short interval; the service compares the current UTC time with the phase
    /// deadline. This keeps the timer accurate when the UI is busy or the
    /// machine has been asleep.
    /// </summary>
    public sealed class PomodoroService
    {
        private DateTime _phaseEndUtc;
        private int _pausedRemainingSeconds;
        private int _roundIndex;
        private int _totalSecondsThisPhase;

        public PomodoroPhase Phase { get; private set; } = PomodoroPhase.Idle;

        public bool IsPaused { get; private set; }

        public bool IsRunning => Phase != PomodoroPhase.Idle;

        public int RemainingSeconds { get; private set; }

        public int CurrentRound => _roundIndex + 1;

        public int TotalRounds { get; private set; }

        public PomodoroTask CurrentTask { get; private set; }

        /// <summary>Raised when the displayed second changes: (remaining, phase total).</summary>
        public event Action<int, int> Tick;

        /// <summary>Raised when the phase, pause state or running state changes.</summary>
        public event Action PhaseChanged;

        /// <summary>Raised when every round of the task has completed.</summary>
        public event Action CycleCompleted;

        public void Start(PomodoroTask task, DateTime nowUtc)
        {
            if (task == null) throw new ArgumentNullException(nameof(task));
            if (task.FocusMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(task), "Focus minutes must be positive.");
            if (task.RestMinutes < 0) throw new ArgumentOutOfRangeException(nameof(task), "Rest minutes cannot be negative.");
            if (task.RepeatCount <= 0) throw new ArgumentOutOfRangeException(nameof(task), "Repeat count must be positive.");

            CurrentTask = task.Clone();
            TotalRounds = task.RepeatCount;
            _roundIndex = 0;
            IsPaused = false;
            SwitchToFocus(nowUtc);

            PhaseChanged?.Invoke();
            Tick?.Invoke(RemainingSeconds, _totalSecondsThisPhase);
        }

        public void Pause(DateTime nowUtc)
        {
            if (!IsRunning || IsPaused) return;

            _pausedRemainingSeconds = Math.Max(0, (int)Math.Ceiling((_phaseEndUtc - nowUtc).TotalSeconds));
            RemainingSeconds = _pausedRemainingSeconds;
            IsPaused = true;
            PhaseChanged?.Invoke();
        }

        public void Resume(DateTime nowUtc)
        {
            if (!IsRunning || !IsPaused) return;

            _phaseEndUtc = nowUtc.AddSeconds(_pausedRemainingSeconds);
            IsPaused = false;
            PhaseChanged?.Invoke();
        }

        public void TogglePause(DateTime nowUtc)
        {
            if (IsPaused) Resume(nowUtc);
            else Pause(nowUtc);
        }

        public void Skip(DateTime nowUtc)
        {
            if (!IsRunning) return;

            IsPaused = false;
            _phaseEndUtc = nowUtc;
            Update(nowUtc);
        }

        public void Stop()
        {
            if (!IsRunning) return;

            ResetToIdle();
            PhaseChanged?.Invoke();
        }

        /// <summary>
        /// Recalculate the remaining time and advance through any phases that
        /// have elapsed since the previous call.
        /// </summary>
        public void Update(DateTime nowUtc)
        {
            if (!IsRunning || IsPaused) return;

            // If the machine slept or the UI was blocked, catch up through all
            // elapsed phases. The guard prevents a pathological infinite loop.
            int guard = 0;
            while (IsRunning && !IsPaused && nowUtc >= _phaseEndUtc && guard++ < 1000)
            {
                AdvancePhase();
            }

            if (!IsRunning || IsPaused) return;

            int remaining = (int)Math.Ceiling((_phaseEndUtc - nowUtc).TotalSeconds);
            if (remaining < 0) remaining = 0;

            if (remaining != RemainingSeconds)
            {
                RemainingSeconds = remaining;
                Tick?.Invoke(RemainingSeconds, _totalSecondsThisPhase);
            }
        }

        private void AdvancePhase()
        {
            DateTime transitionTime = _phaseEndUtc;

            if (Phase == PomodoroPhase.Focusing)
            {
                if (CurrentTask.RestMinutes > 0)
                {
                    Phase = PomodoroPhase.Resting;
                    _totalSecondsThisPhase = CurrentTask.RestMinutes * 60;
                    RemainingSeconds = _totalSecondsThisPhase;
                    _phaseEndUtc = transitionTime.AddSeconds(_totalSecondsThisPhase);
                    PhaseChanged?.Invoke();
                }
                else
                {
                    CompleteRound(transitionTime);
                }
            }
            else if (Phase == PomodoroPhase.Resting)
            {
                CompleteRound(transitionTime);
            }
        }

        private void CompleteRound(DateTime transitionTime)
        {
            _roundIndex++;

            if (_roundIndex < TotalRounds)
            {
                Phase = PomodoroPhase.Focusing;
                _totalSecondsThisPhase = CurrentTask.FocusMinutes * 60;
                RemainingSeconds = _totalSecondsThisPhase;
                _phaseEndUtc = transitionTime.AddSeconds(_totalSecondsThisPhase);
                PhaseChanged?.Invoke();
            }
            else
            {
                ResetToIdle();
                CycleCompleted?.Invoke();
                PhaseChanged?.Invoke();
            }
        }

        private void SwitchToFocus(DateTime nowUtc)
        {
            Phase = PomodoroPhase.Focusing;
            _totalSecondsThisPhase = CurrentTask.FocusMinutes * 60;
            RemainingSeconds = _totalSecondsThisPhase;
            _phaseEndUtc = nowUtc.AddSeconds(_totalSecondsThisPhase);
        }

        private void ResetToIdle()
        {
            Phase = PomodoroPhase.Idle;
            IsPaused = false;
            RemainingSeconds = 0;
            _totalSecondsThisPhase = 0;
            _phaseEndUtc = DateTime.MinValue;
            _pausedRemainingSeconds = 0;
        }
    }
}
