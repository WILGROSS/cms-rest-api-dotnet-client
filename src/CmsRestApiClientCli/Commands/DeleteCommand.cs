using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CmsRestApiClientCli.Configuration;
using CmsRestApiClientCli.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CmsRestApiClientCli.Commands;

public sealed class DeleteCommand : AsyncCommand<DeleteCommand.Settings>
{
    private const string ResourceTypeArguments = "<propertygroups|contenttypes|displaytemplates>";

    private readonly TokenService tokenService;

    private readonly CmsService cmsService;

    private readonly CmsInstanceOptions cmsInstanceOptions;

    public DeleteCommand(
        TokenService tokenService,
        CmsService cmsService,
        IOptions<CmsInstanceOptions> cmsInstance)
    {
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        this.cmsService = cmsService ?? throw new ArgumentNullException(nameof(cmsService));
        this.cmsInstanceOptions = cmsInstance?.Value ?? throw new ArgumentNullException(nameof(cmsInstance));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var resourceType = settings.ResourceType.ToLower(CultureInfo.InvariantCulture).Trim();
        var validArguments = ResourceTypeArguments.Split('|').Select(x => x.Replace("<", string.Empty).Replace(">", string.Empty)).ToArray();

        if (!validArguments.Contains(resourceType))
        {
            AnsiConsole.MarkupLineInterpolated($"[bold red]Invalid argument, the valid ones are: {string.Join(", ", validArguments)}[/]");
            return 0;
        }

        var accessToken = await this.tokenService.GetAccessToken(
            this.cmsInstanceOptions.ClientId,
            this.cmsInstanceOptions.ClientSecret);

        var json = new JsonText(await this.cmsService.Delete(resourceType, settings.Key, accessToken));

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLineInterpolated($"[bold red]Delete {settings.Key} from {resourceType}[/]");

        AnsiConsole.Write(
            new Panel(json)
                .Collapse()
                .RoundedBorder()
                .BorderColor(Color.Red));

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, ResourceTypeArguments)]
        public string ResourceType { get; set; }

        [CommandArgument(1, "<KEY>")]
        public string Key { get; set; }
    }
}
