// Description  : Main class.  Runs game.  Yay!

#region Using Statements
using System;
#endregion

namespace Fortissimo
{
    static class RhythmMain
    {
        static void Main(string[] args)
        {
            using (RhythmGame game = new RhythmGame())
            {
                game.Run();
            }
        }
    }
}

