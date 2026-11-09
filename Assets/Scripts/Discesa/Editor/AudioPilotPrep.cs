using System;
using System.IO;
using System.Text;
using UnityEditor;

namespace Discesa.Editor
{
    // Local pilot preparation only. No generation, playback, or source writes.
    public static class AudioPilotPrep
    {
        internal static double[] Read(string path, out int channels, out int rate)
        {
            channels = 0;
            rate = 0;
            byte[] data = null;
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF") throw new InvalidDataException("Not RIFF");
                reader.ReadUInt32();
                if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE") throw new InvalidDataException("Not WAVE");
                while (reader.BaseStream.Position + 8 <= reader.BaseStream.Length)
                {
                    string id = Encoding.ASCII.GetString(reader.ReadBytes(4));
                    uint length = reader.ReadUInt32();
                    long next = reader.BaseStream.Position + length + (length & 1);
                    if (id == "fmt ")
                    {
                        int format = reader.ReadUInt16();
                        channels = reader.ReadUInt16();
                        rate = reader.ReadInt32();
                        reader.ReadUInt32();
                        reader.ReadUInt16();
                        int bits = reader.ReadUInt16();
                        if (format != 1 || bits != 16) throw new InvalidDataException("Requires PCM16 WAV");
                    }
                    else if (id == "data") data = reader.ReadBytes(checked((int)length));
                    reader.BaseStream.Position = next;
                }
            }
            if (data == null || channels < 1 || rate != 44100) throw new InvalidDataException("Missing data or unexpected format");
            var samples = new double[data.Length / 2];
            for (int i = 0; i < samples.Length; i++) samples[i] = BitConverter.ToInt16(data, i * 2) / 32768.0;
            return samples;
        }

        public static string Analyze(string path, double start, double end, double step)
        {
            int channels, rate;
            double[] samples = Read(path, out channels, out rate);
            int frames = samples.Length / channels;
            double peak = 0;
            int rails = 0;
            foreach (double sample in samples)
            {
                peak = Math.Max(peak, Math.Abs(sample));
                if (sample >= 32767.0 / 32768 || sample <= -1) rails++;
            }
            var text = new StringBuilder();
            text.AppendLine(path + " duration=" + ((double)frames / rate).ToString("F6") + " channels=" + channels + " rate=" + rate + " peak=" + peak.ToString("F6") + " railSamples=" + rails);
            for (double t = start; t < Math.Min(end, (double)frames / rate) - 0.000001; t += step)
            {
                int a = (int)Math.Round(t * rate), b = Math.Min(frames, (int)Math.Round((t + step) * rate));
                double energy = 0, binPeak = 0;
                for (int f = a; f < b; f++)
                    for (int c = 0; c < channels; c++)
                    {
                        double s = samples[f * channels + c];
                        energy += s * s;
                        binPeak = Math.Max(binPeak, Math.Abs(s));
                    }
                double rms = Math.Sqrt(energy / Math.Max(1, (b - a) * channels));
                text.AppendLine(t.ToString("F3") + " RMSdB=" + (20 * Math.Log10(Math.Max(rms, 1e-9))).ToString("F1") + " peak=" + binPeak.ToString("F4"));
            }
            return text.ToString();
        }

        public static string Export(string source, string output, int startFrame, int endFrame)
        {
            if (File.Exists(output) || File.Exists(output + ".meta")) throw new IOException("Output already exists: " + output);
            int channels, rate;
            double[] samples = Read(source, out channels, out rate);
            int frames = endFrame - startFrame;
            if (startFrame < 0 || endFrame > samples.Length / channels || frames < 1) throw new ArgumentOutOfRangeException();
            double nativeEnergy = 0, monoEnergy = 0;
            for (int f = startFrame; f < endFrame; f++)
            {
                double sum = 0;
                for (int c = 0; c < channels; c++)
                {
                    double s = samples[f * channels + c];
                    sum += s;
                    nativeEnergy += s * s / channels;
                }
                monoEnergy += (sum / channels) * (sum / channels);
            }
            double cancellationDb = 10 * Math.Log10(Math.Max(monoEnergy, 1e-20) / Math.Max(nativeEnergy, 1e-20));
            int outputChannels = cancellationDb < -6 ? channels : 1;
            double[] prepared = new double[frames * outputChannels];
            int fadeIn = (int)Math.Round(rate * 0.0015), fadeOut = (int)Math.Round(rate * 0.015);
            double peak = 0;
            for (int f = 0; f < frames; f++)
            {
                double fade = Math.Min(1.0, (double)f / (fadeIn - 1)) * Math.Min(1.0, (double)(frames - 1 - f) / (fadeOut - 1));
                for (int c = 0; c < outputChannels; c++)
                {
                    double value = 0;
                    if (outputChannels == 1)
                    {
                        for (int k = 0; k < channels; k++) value += samples[(startFrame + f) * channels + k] / channels;
                    }
                    else value = samples[(startFrame + f) * channels + c];
                    value *= fade;
                    prepared[f * outputChannels + c] = value;
                    peak = Math.Max(peak, Math.Abs(value));
                }
            }
            if (peak <= 0) throw new InvalidDataException("Silent excerpt");
            double gain = Math.Pow(10, -3.0 / 20) / peak;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            using (var writer = new BinaryWriter(new FileStream(output, FileMode.CreateNew)))
            {
                int bytes = prepared.Length * 2;
                writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + bytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)outputChannels); writer.Write(rate);
                writer.Write(rate * outputChannels * 2); writer.Write((short)(outputChannels * 2)); writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(bytes);
                foreach (double value in prepared) writer.Write((short)Math.Round(value * gain * 32768));
            }
            AssetDatabase.ImportAsset(output, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(output);
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = UnityEngine.AudioCompressionFormat.PCM;
            settings.loadType = UnityEngine.AudioClipLoadType.DecompressOnLoad;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.SaveAndReimport();
            return "Cut [" + startFrame + "," + endFrame + ") frames = " + ((double)startFrame / rate).ToString("F6") + "–" + ((double)endFrame / rate).ToString("F6") + "s; mono energy relative to native=" + cancellationDb.ToString("F2") + "dB; gain=" + (20 * Math.Log10(gain)).ToString("F2") + "dB; fades=1.5ms/15ms; " + Analyze(output, 0, 0, 1);
        }
    }
}
