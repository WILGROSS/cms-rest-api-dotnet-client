using System.Collections.Generic;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert;

public interface IUpsertContentTypes
{
    int SortIndex { get; set; }

    Task<Dictionary<string, string>> GetContentTypesToUpsertAsync();
}