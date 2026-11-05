using System.Diagnostics;

namespace BundleUtilities;

public class DebugTimer
{
    private readonly Stopwatch stopwatch;
    public readonly string Name;

    private DebugTimer(string name)
    {
        Name = name;

        stopwatch = new();
        stopwatch.Start();
    }

    public static DebugTimer Start(string name = "")
    {
        return new(name);
    }

    public double Stop()
    {
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds;
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
