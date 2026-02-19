using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace TycoonGame.Scripts
{
    // ====================================
    // 🔹 Cached sound pentru click-uri rapide
    // ====================================
    public class CachedSound
    {
        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }

        public CachedSound(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            WaveFormat = reader.WaveFormat;

            var wholeFile = new List<float>();
            float[] buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                wholeFile.AddRange(buffer.AsSpan(0, read).ToArray());

            AudioData = wholeFile.ToArray();
        }
    }

    public class CachedSoundPlayer
    {
        private CachedSound sound;

        public CachedSoundPlayer(CachedSound sound)
        {
            this.sound = sound;
        }

        public void Play()
        {
            var waveOut = new WaveOutEvent();
            var provider = new BufferedWaveProvider(sound.WaveFormat);
            provider.AddSamples(FloatToByte(sound.AudioData), 0, sound.AudioData.Length * 4);
            waveOut.Init(provider);
            waveOut.Play();
        }

        private byte[] FloatToByte(float[] floatArray)
        {
            var bytes = new byte[floatArray.Length * 4];
            for (int i = 0; i < floatArray.Length; i++)
                Array.Copy(BitConverter.GetBytes(floatArray[i]), 0, bytes, i * 4, 4);
            return bytes;
        }
    }

    // ====================================
    // 🔹 SoundManager principal
    // ====================================
    public class SoundManager : IDisposable
    {
        private float _masterVolume = 1f;
        private float _musicVolume = 1f;
        private float _sfxVolume = 1f;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = value;
                UpdateMusicVolume();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = value;
                UpdateMusicVolume();
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = value;
        }

        private WaveOutEvent? musicOutput;
        private AudioFileReader? musicFile;
        private CachedSound? clickCached;

        private List<(WaveOutEvent, AudioFileReader)> sfxPlayers = new();

        // Muzică de fundal
        public void PlayMusic(string relativePath)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Music file not found", fullPath);

            if (musicOutput != null) return;

            musicFile = new AudioFileReader(fullPath);
            musicOutput = new WaveOutEvent();
            UpdateMusicVolume();
            musicOutput.Init(musicFile);
            musicOutput.Play();

            musicOutput.PlaybackStopped += (s, e) =>
            {
                if (musicFile != null)
                {
                    musicFile.Position = 0;
                    musicOutput.Play();
                }
            };
        }

        // Initializează click-ul o singură dată
        public void InitClickSound()
        {
            string clickPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets/Sounds/select_005.wav");
            if (File.Exists(clickPath))
                clickCached = new CachedSound(clickPath);
        }

        public void PlayClick()
        {
            if (clickCached == null) return;
            var player = new CachedSoundPlayer(clickCached);
            player.Play();
        }

        public void UpdateMusicVolume()
        {
            if (musicFile != null)
                musicFile.Volume = MusicVolume * MasterVolume;
        }

        public void UpdateSFXVolume()
        {
            foreach (var pair in sfxPlayers)
                pair.Item2.Volume = SFXVolume * MasterVolume;
        }
        public void Dispose()
        {
            musicOutput?.Stop();
            musicOutput?.Dispose();
            musicFile?.Dispose();
            sfxPlayers.Clear();
        }
    }
}
