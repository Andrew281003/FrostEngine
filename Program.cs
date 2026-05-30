using System;

namespace FrostEngine
{
    class Program
    {
        [STAThread] // Good practice for Windows UI applications
        static void Main(string[] args)
        {
            // Just boot the engine instantly! No more console dependencies.
            Engine engine = new Engine();
            engine.Run();
        }
    }
}