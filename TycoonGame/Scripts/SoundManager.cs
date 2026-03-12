using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TycoonGame.Scripts
{
    public class SoundManager : IDisposable
    {
        private const string SettingsFileName = "audio-settings.json";

        private float _masterVolume = 1f;
        private float _musicVolume = 0.5f;
        private float _sfxVolume = 0.5f;

        private WaveOutEvent? musicOutput;
        private AudioFileReader? musicFile;

        private readonly List<(WaveOutEvent output, AudioFileReader file)> activeSfx = new();
        private readonly string clickPath;
        private readonly string settingsPath;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Clamp01(value);
                UpdateMusicVolume();
                SaveSettings();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Clamp01(value);
                UpdateMusicVolume();
                SaveSettings();
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Clamp01(value);
                SaveSettings();
            }
        }

        public SoundManager()
        {
            clickPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sounds", "select_005.wav");

            var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TycoonGame");
            Directory.CreateDirectory(appDataDir);
            settingsPath = Path.Combine(appDataDir, SettingsFileName);

            LoadSettings();
        }

        public void PlayMusic(string relativePath)
        {
            string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Music file not found", fullPath);

            if (musicOutput != null)
                return;

            musicFile = new AudioFileReader(fullPath);
            musicOutput = new WaveOutEvent();
            UpdateMusicVolume();
            musicOutput.Init(musicFile);
            musicOutput.Play();

            musicOutput.PlaybackStopped += (_, _) =>
            {
                if (musicFile != null)
                {
                    musicFile.Position = 0;
                    musicOutput.Play();
                }
            };
        }

        // Păstrat pentru compatibilitate cu codul existent
        public void InitClickSound()
        {
            // Nu e nevoie de pre-load; verificăm doar existența
            _ = File.Exists(clickPath);
        }

        public void PlayClick()
        {
            if (!File.Exists(clickPath)) return;

            try
            {
                var file = new AudioFileReader(clickPath)
                {
                    Volume = SFXVolume * MasterVolume
                };
                var output = new WaveOutEvent();
                output.Init(file);

                output.PlaybackStopped += (_, _) =>
                {
                    output.Dispose();
                    file.Dispose();
                    activeSfx.RemoveAll(x => ReferenceEquals(x.output, output));
                };

                activeSfx.Add((output, file));
                output.Play();
            }
            catch
            {
                // silent fail
            }
        }

        public void UpdateMusicVolume()
        {
            if (musicFile != null)
                musicFile.Volume = MusicVolume * MasterVolume;
        }

        public void UpdateSFXVolume()
        {
            foreach (var (_, file) in activeSfx)
                file.Volume = SFXVolume * MasterVolume;
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(settingsPath)) return;

                var json = File.ReadAllText(settingsPath);
                var data = JsonSerializer.Deserialize<AudioSettingsData>(json);
                if (data == null) return;

                _masterVolume = Clamp01(data.MasterVolume);
                _musicVolume = Clamp01(data.MusicVolume);
                _sfxVolume = Clamp01(data.SFXVolume);
            }
            catch
            {
                // păstrăm valorile default
            }
        }

        private void SaveSettings()
        {
            try
            {
                var data = new AudioSettingsData
                {
                    MasterVolume = _masterVolume,
                    MusicVolume = _musicVolume,
                    SFXVolume = _sfxVolume
                };
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(data));
            }
            catch
            {
                // silent fail
            }
        }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        public void Dispose()
        {
            musicOutput?.Stop();
            musicOutput?.Dispose();
            musicFile?.Dispose();

            foreach (var (output, file) in activeSfx)
            {
                output.Stop();
                output.Dispose();
                file.Dispose();
            }
            activeSfx.Clear();
        }

        private sealed class AudioSettingsData
        {
            public float MasterVolume { get; set; } = 1f;
            public float MusicVolume { get; set; } = 0.5f;
            public float SFXVolume { get; set; } = 0.5f;
        }
    }
}
