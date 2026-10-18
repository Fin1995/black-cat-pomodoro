using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace BlackCatPomodoro
{
    public enum SoundMode
    {
        SystemBeep,
        CustomFile,
        Silent
    }

    public sealed class AudioDevice
    {
        public int Index { get; set; }
        public string Name { get; set; }

        public override string ToString() => Name ?? string.Empty;
    }

    /// <summary>
    /// Audio playback with a runtime fallback to the system notification sound.
    ///
    /// NAudio is a build-time dependency. If its assemblies are missing at
    /// runtime, the probe fails and the service keeps working with system sounds.
    /// </summary>
    public sealed class AudioService : IDisposable
    {
        private bool _naudioAvailable;
        private IDisposable _player;
        private IDisposable _reader;

        public AudioService()
        {
            _naudioAvailable = TryProbeNAudio();
        }

        private static bool TryProbeNAudio()
        {
            try
            {
                ProbeNAudio();
                return true;
            }
            catch
            {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ProbeNAudio()
        {
            var _ = typeof(NAudio.Wave.WaveOutEvent);
        }

        public static List<AudioDevice> GetDevices()
        {
            var devices = new List<AudioDevice>();
            try
            {
                GetDevicesNAudio(devices);
            }
            catch
            {
                // NAudio is unavailable; the caller will use the system default.
            }
            return devices;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void GetDevicesNAudio(List<AudioDevice> devices)
        {
            for (int i = 0; i < NAudio.Wave.WaveOut.DeviceCount; i++)
            {
                var caps = NAudio.Wave.WaveOut.GetCapabilities(i);
                devices.Add(new AudioDevice
                {
                    Index = i,
                    Name = string.IsNullOrWhiteSpace(caps.ProductName) ? "Audio device " + (i + 1) : caps.ProductName
                });
            }
        }

        public void Play(string filePath, int deviceIndex = -1)
        {
            Stop();

            if (!_naudioAvailable || string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                PlaySystemBeep();
                return;
            }

            try
            {
                PlayNAudio(filePath, deviceIndex);
            }
            catch
            {
                PlaySystemBeep();
            }
        }

        /// <summary>
        /// Open an audio file by content. AudioFileReader chooses its reader
        /// from the extension, so a mislabeled file (for example an MP3 named
        /// .wav) falls back to Mp3FileReader and then Media Foundation.
        /// </summary>
        public static NAudio.Wave.WaveStream CreateReader(string path)
        {
            try
            {
                return new NAudio.Wave.AudioFileReader(path);
            }
            catch
            {
                // Try the actual content below.
            }

            try
            {
                return new NAudio.Wave.Mp3FileReader(path);
            }
            catch
            {
                // Try Media Foundation for other container formats.
            }

            return new NAudio.Wave.MediaFoundationReader(path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PlayNAudio(string path, int deviceIndex)
        {
            var reader = CreateReader(path);
            NAudio.Wave.IWavePlayer player = null;

            try
            {
                player = (deviceIndex >= 0 && deviceIndex < NAudio.Wave.WaveOut.DeviceCount)
                    ? new NAudio.Wave.WaveOutEvent { DeviceNumber = deviceIndex }
                    : new NAudio.Wave.WaveOutEvent();

                player.Init(reader);
                player.Play();

                _reader = reader;
                _player = player;
            }
            catch
            {
                player?.Dispose();
                reader.Dispose();
                throw;
            }
        }

        public static void PlaySystemBeep()
        {
            try { System.Media.SystemSounds.Exclamation.Play(); return; } catch { }
            try { System.Media.SystemSounds.Asterisk.Play(); return; } catch { }
            try { System.Media.SystemSounds.Beep.Play(); return; } catch { }
            try { Console.Beep(800, 300); } catch { }
        }

        public void Stop()
        {
            try
            {
                if (_player != null)
                {
                    var stopMethod = _player.GetType().GetMethod("Stop");
                    stopMethod?.Invoke(_player, null);
                }
            }
            catch
            {
                // Ignore stop errors.
            }

            _player?.Dispose();
            _player = null;

            _reader?.Dispose();
            _reader = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
