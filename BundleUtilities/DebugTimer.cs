using System.Diagnostics;

namespace BundleUtilities;

public class DebugTimer
{
    private readonly Stopwatch _stopwatch;
    public readonly string Name;

    private DebugTimer(string name)
    {
        Name = name;

        _stopwatch = new Stopwatch();
        _stopwatch.Start();
    }

    public static DebugTimer Start(string name = "")
    {
        return new DebugTimer(name);
    }

    public double Stop()
    {
        _stopwatch.Stop();
        return _stopwatch.Elapsed.TotalMilliseconds;
    }

    public double StopLog()
    {
        double elapsed = Stop();
        if (Name == "")
            Debug.WriteLine("took " + elapsed + " ms");
        else
            Debug.WriteLine(Name + " took " + elapsed + " ms");
        return elapsed;
    }
}
