using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CmsRestApiClientCli.Configuration;
using CmsRestApiClientCli.Services;
using CmsRestApiClientCli.Upsert;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace CmsRestApiClientCli.Commands;

public sealed class UpsertCommand : AsyncCommand<UpsertCommand.Settings>
{
    private readonly TokenService tokenService;

    private readonly CmsService cmsService;

    private readonly CmsInstanceOptions cmsInstanceOptions;

    private readonly IEnumerable<IUpsertPropertyGroups> propertyGroups;

    private readonly IEnumerable<IUpsertContentTypes> contentTypes;

    private readonly IEnumerable<IUpsertDisplayTemplates> displayTemplates;

    public UpsertCommand(
        TokenService tokenService,
        CmsService cmsService,
        IOptions<CmsInstanceOptions> cmsInstance,
        IEnumerable<IUpsertPropertyGroups> propertyGroups,
        IEnumerable<IUpsertContentTypes> contentTypes,
        IEnumerable<IUpsertDisplayTemplates> displayTemplates)
    {
        this.tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        this.cmsService = cmsService ?? throw new ArgumentNullException(nameof(cmsService));
        this.cmsInstanceOptions = cmsInstance?.Value ?? throw new ArgumentNullException(nameof(cmsInstance));
        this.propertyGroups = propertyGroups ?? throw new ArgumentNullException(nameof(propertyGroups));
        this.contentTypes = contentTypes ?? throw new ArgumentNullException(nameof(contentTypes));
        this.displayTemplates = displayTemplates ?? throw new ArgumentNullException(nameof(displayTemplates));
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var accessToken = await this.tokenService.GetAccessToken(
            this.cmsInstanceOptions.ClientId,
            this.cmsInstanceOptions.ClientSecret);

        var resourceType = settings.ResourceType.ToLower(CultureInfo.InvariantCulture).Trim();

        if (resourceType == "all")
        {
            await this.UpsertPropertyGroupsAsync(accessToken, settings);
            await this.UpsertContentTypesAsync(accessToken, settings);
            await this.UpsertDisplayTemplatesAsync(accessToken, settings);
        }
        else if (resourceType == "propertygroups")
        {
            await this.UpsertPropertyGroupsAsync(accessToken, settings);
        }
        else if (resourceType == "contenttypes")
        {
            await this.UpsertContentTypesAsync(accessToken, settings);
        }
        else if (resourceType == "displaytemplates")
        {
            await this.UpsertDisplayTemplatesAsync(accessToken, settings);
        }
        else
        {
            AnsiConsole.MarkupLineInterpolated($"[red]Error:[/] Invalid argument, the valid ones are: all, propertygroups, contenttypes or displaytemplates");
        }

        return 0;
    }

    private async Task UpsertPropertyGroupsAsync(string accessToken, Settings settings)
    {
        foreach (var propertyGroupsService in this.propertyGroups.OrderBy(x => x.SortIndex))
        {
            var itemsToUpsert = await propertyGroupsService.GetPropertyGroupsToUpsertAsync();

            foreach (var item in itemsToUpsert)
            {
                var jsonResult = await this.cmsService.Upsert("propertygroups", item.Key, item.Value, settings.IgnoreDataLossWarnings ?? false, accessToken);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLineInterpolated($"[bold yellow]Upsert {item.Key} to propertygroups[/]");

                var json = new JsonText(jsonResult);

                AnsiConsole.Write(
                    new Panel(json)
                        .Collapse()
                        .RoundedBorder()
                        .BorderColor(Color.Yellow));
            }
        }
    }

    private async Task UpsertContentTypesAsync(string accessToken, Settings settings)
    {
        foreach (var contentTypeService in this.contentTypes.OrderBy(x => x.SortIndex))
        {
            var itemsToUpsert = await contentTypeService.GetContentTypesToUpsertAsync();
            var contentTypesInResponse = new List<ContentType>();

            foreach (var item in itemsToUpsert)
            {
                var jsonResult = await this.cmsService.Upsert("contenttypes", item.Key, item.Value, settings.IgnoreDataLossWarnings ?? false, accessToken);

                var failed = false;

                try
                {
                    var responseItem = JsonSerializer.Deserialize<ContentType>(jsonResult);
                    contentTypesInResponse.Add(responseItem);
                }
                catch (Exception)
                {
                    failed = true;
                }

                if ((settings.WriteJson.HasValue && settings.WriteJson.Value) || failed)
                {
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLineInterpolated($"[bold yellow]Upsert {item.Key} to contenttypes[/]");

                    var json = new JsonText(jsonResult);

                    AnsiConsole.Write(
                        new Panel(json)
                            .Collapse()
                            .RoundedBorder()
                            .BorderColor(Color.Yellow));
                }
            }

            if (settings.WriteJson is null or false)
            {
                var table = new Table()
                    .BorderColor(Color.Yellow)
                    .Border(TableBorder.Rounded);

                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.Key)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.BaseType)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.DisplayName)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.Description)}[/]")));
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]{nameof(ContentType.SortOrder)}[/]")).RightAligned());
                table.AddColumn(new TableColumn(new Markup($"[bold yellow]Upsert[/]")).Centered());

                foreach (var item in itemsToUpsert)
                {
                    var rowValues = new List<IRenderable> { new Markup(item.Key) };

                    var itemInResponse = contentTypesInResponse.FirstOrDefault(x =>
                        x.Key.Equals(item.Key, StringComparison.OrdinalIgnoreCase));

                    if (itemInResponse is not null)
                    {
                        rowValues.Add(new Markup(itemInResponse.BaseType));
                        rowValues.Add(new Markup(itemInResponse.DisplayName));
                        rowValues.Add(new Markup(itemInResponse.Description));
                        rowValues.Add(new Markup(itemInResponse.SortOrder.ToString(CultureInfo.InvariantCulture)));
                        rowValues.Add(new Markup(":check_mark_button: OK"));
                    }
                    else
                    {
                        rowValues.Add(new Markup(string.Empty));
                        rowValues.Add(new Markup("ERROR"));
                        rowValues.Add(new Markup("See JSON output above table for details."));
                        rowValues.Add(new Markup(string.Empty));
                        rowValues.Add(new Markup(":cross_mark:"));
                    }

                    table.AddRow(rowValues.ToArray());
                }

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLineInterpolated($"[bold yellow]Upsert contenttypes summary[/]");
                AnsiConsole.Write(table);
            }
        }
    }

    private async Task UpsertDisplayTemplatesAsync(string accessToken, Settings settings)
    {
        foreach (var displayTemplateService in this.displayTemplates.OrderBy(x => x.SortIndex))
        {
            var itemsToUpsert = await displayTemplateService.GetDisplayTemplatesToUpsertAsync();

            foreach (var item in itemsToUpsert)
            {
                var jsonResult = await this.cmsService.Upsert("displaytemplates", item.Key, item.Value, settings.IgnoreDataLossWarnings ?? false, accessToken);

                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLineInterpolated($"[bold yellow]Upsert {item.Key} to displaytemplates[/]");

                var json = new JsonText(jsonResult);

                AnsiConsole.Write(
                    new Panel(json)
                        .Expand()
                        .Collapse()
                        .RoundedBorder()
                        .BorderColor(Color.Yellow));
            }
        }
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<all|propertygroups|contenttypes|displaytemplates>")]
        public string ResourceType { get; set; }

        [CommandOption("-i|--ignore-data-loss-warnings")]
        public bool? IgnoreDataLossWarnings { get; set; }

        [CommandOption("-j|--json")]
        public bool? WriteJson { get; set; }
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

        [JsonIgnore]
        public bool Failed { get; set; }
    }
}
