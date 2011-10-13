using System;

namespace Pandamonium
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (Pandamonium game = new Pandamonium())
            {
                //GIT is awesomesauce!
                game.Run();
            }
        }
    }
#endif
}

