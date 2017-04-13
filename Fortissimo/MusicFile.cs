using System;
using System.Collections.Generic;
using System.IO;
using SongDataIO;

namespace GrimTides
{
    public class MusicFile
    {
        private const int NOT_USED = 1 << 8; // Larger than any byte (==256)
        // Track class
        private class Track
        {
            public class Event
            {
                public long absTickCount;
                public long absTimeMS;
                public int eventType;
                public int channel;
                public int param1;
                public int param2;

                public Event(long absTickCount, long absTimeMS, int eventType, int channel, int param1, int param2)
                {
                    this.absTickCount = absTickCount;
                    this.absTimeMS = absTimeMS;
                    this.eventType = eventType;
                    this.channel = channel;
                    this.param1 = param1;
                    this.param2 = param2;
                }
            }

            public String trackID;
            public int size;
            public List<Event> allEvents;

            public Track()
            {
                allEvents = new List<Event>(300); // Arbitrary starting value for list size
            }

            public bool AddEvent(long absTickCount, long absTimeMS, int eventType, int channel, int param1, int param2)
            {
                try
                {
                    allEvents.Add(new Event(absTickCount, absTimeMS, eventType, channel, param1, param2));
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
            public bool AddMetaEvent(long absTickCount, long absTimeMS, int command, int param1, int param2, int param3)
            {
                try
                {
                    allEvents.Add(new Event(absTickCount, absTimeMS, MetaCommandToEventType(command), param1, param2, param3));
                }
                catch (Exception)
                {
                    return false;
                }
                return true;
            }
            public static int MetaCommandToEventType(int metaCommand)
            {
                return metaCommand + 1000;
            }
            public static int EventTypeToMetaCommand(int eventType)
            {
                return eventType - 1000;
            }
        }

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

        // Lists, allNotes is public
        private Track[] allTracks;
        private List<NoteX>[] allNotes;

        public List<NoteX>[] AllNotes { get { return allNotes; } }

        // Constructor
        public MusicFile()
        {
            formatType = -1;
            ticksPerFrame = -1;
            ticksPerBeat = -1;
            timeCode = -1;
        }
        // File Parsing functions
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

            for (int i = 0; i < totalTracks; i++)
            {
                allTracks[i] = new Track();
                int curTickCount = 0;
                if (fdIndex >= curFileSize) // Should never be false
                    return false;

                allTracks[i].trackID = ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();
                allTracks[i].trackID += ((char)fileData[fdIndex++]).ToString();

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
                while (fdIndex < nextTrack)
                {
                    int dT = ParseVariableLength();
                    curTickCount += dT;

                    // Parse event type, then the corresponding parameters
                    int eventType = fileData[fdIndex] >> 4; // Higher 4 bits
                    int channel = fileData[fdIndex++] & 0x0F; // Lower 4 bits
                    switch (eventType)
                    {
                        case (0x8): // Note off
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]))
                                break; // AddEvent should never return false
                            fdIndex += 2;
                            break;
                        case (0x9): // Note on
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]))
                                break; // AddEvent should never return false
                            fdIndex += 2;
                            totalNoteOnEvents[i]++;
                            break;
                        case (0xA): // Note aftertouch
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]))
                                break; // AddEvent should never return false
                            fdIndex += 2;
                            break;
                        case (0xB): // Controller
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]))
                                break; // AddEvent should never return false
                            fdIndex += 2;
                            break;
                        case (0xC): // Program change
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex++], -1))
                                break; // AddEvent should never return false
                            break;
                        case (0xD): // Channel aftertouch
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex++], -1))
                                break; // AddEvent should never return false
                            break;
                        case (0xE): // Pitch blend
                            if (!allTracks[i].AddEvent(curTickCount, -1, eventType, channel, fileData[fdIndex], fileData[fdIndex + 1]))
                                break; // AddEvent should never return false
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
                                    fdIndex += length; // Skip over these events
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
                                            if (!allTracks[i].AddMetaEvent(curTickCount, -1, command, -1, -1, -1))
                                                break;
                                            break;
                                        case (0x51): // Set Tempo
                                            if (!allTracks[i].AddMetaEvent(curTickCount, -1, command, fileData[fdIndex - 3], fileData[fdIndex - 2], fileData[fdIndex - 1]))
                                                break;
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
                        default: // There is a 0 in MSB of eventType so this is a running event
                            break;
                    }

                }
                if (fdIndex != nextTrack)
                    fdIndex = fdIndex * 1;
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
            allNotes = new List<NoteX>[0];

            for (int i = 0; i < allTracks.Length; i++)
            {
                if (totalNoteOnEvents[i] != 0)
                {
                    totalValidTracks++;
                    List<NoteX>[] newArray = new List<NoteX>[totalValidTracks];
                    newArray[totalValidTracks - 1] = new List<NoteX>();
                    allNotes.CopyTo(newArray, 0);
                    allNotes = newArray;

                    uint lastTime = uint.MaxValue;
                    NoteX lastNote = null;
                    foreach (Track.Event e in allTracks[i].allEvents)
                    {
                        if (e.eventType == 0x9) // Note on event
                        {
                            if (e.absTimeMS == lastTime && lastNote != null)
                                lastNote.AddTo((ulong)e.param1 % 5);
                            else
                            {
                                lastNote = new NoteX((uint)e.absTimeMS, (ulong)e.param1 % 5);
                                lastTime = (uint)e.absTimeMS;

                                allNotes[totalValidTracks - 1].Add(lastNote);
                            }
                        }
                    }
                }
            }
            return true;
        }
    }
}
