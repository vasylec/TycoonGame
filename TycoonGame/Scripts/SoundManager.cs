using NAudio.Wave;
using System;
using System.Collections.Generic;

namespace TycoonGame.Scripts
{
    public class SoundManager : IDisposable
    {
        public float MasterVolume { get; set; } = 1f;
        public float MusicVolume { get; set; } = 1f;
        public float SFXVolume { get; set; } = 1f;

        private WaveOutEvent musicOutput;
        private AudioFileReader musicFile;

        private List<(WaveOutEvent, AudioFileReader)> sfxPlayers = new List<(WaveOutEvent, AudioFileReader)>();

        // Muzica de fundal
        public void PlayMusic(string path, bool loop = true)
        {
            musicFile = new AudioFileReader(path);
            musicOutput = new WaveOutEvent();
            UpdateMusicVolume();
            musicOutput.Init(musicFile);
            musicOutput.Play();

            if (loop)
            {
                musicOutput.PlaybackStopped += (s, e) =>
                {
                    musicFile.Position = 0;
                    musicOutput.Play();
                };
            }
        }

        // Efecte SFX
        public void PlaySFX(string path)
        {
            var file = new AudioFileReader(path);
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

        // Actualizare volum muzică
        public void UpdateMusicVolume()
        {
            if (musicFile != null)
                musicFile.Volume = MusicVolume * MasterVolume;
        }

        // Actualizare volum SFX
        public void UpdateSFXVolume()
        {
            foreach (var (output, file) in sfxPlayers)
                if (file != null)
                    file.Volume = SFXVolume * MasterVolume;
        }

        public void Dispose()
        {
            musicOutput?.Dispose();
            musicFile?.Dispose();
        }
    }
}
