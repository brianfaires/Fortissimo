using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Midi2fffConverter
{
    class Midi2fffConverter
    {
        [STAThread]
        static void Main(string[] args)
        {
            Form1 f = new Form1();
            
            f.ShowDialog();
            f.Close();
        }
    }
}
