using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BlackCatPomodoro
{
    /// <summary>
    /// Result of loading the task file. On failure, <see cref="Error"/> is set
    /// and <see cref="BackupPath"/> points to a copy of the unreadable file.
    /// </summary>
    public sealed class DataLoadResult
    {
        public List<PomodoroTask> Tasks { get; set; } = new List<PomodoroTask>();
        public string Error { get; set; }
        public string BackupPath { get; set; }
    }

    /// <summary>
    /// XML persistence for the task list.
    ///
    /// Saves are written to a temporary file and then swapped in with
    /// File.Replace, so a crash cannot leave a half-written tasks.xml.
    /// A corrupt file is copied aside before the user is asked to continue.
    /// </summary>
    public sealed class DataService
    {
        private const string DataFileName = "tasks.xml";
        private const string BackupFileName = "tasks.xml.bak";

        private readonly XmlSerializer _serializer = new XmlSerializer(typeof(TaskListData));

        public DataService(string dataFolder = null)
        {
            DataFolder = string.IsNullOrWhiteSpace(dataFolder)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlackCatPomodoro")
                : dataFolder;

            DataFilePath = Path.Combine(DataFolder, DataFileName);
            BackupFilePath = Path.Combine(DataFolder, BackupFileName);
        }

        public string DataFolder { get; }

        public string DataFilePath { get; }

        public string BackupFilePath { get; }

        public string LastError { get; private set; }

        /// <summary>
        /// Load the task list. A missing file returns the default sample task.
        /// An unreadable file is backed up and an empty list is returned.
        /// </summary>
        public DataLoadResult Load()
        {
            LastError = null;

            try
            {
                if (!File.Exists(DataFilePath))
                {
                    return new DataLoadResult { Tasks = CreateDefault() };
                }

                using (var stream = new FileStream(DataFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var data = (TaskListData)_serializer.Deserialize(stream);
                    return new DataLoadResult { Tasks = data.Tasks ?? new List<PomodoroTask>() };
                }
            }
            catch (Exception ex)
            {
                LastError = ex.Message;

                string backupPath = null;
                try
                {
                    if (File.Exists(DataFilePath))
                    {
                        backupPath = Path.Combine(
                            DataFolder,
                            "tasks.corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".xml");
                        File.Copy(DataFilePath, backupPath, overwrite: true);
                    }
                }
                catch
                {
                    // The load error is the important one; a failed backup is
                    // reported by the absence of BackupPath.
                }

                return new DataLoadResult
                {
                    Tasks = new List<PomodoroTask>(),
                    Error = ex.Message,
                    BackupPath = backupPath
                };
            }
        }

        /// <summary>
        /// Save the task list atomically. Returns false and sets LastError on failure.
        /// </summary>
        public bool Save(List<PomodoroTask> tasks)
        {
            LastError = null;

            if (tasks == null) throw new ArgumentNullException(nameof(tasks));

            string tempPath = DataFilePath + ".tmp";

            try
            {
                Directory.CreateDirectory(DataFolder);

                var data = new TaskListData { Tasks = tasks };
                using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    _serializer.Serialize(stream, data);
                    stream.Flush(true);
                }

                if (File.Exists(DataFilePath))
                {
                    try
                    {
                        File.Replace(tempPath, DataFilePath, BackupFilePath, ignoreMetadataErrors: true);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Very old or non-NTFS volumes: fall back to a copy.
                        File.Copy(tempPath, DataFilePath, overwrite: true);
                        File.Delete(tempPath);
                    }
                }
                else
                {
                    File.Move(tempPath, DataFilePath);
                }

                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                try
                {
                    if (File.Exists(tempPath)) File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors.
                }
                return false;
            }
        }

        private static List<PomodoroTask> CreateDefault()
        {
            return new List<PomodoroTask>
            {
                new PomodoroTask
                {
                    Name = Localization.T("SampleTaskName"),
                    FocusMinutes = 25,
                    RestMinutes = 5,
                    RepeatCount = 4,
                    Notes = Localization.T("SampleTaskNotes")
                }
            };
        }
    }
}
