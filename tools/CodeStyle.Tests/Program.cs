using CodeStyle;

namespace CodeStyleTests;

internal static class Program
{
    private static async Task<int> Main()
    {
        try
        {
            await SdkHost.InitializeAsync();
            await FixtureChecks.RunAsync();
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
