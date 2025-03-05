using System.Collections.Generic;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert;

public interface IUpsertDisplayTemplates
{
    int SortIndex { get; set; }

    Task<Dictionary<string, string>> GetDisplayTemplatesToUpsertAsync();
}