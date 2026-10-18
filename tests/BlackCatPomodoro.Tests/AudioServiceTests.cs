using System;
using System.IO;
using Xunit;

namespace BlackCatPomodoro.Tests
{
    public class AudioServiceTests
    {
        [Fact]
        public void CreateReader_ReadsWavFile()
        {
            string path = Path.Combine(
                Path.GetTempPath(),
                "BlackCatPomodoro-" + Guid.NewGuid().ToString("N") + ".wav");

            WriteSilentWav(path);

            try
            {
                using (var reader = AudioService.CreateReader(path))
                {
                    Assert.NotNull(reader);
                    Assert.Equal(8000, reader.WaveFormat.SampleRate);
                    Assert.Equal(1, reader.WaveFormat.Channels);
                }
            }
            finally
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Best-effort test cleanup.
                }
            }
        }

        private static void WriteSilentWav(string path)
        {
            const int sampleRate = 8000;
            const short channels = 1;
            const short bitsPerSample = 8;
            const int dataSize = 8;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            short blockAlign = (short)(channels * bitsPerSample / 8);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                WriteAscii(writer, "RIFF");
                writer.Write(36 + dataSize);
                WriteAscii(writer, "WAVE");
                WriteAscii(writer, "fmt ");
                writer.Write(16);
                writer.Write((short)1); // PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);
                WriteAscii(writer, "data");
                writer.Write(dataSize);

                for (int i = 0; i < dataSize; i++)
                    writer.Write((byte)128); // 8-bit PCM silence
            }
        }

        private static void WriteAscii(BinaryWriter writer, string value)
        {
            foreach (char c in value)
                writer.Write((byte)c);
        }
    }
}
