using Godot;
using System;

public static class AudioHelper
{
    public static AudioStream LoadAudio(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (path.StartsWith("res://"))
        {
            return GD.Load<AudioStream>(path);
        }

        if (!FileAccess.FileExists(path))
        {
            Terminal.WriteError($"Audio file not found: {path}");
            return null;
        }

        byte[] data = FileAccess.GetFileAsBytes(path);
        string ext = path.GetExtension().ToLower();

        try
        {
            switch (ext)
            {
                case "mp3":
                    var mp3 = new AudioStreamMP3();
                    mp3.Data = data;
                    return mp3;
                case "ogg":
                    return AudioStreamOggVorbis.LoadFromBuffer(data);
                case "wav":
                    return LoadWav(data);
                default:
                    Terminal.WriteError($"Unsupported audio format: {ext}");
                    return null;
            }
        }
        catch (Exception e)
        {
            Terminal.WriteError($"Failed to load external audio ({ext}): {e.Message}");
            return null;
        }
    }

    private static AudioStreamWav LoadWav(byte[] data)
    {
        // Minimal WAV parser to support external .wav files in Godot
        // This assumes a standard RIFF WAV format
        var stream = new AudioStreamWav();
        
        if (data.Length < 44) return null; // Too small for a valid WAV

        // Basic header parsing (RIFF)
        // Offset 22: Channels (2 bytes)
        // Offset 24: Sample Rate (4 bytes)
        // Offset 34: Bits per sample (2 bytes)
        // Offset 40: Data size (4 bytes)
        
        ushort channels = BitConverter.ToUInt16(data, 22);
        uint sampleRate = BitConverter.ToUInt32(data, 24);
        ushort bitsPerSample = BitConverter.ToUInt16(data, 34);

        stream.Format = bitsPerSample switch
        {
            8 => AudioStreamWav.FormatEnum.Format8Bits,
            16 => AudioStreamWav.FormatEnum.Format16Bits,
            _ => AudioStreamWav.FormatEnum.ImaAdpcm // Fallback/default
        };

        stream.Stereo = channels == 2;
        stream.MixRate = (int)sampleRate;
        
        // Find data chunk
        int dataOffset = 12;
        while (dataOffset + 8 < data.Length)
        {
            string chunkName = System.Text.Encoding.ASCII.GetString(data, dataOffset, 4);
            uint chunkSize = BitConverter.ToUInt32(data, dataOffset + 4);
            if (chunkName == "data")
            {
                byte[] audioData = new byte[chunkSize];
                Array.Copy(data, dataOffset + 8, audioData, 0, Math.Min(chunkSize, data.Length - dataOffset - 8));
                stream.Data = audioData;
                break;
            }
            dataOffset += (int)chunkSize + 8;
        }

        return stream;
    }
}
