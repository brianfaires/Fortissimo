using System;
using System.Collections.Generic;
using System.IO;
using SongDataIO;

namespace Fortissimo
{
    public class GuitarHeroMusicFile
    {
        // File data
        private int[] fileData; // Contains each byte of the file. Stored in ints for easier data manipulation
        private int fdIndex; // Index into fileData
        private long curFileSize;

        // File values that apply to all tracks
        private int formatType;
        private int ticksPerBeat;
        private int timeCode;
        private int ticksPerFrame;
        private int totalTracks;
        public int[] totalNoteOnEvents;
        public List<long> markers;

        // Lists, allNotes is public
        private Track[] allTracks;
        private List<NoteX>[][] allNotes;
        public List<NoteX>[][] AllNotes { get { return allNotes; } }

        // Constructor
        public GuitarHeroMusicFile()
        {
            formatType = -1;
            ticksPerFrame = -1;
            ticksPerBeat = -1;
            timeCode = -1;
            markers = new List<long>();
        }
        public bool GenerateNotesFromFile(String filename)
        {
            if (!OpenFile(filename))
                return false;
            if (!ParseHeader())
                return false;
            if (!ParseTracks())
                return false;
            if (!CalculateAbsTimes())
                return false;
            if (!GenerateFormattedNotes())
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
                                            break;
                                        case (0x04): // Instument name
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
                                            break;
                                        case (0x58): // Time Signature
                                            break;
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
                                            break; // should never be executed
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

            bool continueLoop = true;
            long nextMarker = 0;
            while (continueLoop)
            {
                int earliestElement = -1;
                long earliestTickCount = 0x7FFFFFFFFFFFFFFF;

                // Find the earliest event (must be in a valid track with more unhandled events)
                for (int i = 0; i < totalTracks; i++)
                {
                    if (curEventIndex[i] >= allTracks[i].allEvents.Count)
                        continue;
                    if (allTracks[i].allEvents[curEventIndex[i]].absTickCount < earliestTickCount)
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
        private bool GenerateFormattedNotes()
        {
            int totalValidTracks = 0;
            for (int i = 0; i < allTracks.Length; i++)
            {
                if (totalNoteOnEvents[i] != 0)
                {
                    totalValidTracks++;
                }
            }
            allNotes = new List<NoteX>[totalValidTracks][];
            for (int i = 0; i < totalValidTracks; i++)
            {
                allNotes[i] = new List<NoteX>[4];
                allNotes[i][0] = new List<NoteX>();
                allNotes[i][1] = new List<NoteX>();
                allNotes[i][2] = new List<NoteX>();
                allNotes[i][3] = new List<NoteX>();
            }

            int curValidTrack = -1;
            for (int i = 0; i < allTracks.Length; i++)
            {
                if (totalNoteOnEvents[i] != 0)
                {
                    curValidTrack++;
                    uint lastTime = uint.MaxValue;
                    NoteX lastNote = null;
                    bool[] starPowerActive = { false, false, false, false };
                    foreach (Track.Event e in allTracks[i].allEvents)
                    {

                        if (e.eventType == 0x9) // Note on event
                        {
                            ProcessNoteOn(curValidTrack, ref lastTime, ref lastNote, starPowerActive, e);
                        }
                        else if (e.eventType == 0x8) // Note-Off event
                        {
                            ProcessNoteOff(curValidTrack, starPowerActive, e);
                        }
                        else if(e.eventType != 1047) // End of Track event
                        {
                            // Not a Note-On, Note-Off or End of Track event
                        }
                    }
                }
            }

            CleanUpNotes();

            return true;
        }

        private void ProcessNoteOff(int curValidTrack, bool[] starPowerActive, Track.Event e)
        {
            int myDiff;
            int endLocation;
            bool isStarPower = false;
            if (e.param1 >= 96 && e.param1 <= 100)
            {
                // End Expert Note
                myDiff = 3;
                endLocation = 1 << (e.param1 - 96);
                isStarPower |= starPowerActive[3];
            }
            else if (e.param1 >= 84 && e.param1 <= 88)
            {
                // End Hard Note
                myDiff = 2;
                endLocation = 1 << (e.param1 - 84);
                isStarPower |= starPowerActive[2];
            }
            else if (e.param1 >= 72 && e.param1 <= 76)
            {
                // End Moderate Note
                myDiff = 1;
                endLocation = 1 << (e.param1 - 72);
                isStarPower |= starPowerActive[1];
            }
            else if (e.param1 >= 60 && e.param1 <= 64)
            {
                // End Beginner Note
                myDiff = 0;
                endLocation = 1 << (e.param1 - 60);
                isStarPower |= starPowerActive[0];
            }
            else if (e.param1 >= 65 && e.param1 <= 71)
            {
                // Beginner
                // 67 == End Star Power
                if (e.param1 == 67)
                    starPowerActive[0] = false;
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 77 && e.param1 <= 83)
            {
                // Moderate
                // 79 == End Star Power
                if (e.param1 == 79)
                    starPowerActive[1] = false;
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 89 && e.param1 <= 95)
            {
                // Hard
                // 91 == End Star Power
                if (e.param1 == 91)
                    starPowerActive[2] = false;
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 101 && e.param1 <= 107)
            {
                // Expert
                // 103 == End Star Power
                if (e.param1 == 103)
                    starPowerActive[3] = false;
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 40 && e.param1 <= 59)
            {
                // End antiquated note/chord
                myDiff = -1;
                endLocation = 0;
            }
            // 40-107 already handled
            else if (e.param1 >= 12 && e.param1 <= 15)
            {
                // AAA
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 24 && e.param1 <= 27)
            {
                // BBB
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 36 && e.param1 <= 39)
            {
                // CCC
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 30 && e.param1 <= 31 || e.param1 == 34)
            {
                // Only on Advanced Harmony
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 108 && e.param1 <= 112)
            {
                // 110 precedes 1st star power note on Less Talk More Rokk (same time slot)
                // 111 is Bass Track only so far
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 == 116)
            {
                myDiff = -1;
                endLocation = 0;
            }
            else if (e.param1 >= 120 && e.param1 <= 124)
            {
                // Bass track only
                myDiff = -1;
                endLocation = 0;
            }
            else
            {
                myDiff = -1;
                endLocation = 0;
            }

            if (myDiff != -1)
            {
                int lastIndex = allNotes[curValidTrack][myDiff].Count - 1;
                while (allNotes[curValidTrack][myDiff][lastIndex].type != (ulong)endLocation)
                    lastIndex--;
                allNotes[curValidTrack][myDiff][lastIndex].length = (uint)e.absTimeMS - allNotes[curValidTrack][myDiff][lastIndex].time;
                allNotes[curValidTrack][myDiff][lastIndex].isBonus |= isStarPower;
            }
        }

        private void ProcessNoteOn(int curValidTrack, ref uint lastTime, ref NoteX lastNote, bool[] starPowerActive, Track.Event e)
        {
            int noteLocation;
            int difficulty;
            bool condenseNotes = false;
            bool isStarPower = false;
            // Determine where to put the note given e.param1 (pitch), param2, e.channel and the current instrument
            //
            if (e.param2 == 0) // Signals an end note as a Note-On event
            {
                // End of the last note
                int myDiff;
                int endLocation;
                if (e.param1 >= 96 && e.param1 <= 100)
                {
                    // End Expert Note
                    myDiff = 3;
                    endLocation = 1 << (e.param1 - 96);
                    isStarPower |= starPowerActive[3];
                }
                else if (e.param1 >= 84 && e.param1 <= 88)
                {
                    // End Hard Note
                    myDiff = 2;
                    endLocation = 1 << (e.param1 - 84);
                    isStarPower |= starPowerActive[2];
                }
                else if (e.param1 >= 72 && e.param1 <= 76)
                {
                    // End Moderate Note
                    myDiff = 1;
                    endLocation = 1 << (e.param1 - 72);
                    isStarPower |= starPowerActive[1];
                }
                else if (e.param1 >= 60 && e.param1 <= 64)
                {
                    // End Beginner Note
                    myDiff = 0;
                    endLocation = 1 << (e.param1 - 60);
                    isStarPower |= starPowerActive[0];
                }
                else if (e.param1 >= 65 && e.param1 <= 71)
                {
                    // Beginner
                    // 67 == End Star Power
                    if (e.param1 == 67)
                        starPowerActive[0] = false;
                    endLocation = 0;
                    myDiff = -1;
                }
                else if (e.param1 >= 77 && e.param1 <= 83)
                {
                    // Moderate
                    // 79 == End Star Power
                    if (e.param1 == 79)
                        starPowerActive[1] = false;
                    endLocation = 0;
                    myDiff = -1;
                }
                else if (e.param1 >= 89 && e.param1 <= 95)
                {
                    // Hard
                    // 91 == End Star Power
                    if (e.param1 == 91)
                        starPowerActive[2] = false;
                    endLocation = 0;
                    myDiff = -1;
                }
                else if (e.param1 >= 101 && e.param1 <= 107)
                {
                    // Expert
                    // 103 == End Star Power
                    if (e.param1 == 103)
                        starPowerActive[3] = false;
                    // This has usually triggered an identical response in the previous 3 else if's
                    // 106, then 105: Markers for saved riffs?
                    endLocation = 0;
                    myDiff = -1;
                }
                else if (e.param1 >= 40 && e.param1 <= 59)
                {
                    // Antiquated markers for changing notes
                    myDiff = -1;
                    endLocation = 0;
                }
                //
                // Handled 40 through 107
                else if (e.param1 >= 12 && e.param1 <= 15)
                {
                    // AAA
                    myDiff = -1;
                    endLocation = 0;
                }
                else if (e.param1 >= 24 && e.param1 <= 27)
                {
                    // BBB
                    myDiff = -1;
                    endLocation = 0;
                }
                else if (e.param1 >= 36 && e.param1 <= 39)
                {
                    // CCC
                    myDiff = -1;
                    endLocation = 0;
                }
                else if (e.param1 >= 108 && e.param1 <= 112)
                {
                    // ???
                    myDiff = -1;
                    endLocation = 0;
                }
                else if (e.param1 >= 116 && e.param1 <= 124)
                {
                    // ???
                    myDiff = -1;
                    endLocation = 0;
                }
                else if (e.param1 >= 30 && e.param1 <= 31 || e.param1 == 34)
                {
                    // ???
                    myDiff = -1;
                    endLocation = 0;
                }
                else
                {
                    myDiff = -1;
                    endLocation = 0;
                }

                if (myDiff != -1)
                {
                    // Valid end of note event found
                    int lastIndex = allNotes[curValidTrack][myDiff].Count - 1;
                    while (allNotes[curValidTrack][myDiff][lastIndex].type != (ulong)endLocation)
                        lastIndex--;
                    allNotes[curValidTrack][myDiff][lastIndex].length = (uint)e.absTimeMS - allNotes[curValidTrack][myDiff][lastIndex].time;
                    allNotes[curValidTrack][myDiff][lastIndex].isBonus |= isStarPower;
                }

                noteLocation = 0;
                difficulty = -1;
            }
            // param2 != 0
            else if (e.param1 >= 96 && e.param1 <= 100)
            {
                // Start Expert Note
                noteLocation = 1 << (e.param1 - 96);
                difficulty = 3;
                isStarPower |= starPowerActive[3];
            }
            else if (e.param1 >= 84 && e.param1 <= 88)
            {
                // Start Hard Note
                noteLocation = 1 << (e.param1 - 84);
                difficulty = 2;
                isStarPower |= starPowerActive[2];
            }
            else if (e.param1 >= 72 && e.param1 <= 76)
            {
                // Start Moderate Note
                noteLocation = 1 << (e.param1 - 72);
                difficulty = 1;
                isStarPower |= starPowerActive[1];
            }
            else if (e.param1 >= 60 && e.param1 <= 64)
            {
                // Start Beginner Note
                noteLocation = 1 << (e.param1 - 60);
                difficulty = 0;
                isStarPower |= starPowerActive[0];
            }
            else if (e.param1 >= 65 && e.param1 <= 71)
            {
                // Beginner
                // 67 == Start Star Power
                if (e.param1 == 67)
                    starPowerActive[0] = true;
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 77 && e.param1 <= 83)
            {
                // Moderate
                // 79 == Start Star Power
                if (e.param1 == 79)
                    starPowerActive[1] = true;
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 89 && e.param1 <= 95)
            {
                // Hard
                // 91 == Start Star Power
                if (e.param1 == 91)
                    starPowerActive[2] = true;
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 101 && e.param1 <= 107)
            {
                // Expert
                // 103 == Start Star Power
                if (e.param1 == 103)
                    starPowerActive[3] = true;
                // This has usually triggered an identical response in the previous 3 else if's
                // 106, then 105: Sustained?
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 40 && e.param1 <= 59)
            {
                /* This is probably antiquated data that's a remnent of some step 
                 * the Rock Band creators used to generate notes, these are being ignored*/
                // -------Notes-------
                // This seems to usually be the last in the time slot. Guesses:
                // American Woman, this comes first in time slot
                // Tappable for all notes at this time slot
                // 41 : Next note is tappable, or begin SP sequence on this note
                // 40 : Tappable, SP note
                // SP+tappable for note param1-40
                noteLocation = 0;
                difficulty = -1;
            }
            // All events 40-107 have been handled
            else if (e.param1 >= 12 && e.param1 <= 15)
            {
                // AAA
                noteLocation = 0;
                difficulty = -1;
            }
            else if ((e.param1 >= 24 && e.param1 <= 27))
            {
                // BBB
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 36 && e.param1 <= 39)
            {
                // CCC
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 30 && e.param1 <= 31 || e.param1 == 34)
            {
                // Only on (Advanced Harmony)
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 108 && e.param1 <= 112)
            {
                // ???
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 == 116)
            {
                // ???
                noteLocation = 0;
                difficulty = -1;
            }
            else if (e.param1 >= 120 && e.param1 <= 124)
            {
                // Bass?
                noteLocation = 0;
                difficulty = -1;
            }
            else
            {
                noteLocation = 0;
                difficulty = -1;
            }

            if (noteLocation > 31 || noteLocation < 0)
                throw new System.NotSupportedException();

            if (condenseNotes && (e.absTimeMS == lastTime) && (lastNote != null))
                lastNote.AddTo((ulong)noteLocation);
            else
            {
                if (noteLocation != 0)
                {
                    lastTime = (uint)e.absTimeMS;
                    lastNote = new NoteX((uint)e.absTimeMS, (ulong)noteLocation);
                    lastNote.isBonus |= isStarPower;
                    allNotes[curValidTrack][difficulty].Add(lastNote);
                }
            }
        }

        private void CleanUpNotes()
        {
            // Condense simultaneous notes and enforce a minimum length
            foreach (List<NoteX>[] lna in allNotes)
            {
                // Instrument
                foreach (List<NoteX> ln in lna)
                {
                    // Difficulty
                    NoteX last = null;
                    for (int i = 0; i < ln.Count; i++)
                    {
                        // Each note for the current instrument/difficulty
                        NoteX n = ln[i];

                        /*
                        // Enforce min length
                        if (n != null && n.length < 100)
                            n.length = 100;
                        */
                        // Condense chords to a single note
                        if (last != null && last.time == n.time)
                        {
                            last.type |= n.type;
                            ln.Remove(n);
                            i--;
                        }
                        last = n;
                    }
                }
            }
        }
    }
}
