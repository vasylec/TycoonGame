using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;

namespace TycoonGame.Scripts
{
    public class SoundManager : IDisposable
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SFXVolume { get; set; } = 1f;

        private WaveOutEvent? musicOutput;
        private AudioFileReader? musicFile;

        private List<(WaveOutEvent, AudioFileReader)> sfxPlayers = new();

        public void PlayMusic(string relativePath)
        {
            string fullPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                relativePath
            );

            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Music file not found", fullPath);

            if (musicOutput != null) return; // deja rulează

            musicFile = new AudioFileReader(fullPath);
            musicOutput = new WaveOutEvent();

            UpdateMusicVolume();
            musicOutput.Init(musicFile);
            musicOutput.Play();

            // loop
            musicOutput.PlaybackStopped += (s, e) =>
            {
                if (musicFile != null)
                {
                    musicFile.Position = 0;
                    musicOutput.Play();
                }
            };
        }

        public void PlaySFX(string relativePath)
        {
            string fullPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                relativePath
            );

            if (!File.Exists(fullPath)) return;

            var file = new AudioFileReader(fullPath);
            var output = new WaveOutEvent();
            file.Volume = SFXVolume * MasterVolume;

            output.Init(file);
            output.Play();

            output.PlaybackStopped += (s, e) =>
            {
                output.Dispose();
                file.Dispose();
            };

            sfxPlayers.Add((output, file));
        }

        public void UpdateMusicVolume()
        {
            if (musicFile != null)
                musicFile.Volume = MusicVolume * MasterVolume;
        }

        public void UpdateSFXVolume()
        {
            foreach (var pair in sfxPlayers)
            {
                pair.Item2.Volume = SFXVolume * MasterVolume;
            }
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
