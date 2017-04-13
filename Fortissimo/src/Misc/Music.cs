// Description  : This provides an abstract class MusicPlayer that is intended
//                to be overloaded for being able to play different types
//                of music files.

#region Using Statements
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using SongDataIO;
#if WINDOWS
using IrrKlang;
#endif
#endregion

namespace Fortissimo
{
    public struct SongDataPlus : IOneLineProvider
    {
        public enum NoteType { GBA, MID, GenMID, None }
        public SongData songData;
        public String dirPath;
        public String fullPath;
        public NoteType type;

        public String GetLine() { return songData.info.name; }
    }

    #region Abstract Music Player
    public abstract class MusicPlayer : Microsoft.Xna.Framework.GameComponent
    {
        protected bool useBaseSong = false;
        protected bool failedLoad = false;
        protected bool failedTrack = false;

        public MusicPlayer(Game game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            // TODO: Add your update code here

            base.Update(gameTime);
        }
        
        public abstract bool LoadSong(String location, String name);
        public abstract int LoadTrack(String location, String name);

        public abstract void PlaySong();
        public abstract void PlayTrack(int idx);
        public abstract void PlayAll();

        public abstract void PauseSong(bool pause);
        public abstract void PauseTrack(int idx, bool pause);
        public abstract void PauseAll(bool pause);

        public abstract void StopAll();
        public abstract TimeSpan PlayPosition();

        public abstract void MissedNote(SongData.NoteSet i);
        public abstract void HitNote(SongData.NoteSet i);

        public abstract String SetupExplanation();

        public abstract void FadeVolume(double fade);
    }
    #endregion

    #region Mp3 Player
    public class MP3Player : MusicPlayer
    {
        protected ArrayList soundEffects;
        protected ArrayList soundEffectInstances;
        protected ContentManager content;

        protected Song baseSong;

        public MP3Player(Game game)
            : base(game)
        {
            content = game.Content;
            soundEffects = new ArrayList();
            soundEffectInstances = new ArrayList();
        }

        #region Loading Songs/Tracks
        public override bool LoadSong(String location, String name)
        {
            try
            {
                String fullPath = Path.Combine(location, name);
                fullPath = fullPath+".mp3";

                Song song = content.Load<Song>(fullPath);
                baseSong = song;
                useBaseSong = true;
            }
            catch (Exception)
            {
                failedLoad = true;
                return false;
            }
            return true;
        }

        public override int LoadTrack(String location, String name)
        {
            try
            {
                String fullPath = Path.Combine(location, name);
                fullPath = fullPath+".mp3";

                SoundEffect effect = content.Load<SoundEffect>(fullPath);
                soundEffectInstances.Add(null);
                return soundEffects.Add(effect);
            }
            catch (Exception)
            {
                failedTrack = true;
                return 0;
            }
        }
        #endregion

        #region Play Songs/Tracks
        public override void PlaySong()
        {
            //if (MediaPlayer.State == MediaState.Playing)
            //    return;
            if ( !failedLoad )
                MediaPlayer.Play(baseSong);
            MediaPlayer.Volume = 1.0F;
        }

        public override void PlayTrack(int idx)
        {
            if (!failedTrack)
            {
                ((SoundEffect)soundEffects[idx]).Play();
                soundEffectInstances[idx] = (SoundEffect)soundEffects[idx];
            }
        }

        public override void PlayAll()
        {
            if (useBaseSong)
            {
                PlaySong();
            }

            if (!failedTrack)
            {
                int i = 0;
                foreach (SoundEffect effect in soundEffects)
                {
                    effect.Play(); // 4.0change
                    SoundEffectInstance e = effect.CreateInstance();
                    //SoundEffectInstance e = effect.Play();
                    soundEffectInstances[i] = e;
                    i++;
                }
            }
        }
        #endregion

        #region Pause Songs/Tracks
        public override void PauseSong(bool pause)
        {
            if (pause)
            {
                MediaPlayer.Pause();
            }
            else
            {
                MediaPlayer.Resume();
            }
        }

        public override void PauseTrack(int idx, bool pause)
        {
            if (pause)
                ((SoundEffectInstance)soundEffectInstances[idx]).Pause();
            else
                ((SoundEffectInstance)soundEffectInstances[idx]).Resume();
        }

        public override void PauseAll(bool pause)
        {
            if (useBaseSong)
            {
                PauseSong(pause);
            }

            foreach (SoundEffectInstance e in soundEffectInstances)
            {
                if (pause)
                    e.Pause();
                else
                    e.Resume();
            }
        }
        #endregion

        public override void StopAll()
        {
            try
            {
                if (MediaPlayer.GameHasControl)
                    MediaPlayer.Stop();
                foreach (SoundEffectInstance e in soundEffectInstances)
                {
                    e.Stop();
                }
            }
            catch { /* Its most likely not running anything */ }
        }

        public override TimeSpan PlayPosition()
        {
            return MediaPlayer.PlayPosition;
        }

        public override void MissedNote(SongData.NoteSet i)
        {
            foreach (SoundEffectInstance e in soundEffectInstances)
            {
                e.Volume = 0.0F;
            }
        }

        public override void HitNote(SongData.NoteSet i)
        {
            foreach (SoundEffectInstance e in soundEffectInstances)
            {
                e.Volume = 1.0F;
            }
        }

        public override String SetupExplanation()
        {
            return "Built-in:\n      MP3 files can be embedded into the game.  Must have source code.";
        }

        public override void FadeVolume(double fade)
        {
            MediaPlayer.Volume = MediaPlayer.Volume + (float)fade;
            if (MediaPlayer.Volume < 0)
                MediaPlayer.Volume = 0;
            foreach (SoundEffectInstance e in soundEffectInstances)
            {
                e.Volume = e.Volume + (float)fade;
                if (e.Volume < 0)
                    e.Volume = 0;
            }
        }
    }
    #endregion

#if WINDOWS
    public class OGGPlayer : MusicPlayer
    {
        ISoundEngine soundEngine;
        ISound background;
        ArrayList tracks;

        public OGGPlayer(Game game)
            : base(game)
        {
            tracks = new ArrayList();
        }

        #region Loading Songs/Tracks
        public override bool LoadSong(String location, String name)
        {
            String fullPath = Path.Combine(location, name);
            fullPath = fullPath+".ogg";

            soundEngine = new ISoundEngine();
            ISoundSource song = soundEngine.AddSoundSourceFromFile(fullPath, StreamMode.NoStreaming, true);
            background = soundEngine.Play2D(song, false, true, true);
            useBaseSong = false;
            if (background != null)
            {
                useBaseSong = true;
                return true;
            }
            return false;
        }

        public override int LoadTrack(String location, String name)
        {
            String fullPath = Path.Combine(location, name);
            fullPath = fullPath + ".ogg";

            soundEngine = new ISoundEngine();
            ISoundSource song = soundEngine.AddSoundSourceFromFile(fullPath, StreamMode.NoStreaming, true);
            ISound sound = soundEngine.Play2D(song, false, true, true);
            if (sound != null)
                return tracks.Add(sound);
            return -1;
        }
        #endregion

        #region Play Songs/Tracks
        public override void PlaySong()
        {
            if (background != null)
            {
                background.Paused = false;
            }
        }

        public override void PlayTrack(int idx)
        {
            if ( tracks[idx] != null )
                ((ISound)tracks[idx]).Paused = false;
        }

        public override void PlayAll()
        {
            PlaySong();
            for (int i = 0; i < tracks.Count; i++)
                if (tracks[i] is ISound)
                    ((ISound)tracks[i]).Paused = false;
        }
        #endregion

        #region Pause Songs/Tracks
        public override void PauseSong(bool pause)
        {
            if (background != null)
                background.Paused = pause;
        }

        public override void PauseTrack(int idx, bool pause)
        {
            if ( idx < tracks.Count )
                if (tracks[idx] != null)
                    ((ISound)tracks[idx]).Paused = pause;
        }

        public override void PauseAll(bool pause)
        {
            PauseSong(pause);

            for (int i = 0; i < tracks.Count; i++)
                if (tracks[i] != null)
                    ((ISound)tracks[i]).Paused = pause;
        }
        #endregion

        public override void StopAll()
        {
            if (background != null)
                background.Stop();
            for (int i = 0; i < tracks.Count; i++)
                if (tracks[i] is ISound)
                    ((ISound)tracks[i]).Stop();
        }

        public override TimeSpan PlayPosition()
        {
            if (background != null)
                return TimeSpan.FromMilliseconds(background.PlayPosition);
            return TimeSpan.Zero;
        }

        public override void MissedNote(SongData.NoteSet i)
        {
            // 4.0change - TODO - only mute the guitar track (usually 5 + 8*i, but not always)
            
            foreach (ISound s in tracks)
            {
                s.Volume = 0.0f;
            }
            //for(int j = 5; j < tracks.Count; j+=8)
              //  ((ISound)tracks[j]).Volume = 0; // MAGIC NUMBER! (It's the guitar track)

        }

        public override void HitNote(SongData.NoteSet i)
        {
            foreach (ISound s in tracks)
            {
                s.Volume = 1.0f;
            }
        }

        public override void FadeVolume(double fade)
        {
            background.Volume = background.Volume + (float)fade;
            if (background.Volume < 0)
                background.Volume = 0;
            foreach (ISound s in tracks)
            {
                s.Volume = s.Volume + (float)fade;
                if (s.Volume < 0)
                    s.Volume = 0;
            }
        }

        public override String SetupExplanation()
        {
            return "OGG Files:\n      Place the .ogg file in the Songs directory of the game.\n      Songs must have the same name as the notes files.";
        }
    }
#endif

    public class MediaLibraryPlayer : MP3Player
    {
        public MediaLibraryPlayer(Game game)
            : base(game)
        {
        }

        #region Loading Songs/Tracks
        public override bool LoadSong(String location, String name)
        {
            MediaLibrary mediaLibrary = null;
            foreach (MediaSource mediaSource in MediaSource.GetAvailableMediaSources())
            {
                if (mediaSource.MediaSourceType == MediaSourceType.LocalDevice)
                {
                    mediaLibrary = new MediaLibrary(mediaSource);
                    break;
                }
            }
            if (mediaLibrary == null)
                return false;

            SongCollection songs = mediaLibrary.Songs;
            foreach (Song s in songs)
            {
                if ( s.Name.Equals(name) )
                {
                    useBaseSong = true;
                    failedLoad = false;
                    baseSong = s;
                    return true;
                }
            }
            failedLoad = true;
            return false;
        }

        public override int LoadTrack(String location, String name)
        {
            failedTrack = true;
            return 0;
        }

        public override String SetupExplanation()
        {
            return "Midi Files:\n      Load the midi files through Windows Media Player.  This adds the songs\n      to your library.  The game then searches your song library for files with\n      the same name as the note format.";
        }
        #endregion

    }
}
