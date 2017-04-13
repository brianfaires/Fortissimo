using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fortissimo;

namespace Midi2fffConverter
{
    public class M2F_NoteManager
    {
        public MidiFile myMidiFile = null;
        public List<List<NoteX>> midiTracks;
        public List<NoteX>[][] formattedNotes;

        public M2F_NoteManager(String filename, float d0, float d1, float d2, float d3)
        {
            float[] diffs = new float[4];
            diffs[0] = d0;
            diffs[1] = d1;
            diffs[2] = d2;
            diffs[3] = d3;
            formattedNotes = null;
            midiTracks = new List<List<NoteX>>();

            // General midi file, parse each track into midiTracks
            myMidiFile = new MidiFile();

            List<int> trackToParse = new List<int>();
            trackToParse.Add(1);
            if (!myMidiFile.GenerateNotesFromFile(filename, trackToParse, .95f))
            {
                myMidiFile = null;
                return;
            }
            int totalMidiTracks = 0;
            for (int i = 0; i < myMidiFile.totalNoteOnEvents.Length; i++)
                if (myMidiFile.totalNoteOnEvents[i] != 0)
                    totalMidiTracks++;

            midiTracks = new List<List<NoteX>>();
            for (int i = 1; i <= totalMidiTracks; i++)
            {
                for (int j = 1; j <= 4; j++)
                {
                    trackToParse[0] = i;
                    myMidiFile.GenerateNotesFromFile(filename, trackToParse, diffs[j-1]);
                    midiTracks.Add(myMidiFile.AllNotes);
                }
            }

            //
            // Load file
            /*if (isGHRB)
            {
                midiTracks = null;
                myMidiFile = null;
                myGHRBFile = new GuitarHeroMusicFile();
                if (!myGHRBFile.GenerateNotesFromFile(filename))
                {
                    myGHRBFile = null;
                    return;
                }
                int totalTracks = myGHRBFile.AllNotes.Length;
                // Notes.mid is already formatted, just fill in formattedNotes[][]
                formattedNotes = new List<NoteX>[totalTracks][];
                for (int i = 0; i < totalTracks; i++)
                    formattedNotes[i] = new List<NoteX>[4];

                foreach (List<NoteX>[] lna in formattedNotes)
                {
                    lna[0] = new List<NoteX>();
                    lna[1] = new List<NoteX>();
                    lna[2] = new List<NoteX>();
                    lna[3] = new List<NoteX>();
                }

                // Copy all notes instead of referencing, to prevent potential accidental aliasing
                for(int i = 0; i/4 < myGHRBFile.AllNotes.Length; i++)
                    formattedNotes[i / 4][i % 4] = myGHRBFile.AllNotes[i/4][i%4];
            }
            else
            {*/
        }
    }
}
