#region Using Declarations
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
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
    public struct ScoreAndStars
    {
        public ScoreAndStars(double score, uint stars)
        {
            this.Score = score;
            this.Stars = stars;
        }
        public double Score;
        public uint Stars;
    }
    public class Band
    {
        private String _bandName;
        private Dictionary<String, ScoreAndStars> _songStats;
        public Dictionary<String, ScoreAndStars> SongStats
        {
            get { return _songStats; }
        }
        
        private String _saveLocation;
        private Texture2D _bandLogo = null;
        private String _logoName = "";

        public String LogoName
        {
            get { return _logoName; }
        }

        public uint Stars
        {
            get
            {
                uint stars = 0;
                foreach (Player p in BandMembers)
                    stars += p.Stars;
                return stars;
            }
        }

        public double CurrentScore
        {
            get 
            {
                double score = 0.0;
                foreach (Player p in BandMembers)
                    score += p.Score;
                return score;
            }
        }

        public int HitNotes
        {
            get
            {
                int notesHit = 0;
                foreach (Player p in BandMembers)
                    notesHit += p.NotesHit;
                return notesHit;
            }
        }

        public int MissedNotes
        {
            get
            {
                int notesHit = 0;
                foreach (Player p in BandMembers)
                    notesHit += p.NotesMissed;
                return notesHit;
            }
        }

        public Texture2D BandLogo
        {
            get
            {
                return _bandLogo;
            }
        }

        public List<Player> BandMembers;

        public Band()
        {
            _bandName = "";
            _songStats = new Dictionary<String, ScoreAndStars>();
            BandMembers = new List<Player>();
        }

        public String BandName
        {
            get { return _bandName; }
            set { _bandName = value; }
        }

        public double GetSongScore(String song) 
        {
            if (_songStats.ContainsKey(song))
                return ((ScoreAndStars)_songStats[song]).Score;
            return 0.0;
        }

        public uint GetSongStars(String song)
        {
            if (_songStats.ContainsKey(song))
                return ((ScoreAndStars)_songStats[song]).Stars;
            return 0;
        }

        public void ScoreSong(String song, double score, uint stars)
        {
            if (!_songStats.ContainsKey(song))
            {
                _songStats.Add(song, new ScoreAndStars(score, stars));
            }
            else if (score > ((ScoreAndStars)_songStats[song]).Score)
            {
                _songStats[song] = new ScoreAndStars(score, stars);
            }
        }

        public void LoadBandLogo(String fileLocation)
        {
            _bandLogo = null;
            if (fileLocation != null && !fileLocation.Equals(""))
            {
                _bandLogo = Texture2D.FromStream(RhythmGame.GameInstance.GraphicsDevice, new FileStream(fileLocation, FileMode.Open)); // 4.0change
            }

            if ( _bandLogo == null )
                _bandLogo = RhythmGame.GameInstance.Content.Load<Texture2D>("Band\\FuerteLogo");
        }

        public static Band LoadBandFromFile(String fileLocation)
        {
            Band band = new Band();
            band._saveLocation = fileLocation;

            try
            {
                StreamReader streamReader = new StreamReader(fileLocation);
                String line;

                // Read in band name
                if ((line = streamReader.ReadLine()) != null)
                    band._bandName = line;
                // Read in song data
                while ((line = streamReader.ReadLine()) != null)
                {
                    String[] data = line.Split(';');
                    switch (data[0])
                    {
                        case "S":
                            // Song data
                            ScoreAndStars scores = new ScoreAndStars();
                            scores.Score = double.Parse(data[2]);
                            if ( data.Length > 3 )
                                scores.Stars = uint.Parse(data[3]);
                            else
                                scores.Stars = 0;
                            band._songStats.Add(data[1], scores);
                            break;
                        case "C":
                            // Challenge data
                            break;
                        case "L":
                            // Logo name...
                            band._logoName = data[1];
                            break;
                    }
                }

                streamReader.Close();
            }
            catch (Exception)
            {
                // Do things here incase it can't read the file
            }

            return band;

        }

        public static Band SaveBandToFile(Band band)
        {
            String fileLocation = band._saveLocation;

            try
            {
                StreamWriter streamWriter = new StreamWriter(fileLocation);

                // Read in band name
                streamWriter.WriteLine(band._bandName);
                // Read in song data
                foreach (KeyValuePair<String, ScoreAndStars> pair in band._songStats)
                {
                    streamWriter.WriteLine("S;" + pair.Key + ";" + pair.Value.Score + ";" + pair.Value.Stars);
                }
                // Save out their logo name...
                streamWriter.WriteLine("L;"+band._logoName);

                streamWriter.Close();
            }
            catch (Exception)
            {
                // Do things here incase it can't read the file
            }

            return band;

        }

        public static Band GenerateRandomBand()
        {
            Band band = new Band();
            band.BandName = "Random band name";
            return band;
        }
    }

    public class BandSearcher
    {
        public static List<Band> SearchDirectory(String location)
        {
            List<Band> list = new List<Band>();
            DirectoryInfo dir = new DirectoryInfo(location);
            if (dir.Exists)
            {
                foreach (FileInfo fl in dir.GetFiles("*.bnd"))
                {
                    Band bandInfo = Band.LoadBandFromFile(fl.FullName);
                    String logoPath = (bandInfo.LogoName != null && !bandInfo.LogoName.Equals("")) ? Path.Combine(fl.Directory.Name, bandInfo.LogoName) : null;
                    bandInfo.LoadBandLogo(logoPath);
                    list.Add(bandInfo);
                }
            }

            return list;
        }

        public static List<Band> SearchDefaultLocation()
        {
            IAsyncResult result;
            result = StorageDevice.BeginShowSelector(PlayerIndex.One, null, null);

            // wait in result...
            StorageDevice device = StorageDevice.EndShowSelector(result);

            StorageContainer container = Setlist.OpenContainer(device, "Fortissimo");
            String directory = Path.Combine(/*TITLE_LOCATION,*/ "Bands");

            return SearchDirectory(directory);
        }
    }
}
