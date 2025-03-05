using System.Collections.Generic;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert.PlainJsonFiles;

public class PlainJsonContentTypesService(string folderPath) : JsonFilesServiceBase, IUpsertContentTypes
{
    public int SortIndex { get; set; } = 0;

    public Task<Dictionary<string, string>> GetContentTypesToUpsertAsync()
    {
        return this.GetFilesToUpsertAsync(folderPath);
    }
}