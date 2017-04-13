using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SongDataIO
{
    public class InstrumentMaster
    {
        private List<Instrument> instruments;

        private static InstrumentMaster SINGLETON_InstrumentMaster = null;

        private InstrumentMaster()
        {
            String dir = Directory.GetCurrentDirectory() + "\\";
            String[] files = Directory.GetFiles(dir + "instruments\\", "*.txt");
            instruments = new List<Instrument>();
            for (int i = 0; i < files.Length; i++)
            {
                Instrument instr = new Instrument();
                StreamReader bin = new StreamReader(File.OpenRead(files[i]));
                while (!bin.EndOfStream)
                {
                    String line = bin.ReadLine();
                    if (line.Contains("="))
                    {
                        String left = line.Substring(0, line.IndexOf('=')).Trim();
                        String right = line.Substring(line.IndexOf('=') + 1).Trim();
                        //if(left.ToLower().Equals("boardbump"))
                        //handle commas
                        instr.SetValue(left, right);
                    }
                }
                bin.Close();

                instruments.Add(instr);
            }
        }

        public Instrument GetInstrument(int index)
        {
            return instruments[index];
        }

        public int GetNumInstruments()
        {
            return instruments.Count;
        }

        public Instrument GetInstrument(String codename)
        {
            for (int i = 0; i < instruments.Count; i++)
                if (instruments[i].CodeName.Equals(codename.ToUpper()))
                    return instruments[i];
            throw new IndexOutOfRangeException();
        }

        public static void CreateSingleton()
        {
            if (SINGLETON_InstrumentMaster == null)
                SINGLETON_InstrumentMaster = new InstrumentMaster();
            else
                throw new InvalidOperationException("Singleton has already been initialized");
        }

        public static void DestroySingleton()
        {
            if (SINGLETON_InstrumentMaster != null)
                SINGLETON_InstrumentMaster = null;
            else
                throw new InvalidOperationException("Singleton has already been destroyed");
        }

        public static InstrumentMaster GetSingleton()
        {
            return SINGLETON_InstrumentMaster;
        }
    }
}
