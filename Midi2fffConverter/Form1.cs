using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Fortissimo;

namespace Midi2fffConverter
{
    public partial class Form1 : Form
    {
        M2F_NoteManager noteManager;
        String filename;
        float[] difficulties = new float[4];
        float editedDifficulty;
        int editedTrackIndex, editedDiffIndex;
        List<int[]> newChanges;
        List<int> GuitarTracks, RhythmTracks, DrumTracks, VocalTracks;
        List<int[]>[][] changes = new List<int[]>[4][];
        List<NoteX> editedNoteList;
        bool runningNotes = false; // Means the preview is moving
        bool runningPreview = false; // Previewing a track, can only close or continue watching in real time
        
        // XNA + Fortissimo graphics stuff
        Microsoft.Xna.Framework.Game game;
        InputManager IM;
        InputSkin skin;

        public Form1()
        {
            InitializeComponent();
            game = new Microsoft.Xna.Framework.Game();
            IM = new GuitarInput(game);
            skin = new InputSkin(game, IM);
        }
        private void Btn_LoadMidi_Click(object sender, EventArgs e)
        {
            filename = TxtBox_LoadMidi.Text;
            // reinit changes[][]
            for (int i = 0; i < 4; i++)
            {
                changes[i] = new List<int[]>[4];
                for (int j = 0; j < 4; j++)
                {
                    changes[i][j] = new List<int[]>();
                }
            }
            
            float concat0 = float.Parse(diff0.Value.ToString()) / 100.0f;
            float concat1 = float.Parse(diff1.Value.ToString()) / 100.0f;
            float concat2 = float.Parse(diff2.Value.ToString()) / 100.0f;
            float concat3 = float.Parse(diff3.Value.ToString()) / 100.0f;

            concat0 += float.Parse(seed0.Value.ToString()) / 10000.0f;
            concat1 += float.Parse(seed1.Value.ToString()) / 10000.0f;
            concat2 += float.Parse(seed2.Value.ToString()) / 10000.0f;
            concat3 += float.Parse(seed3.Value.ToString()) / 10000.0f;

            difficulties[0] = concat0;
            difficulties[1] = concat1;
            difficulties[2] = concat2;
            difficulties[3] = concat3;

            bool valid = true;
            try
            {
                noteManager = new M2F_NoteManager(TxtBox_LoadMidi.Text, concat0, concat1, concat2, concat3);
            }
            catch (Exception)
            {
                valid = false;
            }
            if (noteManager.myMidiFile == null || noteManager.myMidiFile.allTracks.Length == 0)
                valid = false;

            if (valid)
            {
                groupBox2.Hide();
                groupBox1.Show();
                groupBox4.Hide();
                groupBox6.Hide();
                int UT = noteManager.midiTracks.Count / 4;
                UnhandledTracks.Text = UT.ToString();
                currentTrack.Minimum = 1;
                currentTrack.Maximum = UT;
                currentTrack.Value = 1;
                UpdateMidiTrackData();

                GuitarTracks = new List<int>();
                RhythmTracks = new List<int>();
                DrumTracks = new List<int>();
                VocalTracks = new List<int>();
            }
        }
        private void Btn_LoadMidiDialog_Click(object sender, EventArgs e)
        {
            OpenDlg_LoadMidi.ShowDialog();
            if(OpenDlg_LoadMidi.FileName != null)
                TxtBox_LoadMidi.Text = OpenDlg_LoadMidi.FileName;
            groupBox2.Show();
        }
        private void UpdateMidiTrackData()
        {
            int track = int.Parse(currentTrack.Value.ToString());
            List<NoteX> expertNotes = noteManager.midiTracks[(track-1)*4+3];
            label17.Text = expertNotes[0].time.ToString();
            label18.Text = noteManager.midiTracks[(track - 1) * 4 + 3].Count.ToString();
            label19.Text = noteManager.midiTracks[(track - 1) * 4 + 2].Count.ToString();
            label20.Text = noteManager.midiTracks[(track - 1) * 4 + 1].Count.ToString();
            label21.Text = noteManager.midiTracks[(track - 1) * 4].Count.ToString();
        }
        private void currentTrack_ValueChanged(object sender, EventArgs e)
        {
            UpdateMidiTrackData();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            runningPreview = false;
            int trackToAdd = int.Parse(currentTrack.Value.ToString());
            bool found = false;
            if (GuitarTracks.Contains(trackToAdd))
            {
                GuitarTracks.Remove(trackToAdd);
                string newList = "";
                foreach (int i in GuitarTracks)
                    newList += " (" + i.ToString() + ")";
                label30.Text = newList;
                found = true;
            }
            else if (RhythmTracks.Contains(trackToAdd))
            {
                RhythmTracks.Remove(trackToAdd);
                string newList = "";
                foreach (int i in RhythmTracks)
                    newList += " (" + i.ToString() + ")";
                label29.Text = newList;
                found = true;
            }
            else if (DrumTracks.Contains(trackToAdd))
            {
                DrumTracks.Remove(trackToAdd);
                string newList = "";
                foreach (int i in DrumTracks)
                    newList += " (" + i.ToString() + ")";
                label28.Text = newList;
                found = true;
            }
            else if (VocalTracks.Contains(trackToAdd))
            {
                VocalTracks.Remove(trackToAdd);
                string newList = "";
                foreach (int i in VocalTracks)
                    newList += " (" + i.ToString() + ")";
                label27.Text = newList;
                found = true;
            }

            if (!found)
            {
                UnhandledTracks.Text = (int.Parse(UnhandledTracks.Text) - 1).ToString();
            }

            
            if (radioButton1.Checked)
            {
                // Guitar
                GuitarTracks.Add(trackToAdd);
                string newList = "";
                foreach (int i in GuitarTracks)
                    newList += " (" + i.ToString() + ")";
                label30.Text = newList;
            }
            else if (radioButton2.Checked)
            {
                // Rhythm
                RhythmTracks.Add(trackToAdd);
                string newList = "";
                foreach (int i in RhythmTracks)
                    newList += " (" + i.ToString() + ")";
                label29.Text = newList;
            }
            else if (radioButton3.Checked)
            {
                // Drums
                DrumTracks.Add(trackToAdd);
                string newList = "";
                foreach (int i in DrumTracks)
                    newList += " (" + i.ToString() + ")";
                label28.Text = newList;
            }
            else
            {
                // Vocals
                VocalTracks.Add(trackToAdd);
                string newList = "";
                foreach (int i in VocalTracks)
                    newList += " (" + i.ToString() + ")";
                label27.Text = newList;
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            runningPreview = false;
            groupBox1.Hide();
            groupBox4.Show();

        }
        private void button5_Click(object sender, EventArgs e)
        {
            newChanges = new List<int[]>();

            // Load track(s) / difficulty
            editedDiffIndex = radioButton12.Checked ? 3 : radioButton11.Checked ? 2 : radioButton10.Checked ? 1 : 0;

            editedDifficulty = float.Parse(numericUpDown1.Value.ToString()) / 100.0f;
            editedDifficulty += float.Parse(numericUpDown2.Value.ToString()) / 10000.0f;

            if (radioButton15.Checked)
            {
                // Guitar
                noteManager.myMidiFile.GenerateNotesFromFile(filename, GuitarTracks, editedDifficulty);
                editedTrackIndex = 0;
            }
            else if (radioButton14.Checked)
            {
                // Rhythm
                noteManager.myMidiFile.GenerateNotesFromFile(filename, RhythmTracks, editedDifficulty);
                editedTrackIndex = 1;
            }
            else if (radioButton13.Checked)
            {
                // Drums
                noteManager.myMidiFile.GenerateNotesFromFile(filename, DrumTracks, editedDifficulty);
                editedTrackIndex = 2;
            }
            else
            {
                // Vocals
                noteManager.myMidiFile.GenerateNotesFromFile(filename, VocalTracks, editedDifficulty);
                editedTrackIndex = 3;
            }

            editedNoteList = noteManager.myMidiFile.AllNotes;

            // Spawn editor
            groupBox6.Show();
            UD_CurrentNote.Maximum = editedNoteList.Count;
            UD_CurrentNote.Value = 0;
            UD_Time.Maximum = editedNoteList[editedNoteList.Count - 1].time;
            UD_Time.Value = 0;
            UD_Value.Value = editedNoteList[0].type;
            //VerticalScroll.Maximum = (int)editedNoteList[editedNoteList.Count - 1].time;
            //VerticalScroll.Value = 0;
        }
        private void button6_Click(object sender, EventArgs e)
        {
            // Save changes
            changes[editedTrackIndex][editedDiffIndex] = newChanges;
            difficulties[editedDiffIndex] = editedDifficulty;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Dump to file
            MidiFile.UserMods mods = new MidiFile.UserMods();

            mods.difficulty = new float[4];
            for(int i=0; i<4; i++)
                mods.difficulty[i] = difficulties[i];
            
            mods.GuitarTracks = GuitarTracks;
            mods.RhythmTracks = RhythmTracks;
            mods.DrumTracks = DrumTracks;
            mods.VocalTracks = VocalTracks;

            mods.changedNotes = changes;

            // Copy .mid to .fff
            System.IO.File.Copy(filename, filename.Substring(0, filename.Length - 4) + ".fff", true);

            // Dump mods to a .ffm file
            mods.filename = filename.Substring(0, filename.Length - 4) + ".ffm";
            mods.DumpToFile();
        }
        private void button7_Click(object sender, EventArgs e)
        {
            // Store a changed note
            int cur = int.Parse(UD_CurrentNote.Value.ToString());
            int val = int.Parse(UD_Value.Value.ToString());
            int[] pair = new int[2];
            pair[0] = cur;
            pair[1] = val;
            newChanges.Add(pair);
            editedNoteList[int.Parse(UD_CurrentNote.Value.ToString())].type = (ulong)val;
        }
        private void Btn_Play_Click(object sender, EventArgs e)
        {
            runningNotes = true;
        }
        private void Btn_Pause_Click(object sender, EventArgs e)
        {
            runningNotes = false;
        }
        private void UD_CurrentNote_ValueChanged(object sender, EventArgs e)
        {
            UD_Value.Value = editedNoteList[int.Parse(UD_CurrentNote.Value.ToString())].type;
            UD_Time.Value = editedNoteList[int.Parse(UD_CurrentNote.Value.ToString())].time;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            // Preview a track
            runningPreview = true;
            int track = radioButton1.Checked ? 0 : radioButton2.Checked ? 1 : radioButton3.Checked ? 2 : 3;
            int diff = radioButton5.Checked ? 3 : radioButton6.Checked ? 2 : radioButton7.Checked ? 1 : 0;
            List<int> tempList = new List<int>();
            tempList.Add(track);
            //noteManager.myMidiFile.GenerateNotesFromFile(filename, tempList, difficulties[diff]);
            editedNoteList = noteManager.midiTracks[track * 4 + diff];//noteManager.myMidiFile.AllNotes;
        }
        public void StartGraphics()
        {
        }
        public void Update(int dt)
        {
            if (runningNotes)
            {
                Decimal newTime = UD_Time.Value + dt;
                while (newTime > editedNoteList[int.Parse(UD_CurrentNote.Value.ToString())].time)
                    UD_CurrentNote.Value++;
                UD_Time.Value = newTime;
            }
            else if (runningPreview)
            {
                // Draw List<NoteX> editedNoteList
            }
        }
    }
}
