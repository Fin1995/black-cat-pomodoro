using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BlackCatPomodoro.Tests
{
    public class DataServiceTests : IDisposable
    {
        private readonly string _folder;

        public DataServiceTests()
        {
            Localization.SetLanguage(AppLanguage.English);

            _folder = Path.Combine(
                Path.GetTempPath(),
                "BlackCatPomodoroTests",
                Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_folder);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_folder, recursive: true);
            }
            catch
            {
                // Best-effort test cleanup.
            }
        }

        [Fact]
        public void Load_MissingFile_ReturnsDefaultTask()
        {
            var service = new DataService(_folder);

            var result = service.Load();

            Assert.NotNull(result.Tasks);
            Assert.Single(result.Tasks);
            Assert.Contains("Sample", result.Tasks[0].Name);
            Assert.Null(result.Error);
        }

        [Fact]
        public void SaveAndLoad_RoundTripsTasks()
        {
            var service = new DataService(_folder);
            var tasks = new List<PomodoroTask>
            {
                new PomodoroTask
                {
                    Name = "Write tests",
                    FocusMinutes = 50,
                    RestMinutes = 10,
                    RepeatCount = 2,
                    Notes = "note"
                }
            };

            Assert.True(service.Save(tasks));

            var result = service.Load();

            Assert.Null(result.Error);
            Assert.Single(result.Tasks);
            Assert.Equal("Write tests", result.Tasks[0].Name);
            Assert.Equal(50, result.Tasks[0].FocusMinutes);
            Assert.Equal(10, result.Tasks[0].RestMinutes);
            Assert.Equal(2, result.Tasks[0].RepeatCount);
            Assert.Equal("note", result.Tasks[0].Notes);
        }

        [Fact]
        public void Save_SecondTime_CreatesBackup()
        {
            var service = new DataService(_folder);

            Assert.True(service.Save(new List<PomodoroTask>
            {
                new PomodoroTask { Name = "First" }
            }));

            Assert.True(service.Save(new List<PomodoroTask>
            {
                new PomodoroTask { Name = "Second" }
            }));

            Assert.True(File.Exists(service.BackupFilePath));

            var result = service.Load();
            Assert.Single(result.Tasks);
            Assert.Equal("Second", result.Tasks[0].Name);
        }

        [Fact]
        public void Load_CorruptFile_BacksUpAndReturnsError()
        {
            var service = new DataService(_folder);
            File.WriteAllText(service.DataFilePath, "not xml");

            var result = service.Load();

            Assert.NotNull(result.Error);
            Assert.Empty(result.Tasks);
            Assert.False(string.IsNullOrEmpty(result.BackupPath));
            Assert.True(File.Exists(result.BackupPath));
        }
    }
}
