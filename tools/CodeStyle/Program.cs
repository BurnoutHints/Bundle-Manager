namespace CodeStyle;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            CommandOptions? options = CommandOptions.Parse(args);

            if (options == null)
                return 0;

            await SdkHost.InitializeAsync();
            return await StyleRunner.RunAsync(options);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("CodeStyle: " + exception.Message);
            return 2;
        }
    }
}
