using System;
using Xunit;

namespace BlackCatPomodoro.Tests
{
    public class PomodoroServiceTests
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static PomodoroTask CreateTask(int focus = 25, int rest = 5, int repeat = 1)
        {
            return new PomodoroTask
            {
                Name = "Test task",
                FocusMinutes = focus,
                RestMinutes = rest,
                RepeatCount = repeat
            };
        }

        [Fact]
        public void Start_SetsFocusingAndInitialRemaining()
        {
            var service = new PomodoroService();

            service.Start(CreateTask(), T0);

            Assert.Equal(PomodoroPhase.Focusing, service.Phase);
            Assert.True(service.IsRunning);
            Assert.False(service.IsPaused);
            Assert.Equal(25 * 60, service.RemainingSeconds);
            Assert.Equal(1, service.CurrentRound);
            Assert.Equal(1, service.TotalRounds);
        }

        [Fact]
        public void Update_AfterFocus_TransitionsToRest()
        {
            var service = new PomodoroService();
            service.Start(CreateTask(), T0);

            service.Update(T0.AddMinutes(25));

            Assert.Equal(PomodoroPhase.Resting, service.Phase);
            Assert.Equal(5 * 60, service.RemainingSeconds);
        }

        [Fact]
        public void Update_AfterRest_StartsNextRound()
        {
            var service = new PomodoroService();
            service.Start(CreateTask(repeat: 2), T0);

            service.Update(T0.AddMinutes(30));

            Assert.Equal(PomodoroPhase.Focusing, service.Phase);
            Assert.Equal(2, service.CurrentRound);
            Assert.Equal(25 * 60, service.RemainingSeconds);
        }

        [Fact]
        public void Update_AfterLastRest_CompletesCycle()
        {
            var service = new PomodoroService();
            int completed = 0;
            service.CycleCompleted += () => completed++;

            service.Start(CreateTask(), T0);
            service.Update(T0.AddMinutes(30));

            Assert.Equal(PomodoroPhase.Idle, service.Phase);
            Assert.False(service.IsRunning);
            Assert.Equal(1, completed);
        }

        [Fact]
        public void PauseAndResume_DoNotAdvanceWhilePaused()
        {
            var service = new PomodoroService();
            service.Start(CreateTask(), T0);

            service.Pause(T0.AddSeconds(10));
            Assert.True(service.IsPaused);
            Assert.Equal(1490, service.RemainingSeconds);

            service.Update(T0.AddMinutes(10));
            Assert.Equal(1490, service.RemainingSeconds);

            service.Resume(T0.AddMinutes(10));
            Assert.False(service.IsPaused);

            service.Update(T0.AddMinutes(10).AddSeconds(10));
            Assert.Equal(1480, service.RemainingSeconds);
        }

        [Fact]
        public void Skip_TransitionsImmediately()
        {
            var service = new PomodoroService();
            service.Start(CreateTask(), T0);

            service.Skip(T0.AddSeconds(5));

            Assert.Equal(PomodoroPhase.Resting, service.Phase);
            Assert.Equal(5 * 60, service.RemainingSeconds);
        }

        [Fact]
        public void ZeroRest_SkipsRestPhase()
        {
            var service = new PomodoroService();
            service.Start(CreateTask(rest: 0, repeat: 2), T0);

            service.Update(T0.AddMinutes(25));

            Assert.Equal(PomodoroPhase.Focusing, service.Phase);
            Assert.Equal(2, service.CurrentRound);
        }

        [Fact]
        public void SleepCatchUp_CompletesElapsedCycle()
        {
            var service = new PomodoroService();
            int completed = 0;
            service.CycleCompleted += () => completed++;

            service.Start(CreateTask(focus: 1, rest: 1), T0);
            service.Update(T0.AddHours(2));

            Assert.Equal(PomodoroPhase.Idle, service.Phase);
            Assert.Equal(1, completed);
        }

        [Theory]
        [InlineData(0, 5, 1)]
        [InlineData(25, -1, 1)]
        [InlineData(25, 5, 0)]
        public void Start_InvalidTask_Throws(int focus, int rest, int repeat)
        {
            var service = new PomodoroService();
            var task = CreateTask(focus, rest, repeat);

            Assert.Throws<ArgumentOutOfRangeException>(() => service.Start(task, T0));
        }
    }
}
