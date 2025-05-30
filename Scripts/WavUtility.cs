using System.IO;
using System.Text;
using UnityEngine;

public static class WavUtility
{
    public static byte[] FromAudioClip(AudioClip clip)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memoryStream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(0); // Placeholder for file size
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Sub-chunk size (16 for PCM)
                writer.Write((ushort)1); // Audio format (1 for PCM)
                writer.Write((ushort)clip.channels);
                writer.Write(clip.frequency);
                writer.Write(clip.frequency * clip.channels * 2); // Byte rate
                writer.Write((ushort)(clip.channels * 2)); // Block align
                writer.Write((ushort)16); // Bits per sample

                writer.Write(Encoding.ASCII.GetBytes("data"));
                float[] samples = new float[clip.samples * clip.channels];
                clip.GetData(samples, 0);
                byte[] sampleBytes = new byte[samples.Length * 2]; // 16-bit samples

                int sampleIndex = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    short val = (short)(samples[i] * short.MaxValue);
                    sampleBytes[sampleIndex++] = (byte)(val & 0xFF);
                    sampleBytes[sampleIndex++] = (byte)((val >> 8) & 0xFF);
                }
                writer.Write(sampleBytes.Length); // Data sub-chunk size
                writer.Write(sampleBytes);

                // Go back and write the file size
                long fileSize = memoryStream.Length;
                writer.Seek(4, SeekOrigin.Begin);
                writer.Write((int)(fileSize - 8));
            }
            return memoryStream.ToArray();
        }
    }
}
