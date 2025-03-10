using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CmsRestApiClientCli.Configuration;
using CmsRestApiClientCli.Services;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;

namespace CmsRestApiClientCli.Commands;

public sealed class ListCommand : AsyncCommand<ListCommand.Settings>
{
    private const string ResourceTypeArguments = "<contenttypes|propertyformats|propertygroups|displaytemplates>";

    private readonly TokenService tokenService;

    private readonly CmsService cmsService;

    private readonly CmsInstanceOptions cmsInstanceOptions;

    public ListCommand(TokenService tokenService, CmsService cmsService, IOptions<CmsInstanceOptions> cmsInstance)
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
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] Invalid argument, the valid ones are: {string.Join(", ", validArguments)}");
            return 0;
        }

        var accessToken = await this.tokenService.GetAccessToken(
            this.cmsInstanceOptions.ClientId,
            this.cmsInstanceOptions.ClientSecret);

        var result = await this.cmsService.List(resourceType, accessToken);

        if (resourceType == "contenttypes")
        {
            var failed = false;
            ContentType[] contentTypes = [];

            try
            {
                var root = JsonSerializer.Deserialize<RootObject>(result);
                contentTypes = root.ContentTypes.OrderBy(x => x.BaseType).ThenBy(x => x.SortOrder).ThenBy(x => x.Key).ToArray();
            }
            catch (Exception)
            {
                failed = true;
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold yellow]List contenttypes[/]");

            if ((settings.WriteJson.HasValue && settings.WriteJson.Value) || failed)
            {
                var json = new JsonText(result);

                AnsiConsole.Write(
                    new Panel(json)
                        .Collapse()
                        .RoundedBorder()
                        .BorderColor(Color.Yellow));
            }
            else
            {
                var table = new Table()
                    .BorderColor(Color.Yellow)
                    .Border(TableBorder.Rounded);

                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.Key)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.BaseType)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.DisplayName)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.Description)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.SortOrder)}[/]")).RightAligned());
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.LastModified)}[/]")));

                foreach (var contentType in contentTypes)
                {
                    table.AddRow(
                        contentType.Key,
                        contentType.BaseType,
                        contentType.DisplayName,
                        contentType.Description,
                        contentType.SortOrder.ToString(CultureInfo.InvariantCulture),
                        contentType.LastModified > new DateTime(2000, 1, 1) ? contentType.LastModified?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty : string.Empty);
                }

                AnsiConsole.Write(table);
            }
        }
        else
        {
            var json = new JsonText(result);

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLineInterpolated($"[bold yellow]List {resourceType}[/]");

            AnsiConsole.Write(
                new Panel(json)
                    .Collapse()
                    .RoundedBorder()
                    .BorderColor(Color.Yellow));
        }

        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, ResourceTypeArguments)]
        public string ResourceType { get; set; }

        [CommandOption("-j|--json")]
        public bool? WriteJson { get; set; }
    }

    public sealed class RootObject
    {
        [JsonPropertyName("items")]
        public ContentType[] ContentTypes { get; set; }
    }

    public sealed class ContentType
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("baseType")]
        public string BaseType { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }

        [JsonPropertyName("lastModified")]
        public DateTime? LastModified { get; set; }
    }
}
