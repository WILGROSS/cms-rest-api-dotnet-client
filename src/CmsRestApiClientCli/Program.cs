using System;
using System.IO;
using CmsRestApiClientCli.Commands;
using CmsRestApiClientCli.Configuration;
using CmsRestApiClientCli.Infrastructure;
using CmsRestApiClientCli.Services;
using CmsRestApiClientCli.Upsert;
using CmsRestApiClientCli.Upsert.PlainJsonFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace CmsRestApiClientCli;

public class Program
{
    public static int Main(string[] args)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile($"appsettings.json", true, true)
            .AddJsonFile($"appsettings.Development.json", true, true)
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        var config = builder.Build();
        var cmsInstanceOptions = config.GetSection("CmsInstance").Get<CmsInstanceOptions>();
        var plainJsonFilesOptions = config.GetSection("PlainJsonFiles").Get<PlainJsonFilesOptions>();

        var registrations = new ServiceCollection();

        registrations.AddOptions<CmsInstanceOptions>()
            .Bind(config.GetSection("CmsInstance"));

        registrations.AddOptions<PlainJsonFilesOptions>()
            .Bind(config.GetSection("PlainJsonFiles"));

        registrations.AddHttpClient<TokenService>(client =>
        {
            client.BaseAddress = new Uri(cmsInstanceOptions.BaseUrl);
        });

        registrations.AddHttpClient<CmsService>(client =>
        {
            client.BaseAddress = new Uri(cmsInstanceOptions.BaseUrl);
        });

        // The default implementations that "forwards" JSON files as PUT requests with filename as key
        registrations.AddSingleton<IUpsertPropertyGroups>(
            new PlainJsonPropertyGroupsService(
                Path.Combine(
                    plainJsonFilesOptions.FolderToProcess,
                    "PropertyGroups")));

        registrations.AddSingleton<IUpsertContentTypes>(
            new PlainJsonContentTypesService(
                Path.Combine(
                    plainJsonFilesOptions.FolderToProcess,
                    "ContentTypes")));

        registrations.AddSingleton<IUpsertDisplayTemplates>(
            new PlainJsonDisplayTemplatesService(
                Path.Combine(
                    plainJsonFilesOptions.FolderToProcess,
                    "DisplayTemplates")));

        // Add your own IUpsert* implementations
        // Could be C# models you serialize or similar

        // Assemble the command app
        var registrar = new TypeRegistrar(registrations);

        var app = new CommandApp(registrar);

        app.Configure(x =>
        {
            x.AddCommand<ListCommand>("list");
            x.AddCommand<UpsertCommand>("upsert");
            x.AddCommand<DeleteCommand>("delete");
        });

        return app.Run(args);
    }
}
