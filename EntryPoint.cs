using System;
using System.Reflection;
using System.Threading;

namespace LemonLite;

internal class EntryPoint
{
    private static Mutex? _appMutex = null;
    static bool IsAppRunning()
    {
        _appMutex = new Mutex(false, Assembly.GetExecutingAssembly().GetName().Name, out bool firstInstant);
        return !firstInstant;
    }
    [STAThread]
    static void Main(string[] args)
    {
        if (IsAppRunning())
        {
            return;
        }
        App app = new();
        app.Run();
    }
}
