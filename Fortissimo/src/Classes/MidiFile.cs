using System;
using System.Collections.Generic;
using System.IO;
using SongDataIO;

namespace Fortissimo
{
    public class MidiFile
    {
        private const int NOT_USED = 1 << 8; // Larger than any byte (==256)
        // Track class
        private class DensityTimes
        {
            public int ch_ch, ch_nx, nx_ch, nx_nx, ch_rpt, nx_rpt;
            public DensityTimes(float d)
            {
                if (d < 0.25)
                {
                    // Beginner
                    ch_ch = 800;
                    ch_nx = 600;
                    nx_ch = 600;
                    nx_nx = 500;
                    ch_rpt = 400;
                    nx_rpt = 400;
                }
                else if (d < 0.5)
                {
                    // Moderate
                    ch_ch = 600;
                    ch_nx = 500;
                    nx_ch = 500;
                    nx_nx = 400;
                    ch_rpt = 300;
                    nx_rpt = 300;
                }
                else if (d < 0.75)
                {
                    // Hard
                    ch_ch = 300;
                    ch_nx = 250;
                    nx_ch = 250;
                    nx_nx = 200;
                    ch_rpt = 150;
                    nx_rpt = 150;
                }
                else if (d < 0.95)
                {
                    // Expert
                    ch_ch = 75;
                    ch_nx = 60;
                    nx_ch = 60;
                    nx_nx = 60;
                    ch_rpt = 40;
                    nx_rpt = 40;
                }
                else
                {
                    // Super Expert
                    ch_ch = 30;
                    ch_nx = 10;
                    nx_ch = 10;
                    nx_nx = 10;
                    ch_rpt = 10;
                    nx_rpt = 10;
                }
            }
        }
        public class UserMods
        {
            public String filename;
            public float[] difficulty;
            public List<int> GuitarTracks;
            public List<int> RhythmTracks;
            public List<int> DrumTracks;
            public List<int> VocalTracks;

            public List<int[]>[][] changedNotes;

            public UserMods(String MidiFilename)
            {
                filename = MidiFilename.Substring(0, MidiFilename.Length - 4) + ".ffm";
                difficulty = new float[4];
                changedNotes = new List<int[]>[4][];
                for (int i = 0; i < 4; i++)
                {
                    changedNotes[i] = new List<int[]>[4];
                    for (int j = 0; j < 4; j++)
                    {
                        changedNotes[i][j] = new List<int[]>();
                    }
                }

                GuitarTracks = new List<int>();
                RhythmTracks = new List<int>();
                DrumTracks = new List<int>();
                VocalTracks = new List<int>();

                // Load data from .ffm file
                FileStream file = new FileStream(filename, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);

                difficulty[0] = float.Parse(sr.ReadLine());
                difficulty[1] = float.Parse(sr.ReadLine());
                difficulty[2] = float.Parse(sr.ReadLine());
                difficulty[3] = float.Parse(sr.ReadLine());
                sr.ReadLine(); // "End Difficulty/Seed Values"

                string line;
                while ((line = sr.ReadLine()).CompareTo("End Vocal Tracks") != 0)
                    VocalTracks.Add(int.Parse(line));
                while ((line = sr.ReadLine()).CompareTo("End Rhythm Tracks") != 0)
                    RhythmTracks.Add(int.Parse(line));
                while ((line = sr.ReadLine()).CompareTo("End Drum Tracks") != 0)
                    DrumTracks.Add(int.Parse(line));
                while ((line = sr.ReadLine()).CompareTo("End Guitar Tracks") != 0)
                    GuitarTracks.Add(int.Parse(line));

                while ((line = sr.ReadLine()).CompareTo("End Vocal Beginner Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[3][0].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Vocal Moderate Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[3][1].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Vocal Hard Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[3][2].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Vocal Expert Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[3][3].Add(newChange);
                }

                while ((line = sr.ReadLine()).CompareTo("End Rhythm Beginner Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[1][0].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Rhythm Moderate Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[1][1].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Rhythm Hard Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[1][2].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Rhythm Expert Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[1][3].Add(newChange);
                }

                while ((line = sr.ReadLine()).CompareTo("End Drum Beginner Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[2][0].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Drum Moderate Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[2][1].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Drum Hard Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[2][2].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Drum Expert Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[2][3].Add(newChange);
                }

                while ((line = sr.ReadLine()).CompareTo("End Guitar Beginner Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[0][0].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Guitar Moderate Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[0][1].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Guitar Hard Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[0][2].Add(newChange);
                }
                while ((line = sr.ReadLine()).CompareTo("End Guitar Expert Changes") != 0)
                {
                    int[] newChange = new int[2];
                    newChange[0] = int.Parse(line);
                    newChange[1] = int.Parse(sr.ReadLine());
                    changedNotes[0][3].Add(newChange);
                }
                
                // Debugging -- Generic values
                
                /*
                difficulty[0] = 0.2f;
                difficulty[1] = 0.4f;
                difficulty[2] = 0.6f;
                difficulty[3] = 0.81f;
                
                // Fortunate Son trackIDs
                VocalTracks.Add(1);
                RhythmTracks.Add(3);
                DrumTracks.Add(6);
                GuitarTracks.Add(4);
                */
            }
            public UserMods()
            {
            }
            public List<int[]> GetChangedNotes(int track, int difficulty)
            {
                return changedNotes[track][difficulty]; // Guitar, Rhythm, Drums, Vocals in that order. Corresponds to Player.INSTRUMENTS
                /*
                if (GuitarTracks.Contains(track))
                    return changedNotes[0];
                else if (RhythmTracks.Contains(track))
                    return changedNotes[1];
                else if (DrumTracks.Contains(track))
                    return changedNotes[2];
                else if (VocalTracks.Contains(track))
                    return changedNotes[3];
                else
                    return null;
                 */
            }
            public void DumpToFile()
            {
                // Load data from file
                FileStream file = new FileStream(filename, FileMode.Create, FileAccess.Write);
                StreamWriter sw = new StreamWriter(file);

                sw.WriteLine(difficulty[0]);
                sw.WriteLine(difficulty[1]);
                sw.WriteLine(difficulty[2]);
                sw.WriteLine(difficulty[3]);
                sw.WriteLine("End Difficulty/Seed Values");

                foreach (int i in VocalTracks)
                    sw.WriteLine(i);
                sw.WriteLine("End Vocal Tracks");
                foreach (int i in RhythmTracks)
                    sw.WriteLine(i);
                sw.WriteLine("End Rhythm Tracks");
                foreach (int i in DrumTracks)
                    sw.WriteLine(i);
                sw.WriteLine("End Drum Tracks");
                foreach (int i in GuitarTracks)
                    sw.WriteLine(i);
                sw.WriteLine("End Guitar Tracks");

                for (int i = 0; i < 4; i++)
                {
                    string instrument = i == 0 ? "Vocal" : i == 1 ? "Rhythm" : i == 2 ? "Drum" : "Guitar";

                    for (int j = 0; j < 4; j++)
                    {
                        string level = j == 0 ? "Beginner" : j == 1 ? "Moderate" : j == 2 ? "Hard" : "Expert";
                        if(changedNotes[i][j] != null) // This is a hack, I'm not sure why [0][0] == null here
                        foreach (int[] i_a in changedNotes[i][j])
                        {
                            sw.WriteLine(i_a[0]);
                            sw.WriteLine(i_a[1]);
                        }
                        sw.WriteLine("End " + instrument + " " + level + " Changes");
                    }
                }
                sw.Close();
            }
        }

        // File data
        private int[] fileData; // Contains each byte of the file. Stored in ints for easier data manipulation
        private int fdIndex; // Index into fileData
        private long curFileSize;
        public List<long> markers;

        // File values that apply to all tracks
        private int formatType;
        public int ticksPerBeat;
        public int timeCode;
        public int ticksPerFrame;
        public int totalTracks;
        public int[] totalNoteOnEvents;

        public Track[] allTracks;
        private List<NoteX> allNotes;
        public List<NoteX> AllNotes { get { return allNotes; } }

        // Constructor
        public MidiFile()
        {
            formatType = -1;
            ticksPerFrame = -1;
            ticksPerBeat = -1;
            timeCode = -1;
            markers = new List<long>();
        }
        // File Parsing functions
        public bool GenerateNotesFromFile(String filename, List<int> tracks, float difficulty)
        {
            if (!OpenFile(filename))
                return false;
            if (!ParseHeader())
                return false;
            if (!ParseTracks())
                return false;
            if (!CalculateAbsTimes())
                return false;
            if (!GenerateFormattedNotes(tracks, difficulty))
                return false;
            return true;
        }
        private bool OpenFile(String filename)
        {
            FileStream fs;
            try
            {
                fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            }
            catch (Exception)
            {
                return false;
            }
            curFileSize = fs.Length;
            fileData = new int[curFileSize];
            for (int i = 0; i < curFileSize; i++)
                fileData[i] = fs.ReadByte();

            // Each byte is now in fileData[]
            return true;
        }
        private int ParseVariableLength()
        {
            //
            // This function returns the value of the variable length value and increments fdIndex by the appropriate amount
            //

            int returnVal;
            // Parse dT (variable length, max of 4 bytes)
            if ((fileData[fdIndex] & 0x80) == 0)
            {
                // 1 byte length
                returnVal = fileData[fdIndex++] & 0x7F;
            }
            else if ((fileData[fdIndex + 1] & 0x80) == 0)
            {
                // 2 byte length
                returnVal = fileData[fdIndex++] & 0x7F;
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
            }
            else if ((fileData[fdIndex + 2] & 0x80) == 0)
            {
                // 3 byte length
                returnVal = fileData[fdIndex++] & 0x7F;
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
            }
            else
            {
                // 4 byte length
                returnVal = fileData[fdIndex++] & 0x7F;
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
                returnVal <<= 7;
                returnVal |= (fileData[fdIndex++] & 0x7F);
            }
            return returnVal;
        }
        private bool ParseHeader()
        {
            // Verify that chunk ID == "MThd" == 0x4D546864
            if (fileData[0] != 0x4D) return false;
            if (fileData[1] != 0x54) return false;
            if (fileData[2] != 0x68) return false;
            if (fileData[3] != 0x64) return false;

            int chunkSize = (fileData[4] << 24) | (fileData[5] << 16) | (fileData[6] << 8) | (fileData[7]);
            if (chunkSize != 6) return false;

            formatType = (fileData[8] << 8) | (fileData[9]);
            if (formatType < 0 || formatType > 2) return false;

            totalTracks = (fileData[10] << 8) | (fileData[11]);
            totalNoteOnEvents = new int[totalTracks];

            if ((fileData[12] & 0x80) == 0)
            {
                // Ticks per beat
                ticksPerBeat = (((fileData[12] & 0x7F) << 8) | fileData[13]);
            }
            else
            {
                // Frames per second
                int maskedOff = fileData[12] & 0x7F;
                if ((maskedOff != 24) && (maskedOff != 25) && (maskedOff != 29) && (maskedOff != 30))
                    return false;
                timeCode = maskedOff;
                ticksPerFrame = fileData[13];
            }
            if (formatType == 2)
                return false;
            return true;
        }
        private bool ParseTracks()
        {
            allTracks = new Track[totalTracks];
            fdIndex = 14; // fdIndex is the index into fileData. 14 is the 1st byte of the first track

            char[] cFileData = new char[fileData.Length];
            for (int x = 0; x < fileData.Length; x++)
                cFileData[x] = (char)(fileData[x] & 0xFF);

            for (int i = 0; i < totalTracks; i++)
            {
                allTracks[i] = new Track();
                int curTickCount = 0;

                allTracks[i].trackID = ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();

                if (!allTracks[i].trackID.Equals("MTrk"))
                    throw new System.NotImplementedException();

                // Parse track size
                int trackSize = fileData[fdIndex++];
                trackSize <<= 8;
                trackSize |= fileData[fdIndex++];
                trackSize <<= 8;
                trackSize |= fileData[fdIndex++];
                trackSize <<= 8;
                trackSize |= fileData[fdIndex++];

                allTracks[i].size = trackSize;

                // fdIndex now points to the first event in the current track
                // Parse Track Event Data (organized in chunks)
                int nextTrack = fdIndex + trackSize;
                int lastEventType = -1, lastChannel = -1;
                while (fdIndex < nextTrack)
                {
                    int dT = ParseVariableLength();
                    curTickCount += dT;

                    int eventType, channel;

                    if ((fileData[fdIndex] >> 4) < 0x8)
                    {
                        if (lastEventType == -1 || lastChannel == -1)
                            throw new System.NotImplementedException();
                        // This is a running event
                        eventType = lastEventType;
                        channel = lastChannel;
                    }
                    else
                    {
                        // Parse event type, then the corresponding parameters
                        eventType = fileData[fdIndex] >> 4; // Higher 4 bits
                        channel = fileData[fdIndex++] & 0x0F; // Lower 4 bits
                        if (eventType != 0xF || channel != 0xF)
                        {
                            lastEventType = eventType;
                            lastChannel = channel;
                        }
                    }
                    switch (eventType)
                    {
                        case (0x8): // Note off
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]);
                            fdIndex += 2;
                            break;
                        case (0x9): // Note on
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]);
                            fdIndex += 2;
                            totalNoteOnEvents[i]++;
                            break;
                        case (0xA): // Note aftertouch
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]);
                            fdIndex += 2;
                            break;
                        case (0xB): // Controller
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]);
                            fdIndex += 2;
                            break;
                        case (0xC): // Program change
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex++], -1);
                            break;
                        case (0xD): // Channel aftertouch
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex++], -1);
                            break;
                        case (0xE): // Pitch blend
                            allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]);
                            fdIndex += 2;
                            break;
                        case (0xF): // Variable lengths
                            switch (channel)
                            {
                                case (0x0): // SysEx Event
                                    break;
                                case (0x7): // SysEx Event
                                    break;
                                case (0x8): // Timing clock used when synchronization is required
                                    break;
                                case (0xA): // Start current sequence
                                    break;
                                case (0xB): // Continue a stopped sequence where left off
                                    break;
                                case (0xC): // Stop a sequence
                                    break;
                                case (0xF): // Meta-Event
                                    // Parse meta-event command    
                                    int command = fileData[fdIndex++];
                                    int length = ParseVariableLength();
                                    fdIndex += length; // Skip over these events, or index backward if necessary
                                    switch (command)
                                    {
                                        case (0x00): // Sequence number
                                            break;
                                        case (0x01): // Text event
                                            String textEvent = "";
                                            for (int x = 0; x < length; x++)
                                                textEvent += ((char)fileData[fdIndex - length + x]);
                                            break;
                                        case (0x02): // Copyright notice
                                            break;
                                        case (0x03): // Sequence / Track name
                                            allTracks[i].name = "";
                                            for (int x = 0; x < length; x++)
                                                allTracks[i].name += ((char)fileData[fdIndex - length + x]);
                                            break;
                                        case (0x04): // Instument name
                                            allTracks[i].instrument = "";
                                            for (int x = 0; x < length; x++)
                                                allTracks[i].instrument += ((char)fileData[fdIndex - length + x]);
                                            break;
                                        case (0x05): // Lyrics
                                            break;
                                        case (0x06): // Marker
                                            break;
                                        case (0x07): // Cue Point
                                            break;
                                        case (0x20): // MIDI Channel Prefix
                                            break;
                                        case (0x2F): // End of Track
                                            allTracks[i].AddMetaEvent(curTickCount, -1, command, -1, -1, -1);
                                            fdIndex = nextTrack; // Sometimes there is garbage at the end of tracks
                                            break;
                                        case (0x51): // Set Tempo
                                            allTracks[i].AddMetaEvent(curTickCount, -1, command, fileData[fdIndex - 3], fileData[fdIndex - 2], fileData[fdIndex - 1]);
                                            break;
                                        case (0x54): // SMPTE Offset
                                            break;// throw new System.NotImplementedException(); // Use this to get better barlines[]
                                        case (0x58): // Time Signature
                                            break;// throw new System.NotImplementedException();
                                        case (0x59): // Key Signature
                                            break;
                                        case (0x7F): // Sequencer Specific
                                            break;
                                        case (0xF8): // Timing clock used when synchronization is required
                                            break;
                                        case (0xFA): // Start current sequence
                                            break;
                                        case (0xFB): // Continue a stopped sequence where left off
                                            break;
                                        case (0xFC): // Stop a sequence
                                            break;
                                        default:
                                            break;// throw new System.NotImplementedException();
                                    }
                                    break; // from switch(channel)
                            }
                            break; // from switch(eventType)
                        default:
                            throw new System.NotImplementedException();
                    }
                }
                if (fdIndex != nextTrack)
                    throw new System.NotImplementedException();
            }
            CalculateAbsTimes();
            return true;
        }
        private bool CalculateAbsTimes()
        {
            int[] curEventIndex = new int[totalTracks];
            for (int i = 0; i < totalTracks; i++)
                curEventIndex[i] = 0;

            long curTickCount = 0;
            long curTimeUS = 0;
            long USperTick = -1;

            if (ticksPerBeat != -1)
            {
            }
            else if (ticksPerFrame != -1)
            {
                if (timeCode == -1)
                    return false;
            }
            else
                return false; // should never be called

            long nextMarker = 0;
            bool continueLoop = true;
            while (continueLoop)
            {
                int earliestElement = -1;
                long earliestTickCount = -1;

                // Find the earliest event (must be in a valid track with more unhandled events)
                for (int i = 0; i < totalTracks; i++)
                {
                    if (curEventIndex[i] >= allTracks[i].allEvents.Count)
                        continue;
                    if (earliestTickCount == -1 || allTracks[i].allEvents[curEventIndex[i]].absTickCount < earliestTickCount)
                    {
                        earliestTickCount = allTracks[i].allEvents[curEventIndex[i]].absTickCount;
                        earliestElement = i;
                    }
                }

                // Get a handle on the Event object, and increment the curEventIndex for the object's Track.
                Track.Event earliestEvent = allTracks[earliestElement].allEvents[curEventIndex[earliestElement]++];

                if (earliestTickCount <= nextMarker)
                {
                    markers.Add(curTimeUS + (nextMarker - curTickCount) * USperTick);
                    nextMarker += ticksPerBeat;
                }

                // Increase the absolute time upto the earliest event, converting from Ticks to MS
                long dT = earliestTickCount - curTickCount;
                long dTms = dT * USperTick;
                curTickCount += dT;
                curTimeUS += dTms;

                // Check for Set Tempo MetaEvent (command number 0x51)
                if (earliestEvent.eventType == Track.MetaCommandToEventType(0x51))
                {
                    long USperBeat = (earliestEvent.channel << 16) | (earliestEvent.param1 << 8) | (earliestEvent.param2);
                    USperTick = USperBeat / ticksPerBeat;
                }
                else if (USperTick == -1 && dT != 0)
                    break;

                earliestEvent.absTimeMS = curTimeUS / 1000;

                // Repeat loop while there is an outstanding event
                continueLoop = false;
                for (int i = 0; i < totalTracks; i++)
                {
                    if (allTracks[i].allEvents.Count > curEventIndex[i])
                    {
                        continueLoop = true;
                        break;
                    }
                }
            }
            return true;
        }
        // Note/Chord Generation Utilities
        private void SortChord(NoteX[] notes)
        {
            for (int a = 1; a < 5; a++)
            {
                if (notes[a] == null)
                    break;
                int b = a;
                while (b > 0 && notes[b].type < notes[b - 1].type)
                {
                    NoteX temp = notes[b];
                    notes[b] = notes[b - 1];
                    notes[b - 1] = temp;
                    b--;
                }
            }
        }
        private bool ChordIsIn(NoteX[] chord, List<NoteX[]> collection)
        {
            foreach (NoteX[] na in collection)
            {
                int x;
                for (x = 0; x < 5; x++)
                {
                    if (chord[x] == null || na[x] == null)
                        return chord[x] == na[x];
                    if (na[x].type != chord[x].type)
                        break;
                }
                if (x == 5)
                    return true;
            }
            return false;
        }
        private bool areEqual(NoteX[] a, NoteX[] b)
        {
            if (a[0].type != b[0].type)
                return false;
            if (a[1].type != b[1].type)
                return false;
            if (a[2] == null)
                return b[2] == null;
            else if (b[2] == null)
                return false;
            else
                return a[2].type == b[2].type;
        }
        private int rateDifficulty(NoteX[] a, NoteX[] b)
        {

            return rateDifficulty((int)a[0].type, (int)a[1].type, (int)a[2].type, (int)b[0].type, (int)b[1].type, (int)b[2].type);
            /////////////////////
            /*
                        // Coordinated with BuildChordValues()
                        // Must return <= difficulty level + 2 (Must rate <=2 for beginner, <=3 for moderate, or <=4 for hard/expert, the max rating)
                        //
                        if (areEqual(a, b))
                            return 0;

                        if (a.Length == 3 && b.Length == 2)
                        {
                            NoteX[] c = b;
                            b = a;
                            a = c;
                        }

                        int returnVal;

                        if (a.Length == 2 && b.Length == 3)
                        {
                            int pairCount = 0;
                            if (a[0].type == b[0].type || a[0].type == b[1].type || a[0].type == b[2].type) pairCount++;
                            if (a[1].type == b[0].type || a[1].type == b[1].type || a[1].type == b[2].type) pairCount++;

                            // The fewer pairs the harder the transition. Beginner is not considered here, Moderate has >= 1 pair, Hard+Expert can have 0
                            returnVal = pairCount == 0 ? 4 : pairCount == 1 ? 3 : 1;
                        }
                        else if (a.Length == 2 && b.Length == 2)
                        {
                            if (a[0].type == b[0].type || a[0].type == b[1].type || a[1].type == b[0].type || a[1].type == b[1].type)
                            {
                                // only 1 note difference
                                returnVal = 1;
                            }
                            else if ((a[0].type == 1 || a[1].type == 16) && (b[0].type == 1 || b[1].type == 16))
                            {
                                // both notes are different, and a hand shift is involved, only for Hard+Expert
                                returnVal = 3;
                            }
                            else
                            {
                                // different notes, but no hand shift necessary
                                returnVal = 2;
                            }
                        }
                        else
                        {
                            // Both are 3 note chords
                            if (a.Length != 3 || b.Length != 3)
                                throw new System.NotSupportedException();

                            int pairCount = 0;
                            if (a[0].type == b[0].type || a[0].type == b[1].type || a[0].type == b[2].type) pairCount++;
                            if (a[1].type == b[0].type || a[1].type == b[1].type || a[1].type == b[2].type) pairCount++;
                            if (a[2].type == b[0].type || a[2].type == b[1].type || a[2].type == b[2].type) pairCount++;

                            returnVal = pairCount == 1 ? 4 : 3;
                        }
                        if (returnVal < 0 || returnVal > 4)
                            throw new System.NotSupportedException();
                        return returnVal;
             * */
        }
        private int rateDifficulty(int a0, int a1, int a2, int b0, int b1, int b2)
        {
            // Coordinated with BuildChordValues()
            // Must return <= difficulty level + 2 (Must rate <=2 for beginner, <=3 for moderate, or <=4 for hard/expert, the max rating)
            //
            if ((a0 == b0) && (a1 == b1) && (a2 == b2))
                return 0;
            if (a0 == 0 && a1 == 0 && a2 == 0)
                return 0;
            if (b0 == 0 && b1 == 0 && b2 == 0)
                return 0;

            if ((a2 != 0) && (b2 == 0))
            {
                int t = a0;
                a0 = b0;
                b0 = t;
                t = a1;
                a1 = b1;
                b1 = t;
                t = a2;
                a2 = b2;
                b2 = t;
            }

            int returnVal;

            if ((a2 == 0) && (b2 != 0))
            {
                int pairCount = 0;
                if (a0 == b0 || a0 == b1 || a0 == b2) pairCount++;
                if (a1 == b0 || a1 == b1 || a1 == b2) pairCount++;

                // The fewer pairs the harder the transition. Beginner is not considered here, Moderate has >= 1 pair, Hard+Expert can have 0
                returnVal = pairCount == 0 ? 4 : pairCount == 1 ? 3 : 1;
            }
            else if ((a2 == 0) && (b2 == 0))
            {
                if (a0 == b0 || a0 == b1 || a1 == b0 || a1 == b1)
                {
                    // only 1 note difference
                    returnVal = 1;
                }
                else if ((a0 == 1 || a1 == 16) && (b0 == 1 || b1 == 16))
                {
                    // both notes are different, and a hand shift is involved, only for Hard+Expert
                    returnVal = 3;
                }
                else
                {
                    // different notes, but no hand shift necessary
                    returnVal = 2;
                }
            }
            else
            {
                // Both are 3 note chords
                if ((a2 == 0) || (b2 == 0))
                    throw new System.NotSupportedException();

                int pairCount = 0;
                if ((a0 == b0) || (a0 == b1) || (a0 == b2)) pairCount++;
                if ((a1 == b0) || (a1 == b1) || (a1 == b2)) pairCount++;
                if ((a2 == b0) || (a2 == b1) || (a2 == b2)) pairCount++;

                returnVal = pairCount == 1 ? 4 : 3;
            }
            if (returnVal < 0 || returnVal > 4)
                throw new System.NotSupportedException();
            return returnVal;
        }
        private bool EndTypesMatch(NoteX[] a, NoteX[] b)
        {
            if ((a[2] == null) ^ (b[2] == null)) // a 3 note chord and a 2 note chord
                return false;
            if (a[2] == null) // both are 2 note chords
                return a[1].endtype == b[1].endtype && a[0].endtype == b[0].endtype;
            else // both are 3 note chords
                return a[2].endtype == b[2].endtype && a[1].endtype == b[1].endtype && a[0].endtype == b[0].endtype;
        }
        // Note/Chord Generation
        private bool GenerateFormattedNotes(List<int> tracks, float difficulty)
        {
            int totalValidTracks = 0;
            int i;
            for (i = 0; i < allTracks.Length && totalValidTracks < tracks.Count; i++)
                if (totalNoteOnEvents[i] != 0)
                    totalValidTracks++;
            if (tracks.Count > totalValidTracks)
                return false;
            // else we know totalValidTracks == tracks.Length;

            // Fill allNotes with real pitches (these are invalid as NoteX->types)
            List<NoteX[]> allChords;
            List<NoteX[]> diffChords;
            List<NoteX> diffNotes;

            GetAllNotesAndChords(tracks, out allNotes, out allChords, out diffNotes, out diffChords, difficulty);
            // All unique notes and chords are now identified and built according to DENSITY_MIN_TIME values determined by difficulty.

            // Build a pseudo-random, salted array of unique chords
            List<int[]> seededChords;
            Random seededRandom = InitSeededChords(out seededChords, difficulty);
            // Now map each unique chord to 2or3 NoteXs, from the values defined in storedChords[]

            List<NoteX[]> chordValues = BuildChordValues(ref allChords, ref diffChords, ref seededChords, difficulty);

            // chordValues[] now contains for each unique chord: The 5 notes of the chord and the 3 NoteX->types for the visible notes.
            ConvertChordsToValidTypes(ref allChords, chordValues);

            // Assign each note a unique value
            NoteX[][] noteValues = BuildNoteValues(ref allNotes, ref diffNotes, ref allChords, difficulty);

            // Convert AllNotes[] to unique values
            foreach (NoteX n in allNotes)
            {
                foreach (NoteX[] nValues in noteValues)
                {
                    if (n.type == nValues[0].type)
                    {
                        if (n.Equals(nValues[0]))
                        {
                            // Watch out for unintentional aliasing
                            nValues[0] = new NoteX(nValues[0].time, nValues[0].type);
                            nValues[0].endtype = n.endtype; // Currently, should always be 0
                        }
                        n.endtype = n.type;
                        n.type = nValues[1].type;
                        break;
                    }
                }
            }

            // Remove back to back notes/chords that sound different
            RationalizeNotesAndChords(ref allNotes, ref allChords, ref diffNotes, ref diffChords, difficulty, ref seededRandom);
            // Combine into one List of notes (allNotes)
            MergeChordsIntoNotes(ref allChords, ref allNotes);

            /*// Fix last note issue by adding an empty note 500ms after the last one
            NoteX lastNote = new NoteX();
            lastNote.time = allNotes[allNotes.Count].time + 500;
            allNotes[allNotes.Count].visible[0] = SongData.NoteSet.VIS_STATE.INVISIBLE;
            allNotes.Add(lastNote);*/
            return true;
        }
        private void GetAllNotesAndChords(List<int> tracks, out List<NoteX> allNotes, out List<NoteX[]> allChords, out List<NoteX> diffNotes, out List<NoteX[]> diffChords, float difficulty)
        {
            allNotes = null;
            allChords = null;
            diffChords = new List<NoteX[]>();
            diffNotes = new List<NoteX>();

            List<NoteX>[] allAllNotes = new List<NoteX>[tracks.Count];
            List<NoteX[]>[] allAllChords = new List<NoteX[]>[tracks.Count];
            DensityTimes densityTimes = new DensityTimes(difficulty);

            for (int ii = 0; ii < tracks.Count; ii++)
            {
                allNotes = new List<NoteX>();
                allChords = new List<NoteX[]>();

                int trackID = tracks[ii];

                if (trackID == 0) // TrackIDs start at 1
                    throw new System.NotSupportedException();

                NoteX last = null;
                bool chordStarted = false;
                int priorDeadTracks = 0;
                for (int i = 0; i < trackID + priorDeadTracks; i++)
                    if (totalNoteOnEvents[i] == 0)
                        priorDeadTracks++;
                trackID += priorDeadTracks - 1;

                foreach (Track.Event e in allTracks[trackID].allEvents)
                {
                    if (e.eventType == 0x9) // Note On event
                    {
                        ProcessNoteOn(allNotes, ref allChords, diffNotes, ref diffChords, densityTimes, ref last, ref chordStarted, e);
                    }
                    else if (e.eventType == 0x8) // Note off event
                    {
                        // Find the note this corresponds to
                        int lastIndex = allNotes.Count - 1;
                        while (lastIndex >= 0 && (allNotes[lastIndex].type != (ulong)e.param1))
                            lastIndex--;
                        if (lastIndex >= 0)
                        {
                            allNotes[lastIndex].length = (uint)e.absTimeMS - allNotes[lastIndex].time;
                        }
                    }
                }
                if (chordStarted) // The last note was part of an unfinished chord.
                    ProcessNewChord(ref allChords, ref diffChords, densityTimes);

                allAllNotes[ii] = allNotes;
                allAllChords[ii] = allChords;
            }

            //
            // Merge allAllNotes[][] into allNotes[]
            //
            int[] indexs = new int[allAllNotes.Length]; // The current index for each track in allAllNotes[]
            allNotes = new List<NoteX>();
            for (int i = 0; i < indexs.Length; i++)
                indexs[i] = 0;

            bool done = false;
            bool noNotes = true;
            for (int i = 0; i < allAllNotes.Length; i++)
                if (allAllNotes[i].Count != 0)
                    noNotes = false;

            if (!noNotes)
            {
                do
                {
                    // Pick the earliest note
                    int earliestIndex = 0;
                    while (allAllNotes[earliestIndex].Count == 0)
                        earliestIndex++;
                    for (int i = earliestIndex + 1; i < indexs.Length; i++)
                    {
                        if (allAllNotes[i].Count != 0 && allAllNotes[i][0].time < allAllNotes[earliestIndex][0].time)
                            earliestIndex = i;
                    }

                    // Add the earliest note to allNotes
                    allNotes.Add(allAllNotes[earliestIndex][0]);
                    allAllNotes[earliestIndex].RemoveAt(0);
                    //indexs[earliestIndex]++;
                    // Check for end condition
                    done = true;
                    for (int i = 0; i < indexs.Length; i++)
                    {
                        if (indexs[i] != allAllNotes[i].Count)
                            done = false;
                    }
                } while (!done);
            }

            done = CleanUpAllNotes(ref allChords, diffNotes, diffChords, allAllChords, ref indexs, done);
        }

        private static bool CleanUpAllNotes(ref List<NoteX[]> allChords, List<NoteX> diffNotes, List<NoteX[]> diffChords, List<NoteX[]>[] allAllChords, ref int[] indexs, bool done)
        {
            // Merge allAllChords[] into allChords
            indexs = new int[allAllChords.Length];
            for (int i = 0; i < indexs.Length; i++)
                indexs[i] = 0;
            allChords = new List<NoteX[]>();

            bool noChords = true;
            for (int i = 0; i < allAllChords.Length; i++)
                if (allAllChords[i].Count != 0)
                    noChords = false;
            if (!noChords)
            {
                do
                {
                    // Pick the earliest chord
                    int earliestIndex = 0;
                    while (allAllChords[earliestIndex].Count == 0)
                        earliestIndex++;
                    for (int i = earliestIndex + 1; i < indexs.Length; i++)
                    {
                        if (allAllChords[i].Count != 0 && allAllChords[i][0][0].time < allAllChords[earliestIndex][0][0].time)
                            earliestIndex = i;
                    }

                    // Add the earliest chord to allChords
                    allChords.Add(allAllChords[earliestIndex][0]);
                    allAllChords[earliestIndex].RemoveAt(0);

                    // Check for end condition
                    done = true;
                    for (int i = 0; i < indexs.Length; i++)
                    {
                        if (indexs[i] != allAllChords[i].Count)
                            done = false;
                    }
                } while (!done);
            }
            // Sort diffNotes by time
            for (int i = 1; i < diffNotes.Count; i++)
            {
                int j = i;
                while (j > 0)
                {
                    if (diffNotes[j].time < diffNotes[j - 1].time)
                    {
                        NoteX swap = diffNotes[j];
                        diffNotes[j] = diffNotes[j - 1];
                        diffNotes[j - 1] = swap;
                    }
                    j--;
                }
            }

            // Sort diffChords by time
            for (int i = 1; i < diffChords.Count; i++)
            {
                int j = i;
                while (j > 0)
                {
                    if (diffChords[j][0].time < diffChords[j - 1][0].time)
                    {
                        NoteX[] swap = diffChords[j];
                        diffChords[j] = diffChords[j - 1];
                        diffChords[j - 1] = swap;
                    }
                    j--;
                }
            }
            return done;
        }
        private void ProcessNoteOn(List<NoteX> allNotes, ref List<NoteX[]> allChords, List<NoteX> diffNotes, ref List<NoteX[]> diffChords, DensityTimes densityTimes, ref NoteX last, ref bool chordStarted, Track.Event e)
        {
            NoteX cur = new NoteX((uint)e.absTimeMS, (ulong)e.param1);

            if (last != null && last.time == cur.time)
            {
                // This note is part of an unfinished chord
                if (cur.type == last.type)
                {
                    // Ignore a redundant note. (Valid midis may still be poorly written)
                }
                else if (!chordStarted)
                {
                    allNotes.Remove(last);
                    allChords.Add(new NoteX[5]);
                    allChords[allChords.Count - 1][0] = new NoteX(last.time, last.type);
                    allChords[allChords.Count - 1][1] = new NoteX(cur.time, cur.type);
                    chordStarted = true;
                }
                else for (int i = 2; i < 4; i++)
                    {
                        if (allChords[allChords.Count - 1][i] == null)
                        {
                            allChords[allChords.Count - 1][i] = new NoteX(cur.time, cur.type);
                            break;
                        }
                    }
            }
            else
            {
                // This is a new time slot. It may be a single note or the first note of a new chord.
                if (chordStarted)
                {
                    // Finished a chord last iteration. Now we know, so handle it here.
                    chordStarted = false;
                    ProcessNewChord(ref allChords, ref diffChords, densityTimes);
                }

                // Now check the current note for validity
                bool valid = true;
                if (allNotes.Count != 0)
                {
                    if (cur.type == allNotes[allNotes.Count - 1].type)
                        valid = cur.time - allNotes[allNotes.Count - 1].time >= densityTimes.nx_rpt;
                    else
                        valid = cur.time - allNotes[allNotes.Count - 1].time >= densityTimes.nx_nx;
                }
                if (valid)
                {
                    if (allChords.Count == 0 || cur.time - allChords[allChords.Count - 1][0].time > densityTimes.ch_nx)
                    {
                        allNotes.Add(cur);//new NoteX(cur.time, cur.type));

                        // Check diffNotes and add cur if it's not in there yet.
                        int i;
                        for (i = 0; i < diffNotes.Count; i++)
                            if (diffNotes[i].type == cur.type)
                                break;
                        if (i == diffNotes.Count)
                            diffNotes.Add(cur);//new NoteX(cur.time, cur.type));
                    }
                }
            }
            last = cur;
        }
        private void ProcessNewChord(ref List<NoteX[]> allChords, ref List<NoteX[]> diffChords, DensityTimes d)
        {
            SortChord(allChords[allChords.Count - 1]);

            bool valid = true;
            // Check if it's been long enough to have another chord
            if (allChords.Count > 1)
            {
                if (areEqual(allChords[allChords.Count - 1], allChords[allChords.Count - 2]))
                    valid = allChords[allChords.Count - 1][0].time - allChords[allChords.Count - 2][0].time >= d.ch_rpt;
                else
                    valid = allChords[allChords.Count - 1][0].time - allChords[allChords.Count - 2][0].time >= d.ch_ch;
            }

            while (allNotes.Count != 0 && allChords[allChords.Count - 1][0].time - allNotes[allNotes.Count - 1].time < d.nx_ch)
                allNotes.RemoveAt(allNotes.Count - 1);

            if (!valid)
                allChords.RemoveAt(allChords.Count - 1);
            else if (!ChordIsIn(allChords[allChords.Count - 1], diffChords))
                diffChords.Add(allChords[allChords.Count - 1]);//((NoteX[])allChords[allChords.Count - 1].Clone());
        }
        private Random InitSeededChords(out List<int[]> seededChords, float _difficulty)
        {
            int diff = _difficulty < 0.25 ? 0 : _difficulty < 0.5 ? 1 : _difficulty < 0.75 ? 2 : _difficulty < 0.95 ? 3 : 4;
            int seed = (int)(_difficulty * 10000) % 100;
            Random rand = new Random(seed);

            seededChords = new List<int[]>(diff == 0 ? 5 : diff == 1 ? 8 : diff == 2 ? 12 : 16);
            for (int i = 0; i < seededChords.Capacity; i++)
                seededChords.Add(new int[3]);

            seededChords[0][0] = 1;
            seededChords[0][1] = 2;
            seededChords[0][2] = 0;

            seededChords[1][0] = 2;
            seededChords[1][1] = 4;
            seededChords[1][2] = 0;

            seededChords[2][0] = 4;
            seededChords[2][1] = 8;
            seededChords[2][2] = 0;

            seededChords[3][0] = 1;
            seededChords[3][1] = 4;
            seededChords[3][2] = 0;

            seededChords[4][0] = 2;
            seededChords[4][1] = 8;
            seededChords[4][2] = 0;

            if (diff > 0)
            {
                // Moderate
                seededChords[5][0] = 1;
                seededChords[5][1] = 8;
                seededChords[5][2] = 0;

                seededChords[6][0] = 1;
                seededChords[6][1] = 2;
                seededChords[6][2] = 4;

                seededChords[7][0] = 2;
                seededChords[7][1] = 4;
                seededChords[7][2] = 8;

            }
            if (diff > 1)
            {
                // Hard
                seededChords[8][0] = 8;
                seededChords[8][1] = 16;
                seededChords[8][2] = 0;

                seededChords[9][0] = 4;
                seededChords[9][1] = 16;
                seededChords[9][2] = 0;

                seededChords[10][0] = 2;
                seededChords[10][1] = 16;
                seededChords[10][2] = 0;

                seededChords[11][0] = 4;
                seededChords[11][1] = 8;
                seededChords[11][2] = 16;
            }
            if (diff > 2)
            {
                // Expert
                seededChords[12][0] = 1;
                seededChords[12][1] = 2;
                seededChords[12][2] = 8;

                seededChords[13][0] = 1;
                seededChords[13][1] = 4;
                seededChords[13][2] = 8;

                seededChords[14][0] = 2;
                seededChords[14][1] = 4;
                seededChords[14][2] = 16;

                seededChords[15][0] = 2;
                seededChords[15][1] = 8;
                seededChords[15][2] = 16;
            }

            // Pseudo-randomize the initial chord ordering
            for (int i = 0; i < seededChords.Count; i++)
            {
                int r = rand.Next(seededChords.Count - 1);
                int[] swap = seededChords[r];
                seededChords[r] = seededChords[i];
                seededChords[i] = swap;
            }

            return rand;
        }
        private List<NoteX[]> BuildChordValues(ref List<NoteX[]> allChords, ref List<NoteX[]> diffChords, ref List<int[]> seededChords, float difficulty)
        {
            for (int i = 0; i < diffChords.Count; i++)
            {
                if (allChords.IndexOf(diffChords[i]) == -1) // Chord was removed from allChords or inadvertently added to diffChords
                    diffChords.RemoveAt(i--); // This may hide bugs, but is necessary in case some tracks have been merged and chords removed
            }
            List<NoteX[]> chordValues = new List<NoteX[]>();
            List<int[]> availableChords = new List<int[]>();
            DensityTimes densityTimes = new DensityTimes(difficulty);
            int diff = difficulty < 0.25 ? 0 : difficulty < 0.5 ? 1 : difficulty < 0.75 ? 2 : difficulty < 0.95 ? 3 : 4;

            foreach (NoteX[] na in diffChords)
            {
                if (availableChords.Count == 0)
                {
                    // All available unique chords in a song must be used before a 5-key chord will be used twice
                    foreach (int[] chord in seededChords)
                        availableChords.Add(chord);
                }

                // Put first 5 notes (real pitches) into newNA
                NoteX[] newNA = new NoteX[8];
                for (int x = 0; x < 5; x++)
                    newNA[x] = na[x] == null ? null : new NoteX(na[x].time, na[x].type);

                // Find the best fitting chord given the previous and next chords, transition time and difficulty

                // Determine the time between this chord and the last, and between this chord and the next
                // FIXME: this should be checking against the last Chord played in allChords, not diffChords
                int dtLast = diffChords.IndexOf(na) == 0 ? int.MaxValue : (int)(na[0].time - diffChords[diffChords.IndexOf(na) - 1][0].time);
                int dtNext = diffChords.IndexOf(na) == diffChords.Count - 1 ? int.MaxValue : (int)(diffChords[diffChords.IndexOf(na) + 1][0].time - na[0].time);
                int minDT = dtLast < dtNext ? dtLast : dtNext;
                // Determine max difficulty for this transition
                // This code should be coordinated with rateDifficulty() since these values are arbitrary
                int maxChordDiff;
                if (minDT < densityTimes.ch_ch * 1.5)
                {
                    maxChordDiff = diff > 2 ? 2 : diff;
                }
                else if (minDT < densityTimes.ch_ch * 2)
                {
                    maxChordDiff = diff > 2 ? 2 : diff + 1;
                }
                else
                {
                    maxChordDiff = diff > 2 ? 2 : diff + 2;
                }

                int chosenIndex = -1;
                int highestRating = -1;
                // maxChordDiff is >= 0 && <= 4
                ulong[] lastChord = new ulong[3];
                if (allChords.IndexOf(na) == 0)
                {
                    lastChord[0] = 0;
                    lastChord[1] = 0;
                    lastChord[2] = 0;
                }
                else
                {
                    for (int i = 0; i < lastChord.Length; i++)
                    {
                        int index = allChords.IndexOf(na) - 1;
                        lastChord[i] = allChords[index][i] == null ? 0 : allChords[index][i].type;
                    }
                }
                do
                {
                    if (availableChords.Count == 0)
                        throw new System.NotSupportedException();
                    for (int i = 0; i < availableChords.Count; i++)
                    {
                        int rating = rateDifficulty(availableChords[i][0], availableChords[i][1], availableChords[i][2], (int)lastChord[0], (int)lastChord[1], (int)lastChord[2]);
                        if (rating <= maxChordDiff && rating > highestRating)
                        {
                            // Found a valid chord transition, use it for this chord and remove it from availableChords if it's the hardest available transition
                            chosenIndex = i;
                            highestRating = rating;
                        }
                    }
                    maxChordDiff++; // Continue increasing until you find an acceptable chord--this will unfortunately place a hard transition in a short time
                } while (chosenIndex == -1);

                if (chosenIndex >= availableChords.Count)
                    throw new System.NotSupportedException();
                // Put chosen chord (NoteX pitches) into the last 3 slots of newNA, matching the corresponding 5 note chord
                for (int i = 0; i < 3; i++)
                    newNA[5 + i] = availableChords[chosenIndex][i] == 0 ? null : new NoteX(newNA[0].time, (ulong)availableChords[chosenIndex][i]);
                availableChords.RemoveAt(chosenIndex);
                if (newNA[5] == null || newNA[6] == null)
                    throw new System.NotSupportedException();
                chordValues.Add(newNA);
            }
            return chordValues;
        }
        private NoteX[][] BuildNoteValues(ref List<NoteX> allNotes, ref List<NoteX> diffNotes, ref List<NoteX[]> allChords, float difficulty)
        {
            int diff = difficulty < 0.25 ? 0 : difficulty < 0.5 ? 1 : difficulty < 0.75 ? 2 : difficulty < 0.95 ? 3 : 4;
            NoteX[][] noteValues = new NoteX[diffNotes.Count][];
            for (int i = 0; i < noteValues.Length; i++)
            {
                noteValues[i] = new NoteX[2];
                noteValues[i][0] = diffNotes[i];
            }

            int uniqueNoteCount = 0;
            NoteX[] storedNotes = new NoteX[5];
            for (int i = 0; i < diffNotes.Count; i++)
            {
                // Store the value for later assignment
                storedNotes[uniqueNoteCount++] = diffNotes[i];
                if (!(uniqueNoteCount == 5 || (uniqueNoteCount == 4 && diff < 2) || i == diffNotes.Count - 1))
                    continue;

                // Sort the accumulated unassigned notes
                bool swapped;
                if (uniqueNoteCount > 1) do
                    {
                        swapped = false;
                        for (int j = i - uniqueNoteCount + 1; j < i; j++)
                        {
                            if (diffNotes[j].type > diffNotes[j + 1].type)
                            {
                                NoteX swap = diffNotes[j];
                                diffNotes[j] = diffNotes[j + 1];
                                diffNotes[j + 1] = swap;
                                NoteX[] noteValueSwap = noteValues[j];
                                noteValues[j] = noteValues[j + 1];
                                noteValues[j + 1] = noteValueSwap;
                                swapped = true;
                            }
                        }
                    } while (swapped);

                // the relevent portion of diffNotes[] is now sorted
                ulong value = 1;
                for (int j = i - uniqueNoteCount + 1; j <= i; j++)
                {
                    // Assign each 
                    noteValues[j][1] = new NoteX(diffNotes[j].time, value);
                    noteValues[j][1].endtype = noteValues[j][0].type;
                    value <<= 1;
                }

                uniqueNoteCount = 0;
            }
            return noteValues;
        }
        private void MergeChordsIntoNotes(ref List<NoteX[]> allChordsNotCondensed, ref List<NoteX> allNotes)
        {
            // Condense Chords into a single note
            List<NoteX> allChords = new List<NoteX>();
            foreach (NoteX[] na in allChordsNotCondensed)
            {
                NoteX newNote = na[0];
                for (int i = 1; na[i] != null; i++)
                    newNote.type |= na[i].type;
                allChords.Add(newNote);                
            }
            // Merge AllChords[] into AllNotes[]
            List<NoteX> newList = new List<NoteX>();
            while (allNotes.Count != 0 || allChords.Count != 0)
            {
                if (allNotes.Count == 0)
                {
                    // All notes have been processed
                    //for (int b = 0; allChords[0][b] != null; b++)
                        newList.Add(allChords[0]);
                    allChords.RemoveAt(0);
                }
                else if (allChords.Count == 0)
                {
                    // All Chords have been processed
                    newList.Add(allNotes[0]);
                    allNotes.RemoveAt(0);
                }
                else if (allNotes[0].time > allChords[0].time)
                {
                    // The next chord comes before the next note
                    //for (int b = 0; allChords[0][b] != null; b++)
                        newList.Add(allChords[0]);
                    allChords.RemoveAt(0);
                }
                else
                {
                    newList.Add(allNotes[0]);
                    allNotes.RemoveAt(0);
                }
            }
            allNotes = newList;

            // Check for invalid notes
            for (int i = 0; i < allNotes.Count; i++)
            {
                NoteX n = allNotes[i];
                if (n.type > 31 || n.type < 0)
                    throw new System.NotSupportedException();
            }
        }
        private void ConvertChordsToValidTypes(ref List<NoteX[]> allChords, List<NoteX[]> chordValues)
        {
            // Convert AllChords[] to unique values
            foreach (NoteX[] na in allChords)
            {
                bool found = false;
                foreach (NoteX[] naValues in chordValues)
                {
                    int x;
                    for (x = 0; x < 5; x++)
                    {
                        if (na[x] == null || naValues[x] == null)
                        {
                            if (na[x] == naValues[x])
                                x = 5;
                            break;
                        }
                        if (na[x].type != naValues[x].type)
                        {
                            break;
                        }
                    }
                    if (x == 5)
                    {
                        // Found the corresponding chord
                        for (int y = 0; y < 5; y++)
                        {
                            if (na[y] == null)
                                break;
                            na[y].endtype = 1000 + na[y].type; // + 1000 signals that the note is part of a chord
                        }

                        na[0].type = naValues[5].type;
                        na[1].type = naValues[6].type;
                        na[2] = naValues[7] == null ? null : new NoteX(na[1].time, naValues[7].type);
                        na[3] = null;
                        na[4] = null;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    throw new System.NotSupportedException();
            }
        }
        private void RationalizeNotesAndChords(ref List<NoteX> allNotes, ref List<NoteX[]> allChords, ref List<NoteX> diffNotes, ref List<NoteX[]> diffChords, float difficulty, ref Random seededRNG)
        {
            if (allNotes.Count > 3)
            {
                NoteX last = allNotes[0], curr = allNotes[1];
                int maxNote = difficulty < 0.5 ? 3 : 4;
                bool replace = false;
                for (int i = 0; i < allNotes.Count; i++)
                {
                    NoteX next = allNotes[i];
                    if (replace)
                    {
                        if (last.endtype == curr.endtype)
                            curr.type = last.type;
                        else
                            replace = false;
                    }

                    if (last.type == curr.type && last.endtype != curr.endtype)
                    {
                        // Found an irrational note. Pick a new spot for it with seededRNG
                        ulong value;
                        /*
                        value = (ulong)1 << seededRNG.Next(0, maxNote);
                        while(value == last.type || value == next.type)
                            value = (ulong) 1 << seededRNG.Next(0, maxNote);
                        */
                        if (last.type == 1)
                            value = (ulong)(next.type == 2 ? 4 : 2);
                        else if (next.type == 1)
                            value = (ulong)(last.type == 2 ? 4 : 2);
                        else if (last.type == (ulong)1 << maxNote)
                            value = (ulong)(next.type == (ulong)1 << (maxNote - 1) ? 1 << (maxNote - 2) : 1 << (maxNote - 1));
                        else if (next.type == (ulong)1 << maxNote)
                            value = (ulong)(last.type == (ulong)1 << (maxNote - 1) ? 1 << (maxNote - 2) : 1 << (maxNote - 1));
                        else if (curr.endtype < last.endtype)
                            value = last.type >> (next.type == last.type >> 1 ? 2 : 1);
                        else
                            value = last.type << (next.type == last.type << 1 ? 2 : 1);

                        // value is now an appropriate value for this note. Change this note and all immediately repeated ones.
                        replace = true;
                        curr.type = value;
                    }
                    last = curr;
                    curr = next;
                }

                // Handle final iteration
                if (replace)
                {
                    if (last.endtype == curr.endtype)
                        curr.type = last.type;
                    else
                        replace = false;
                }

                if (last.type == curr.type && last.endtype != curr.endtype)
                {
                    // Found an irrational note. Pick a new spot for it with seededRNG
                    ulong value;
                    if (last.type == 1)
                        value = 2;
                    else if (last.type == (ulong)1 << maxNote)
                        value = (ulong)1 << (maxNote - 1);
                    else
                        value = (curr.endtype < last.endtype) ? last.type >> 1 : last.type << 1;

                    // value is now an appropriate value for this note. Change this note and all immediately repeated ones.
                    replace = true;
                    curr.type = value;
                }
            }
            //
            // Chords
            //
            if (allChords.Count > 5) // 5 is arbitrary, chosen to be >2 for the base cases below, and <#chords before beginner chords will repeat
            {
                NoteX[] last = allChords[0];
                NoteX[] curr = allChords[1];
                for (int i = 2; i < allChords.Count; i++)
                {
                    if (curr.Length < 2 || last.Length < 2)
                        throw new System.NotSupportedException();
                    NoteX[] next = allChords[i];
                    if (areEqual(last, curr) && !EndTypesMatch(last, curr))
                    {
                        // An irrational chord progression, add or remove a note from curr[]
                        if (curr[2] == null)
                        {
                            // Add a note, make sure curr doesn't become a copy of next
                            if (curr[0].type != 4 && curr[1].type != 4)
                                curr[2] = new NoteX(curr[1].time, 4);
                            else if (curr[0].type != 2 && curr[1].type != 2)
                                curr[2] = new NoteX(curr[1].time, 2);
                            else if (curr[0].type != 8 && curr[1].type != 8)
                                curr[2] = new NoteX(curr[1].time, 8);

                            if (areEqual(curr, next))
                                curr[2].type = 1; // a safe value
                        }
                        else
                        {
                            // 3 notes, remove one and check against curr as above
                            NoteX removed = curr[2];
                            curr[2] = null;
                            if (areEqual(curr, next))
                            {
                                // The only chords we can't be are last==curr and prev==curr[0:1]
                                // Use curr[1:2]
                                curr[0] = curr[1];
                                curr[1] = removed;
                            }
                        }
                        while (EndTypesMatch(curr, next) && !areEqual(curr, next))
                        {
                            next[0] = new NoteX(curr[0].time, curr[0].type);
                            next[0].endtype = curr[0].endtype;
                            next[1] = new NoteX(curr[1].time, curr[1].type);
                            next[1].endtype = curr[1].endtype;
                            if (curr[2] == null)
                            {
                                next[2] = null;
                            }
                            else
                            {
                                next[2] = new NoteX(curr[2].time, curr[2].type);
                                next[2].endtype = curr[2].endtype;
                            }

                            last = curr;
                            curr = next;
                            i++;
                        }
                    }
                    last = curr;
                    curr = next;
                }
            }

            /*
            foreach (NoteX n in allNotes)
            {
                if (n.length < 100)
                    n.length = 100;
            }
            foreach(NoteX[] na in allChords)
                foreach (NoteX n in na)
                {
                    if (n != null && n.length < 100)
                        n.length = 100;
                }
            */ 
        }
    }
}