using AutoInsta;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;

class Program
{
    public static IConfigurationRoot configuration;

    static int Main(string[] args)
    {
        try
        {
            // Start!
            MainAsync(args).Wait();
            return 0;
        }
        catch(Exception e)
        {
            Console.WriteLine($"Error running service: {e}");
            return 1;
        }
    }

    static async Task MainAsync(string[] args)
    {
        // Create service collection
        Console.WriteLine("## Creating service collection ##");
        ServiceCollection serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);

        // Create service provider
        Console.WriteLine("## Building service provider ##");
        IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        try
        {
            Console.WriteLine("## Starting service ##");
            await serviceProvider.GetRequiredService<IPageObject>().Run();
            Console.WriteLine("## Ending service ##");
        }
        catch (Exception ex)
        {
            Console.WriteLine("## Error running service ##");
            throw ex;
        }
        finally
        {
            Console.WriteLine("## Closed ##");
        }
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        // Build configuration
        configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
            .AddJsonFile("appsettings.json", false)
            .Build();

        // Add access to generic IConfigurationRoot
        serviceCollection.AddSingleton<IConfigurationRoot>(configuration);

        // Add page object
        serviceCollection.AddTransient<IPageObject, Instagram>();
    }
}