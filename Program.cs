using System;

namespace FrostEngine
{
    class Program
    {
        static void Main(string[] args)
        {
            bool playerMode = false;
            foreach(var arg in args)
            {
                if (arg == "--play") playerMode = true;
            }

            Engine engine = new Engine(playerMode);
            engine.Run();
        }
    }
}
