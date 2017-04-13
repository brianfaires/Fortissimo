#region Using Declarations
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
#endregion

namespace Fortissimo
{
    public class Setlist
    {
        public Setlist(List<SongDataPlus> Songs, String Name)
        {
            this.Songs = Songs;
            this.Name = Name;
        }
        public List<SongDataPlus> Songs;
        public String Name;

        /// <summary>
        /// Synchronously opens storage container
        /// </summary>
        public static StorageContainer OpenContainer(StorageDevice storageDevice, string saveGameName)
        {
            IAsyncResult result = storageDevice.BeginOpenContainer(saveGameName, null, null);
 
            // Wait for the WaitHandle to become signaled.
            result.AsyncWaitHandle.WaitOne();
 
            StorageContainer container = storageDevice.EndOpenContainer(result);
 
            // Close the wait handle.
            result.AsyncWaitHandle.Close();
 
            return container;
        }
    }

    public class SongSearcher
    {
        public static SongDataPlus GetMidiSongDataPlus(DirectoryInfo dir, FileInfo fl, bool isFromGuitarHero)
        {
            SongDataPlus dataPlus = new SongDataPlus();
            // TODO: Add midi specific information here.
            dataPlus.songData = new SongData();
            dataPlus.songData.info.name = fl.Name.Split('.')[0];
            dataPlus.songData.info.artist = "Unknown";
            dataPlus.songData.info.filename = dataPlus.songData.info.name;
            dataPlus.fullPath = fl.FullName;
            dataPlus.type = isFromGuitarHero ? SongDataPlus.NoteType.MID : SongDataPlus.NoteType.GenMID;
            dataPlus.dirPath = dir.ToString();

            return dataPlus;
        }

        public static Setlist SearchDirectory(DirectoryInfo dir)
        {
            List<SongDataPlus> list = new List<SongDataPlus>();
            foreach (FileInfo fl in dir.GetFiles("*.gba"))
            {
                SongDataPlus dataPlus = new SongDataPlus();
                dataPlus.fullPath = fl.FullName;
                dataPlus.songData = SongDataIO.SongLoader.LoadSong(dataPlus.fullPath);
                dataPlus.dirPath = dir.ToString();
                dataPlus.type = SongDataPlus.NoteType.GBA;
                list.Add(dataPlus);
            }
            foreach (FileInfo fl in dir.GetFiles("*.mid"))
            {
                // .mid files are considered to be Notes.mid files from commercial games, not general Midis
                //if(fl.Name.Contains("if"))

               list.Add(GetMidiSongDataPlus(dir, fl, true));
            }
            
            foreach (FileInfo fl in dir.GetFiles("*.fff"))
            {
                foreach (FileInfo fl2 in dir.GetFiles("*.ffm"))
                {
                    // Only add .fff files if there is a corresponding .ffm file
                    if (fl2.FullName.Substring(0, fl2.FullName.Length - 4).CompareTo(fl.FullName.Substring(0, fl.FullName.Length - 4)) == 0)
                    {
                        list.Add(GetMidiSongDataPlus(dir, fl, false));
                        break;
                    }
                }
            }
            
            Setlist s = new Setlist(list, dir.Name);
            return s;
        }

        public static List<Setlist> SearchFileLocation(String location)
        {
            List<Setlist> list = new List<Setlist>();
            DirectoryInfo dr = new DirectoryInfo(location);
            if (dr.Exists)
            {
                Setlist s = SearchDirectory(dr);
                s.Name = "Core list";
                list.Add(s);
                foreach (DirectoryInfo dir in dr.GetDirectories())
                {
                    list.Add(SearchDirectory(dir));
                }
            }
            return list;
        }

        public static List<Setlist> SearchDefaultLocation()
        {
            IAsyncResult result;
            result = StorageDevice.BeginShowSelector(PlayerIndex.One, null, null);

            // wait in result...
            StorageDevice device = StorageDevice.EndShowSelector(result);
    
            StorageContainer container = Setlist.OpenContainer(device, "Fortissimo");
            String directory = Path.Combine(/*TITLE_LOCATION,*/ "Songs");

            return SearchFileLocation(directory);
        }

        public static Setlist SearchAllLocations()
        {
            IAsyncResult result;
            result = StorageDevice.BeginShowSelector(PlayerIndex.One, null, null);

            // wait in result...
            StorageDevice device = StorageDevice.EndShowSelector(result);

            StorageContainer container = Setlist.OpenContainer(device, "Fortissimo");
            String directory = Path.Combine(/*TITLE_LOCATION,*/ "Songs");


            List<SongDataPlus> list = new List<SongDataPlus>();
            DirectoryInfo dr = new DirectoryInfo(directory);
            if (dr.Exists)
            {
                list.AddRange(SearchDirectory(dr).Songs);
                //list.Add(SearchDirectory(dr));
                foreach (DirectoryInfo dir in dr.GetDirectories())
                {
                    list.AddRange(SearchDirectory(dir).Songs);
                }
            }

            Setlist s = new Setlist(list, "All Songs");
            return s;
        }

        public static SongDataPlus PickRandomSong()
        {
            IAsyncResult result;
            result = StorageDevice.BeginShowSelector(PlayerIndex.One, null, null);

            // wait in result...
            StorageDevice device = StorageDevice.EndShowSelector(result);

            StorageContainer container = Setlist.OpenContainer(device, "Fortissimo");
            String directory = Path.Combine(/*TITLE_LOCATION,*/ "Songs");


            List<SongDataPlus> list = new List<SongDataPlus>();
            DirectoryInfo dr = new DirectoryInfo(directory);
            if (dr.Exists)
            {
                list.AddRange(SearchDirectory(dr).Songs);
                //list.Add(SearchDirectory(dr));
                foreach (DirectoryInfo dir in dr.GetDirectories())
                {
                    list.AddRange(SearchDirectory(dir).Songs);
                }
            }
            if (list.Count != 0)
            {
                Random r = new Random();
                int idx = r.Next(list.Count);
                return list[idx];
            }
            else
            {
                SongDataPlus nullSong = new SongDataPlus();
                nullSong.type = SongDataPlus.NoteType.None;
                return nullSong;
            }
        }

        public static void SearchMediaLibrary()
        {
        }
    }

    public class MediaPlayerSetlist
    {
        protected List<Song> setlist;
        public List<Song> Set { get { return setlist; } }

        public void LoadAll()
        {
            setlist = new List<Song>();
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
                return;

            SongCollection songs = mediaLibrary.Songs;
            foreach (Song s in songs)
            {
                setlist.Add(s);
            }
        }
    }
}
