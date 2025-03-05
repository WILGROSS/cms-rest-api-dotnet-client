using System.Collections.Generic;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert.PlainJsonFiles;

public class PlainJsonPropertyGroupsService(string folderPath) : JsonFilesServiceBase, IUpsertPropertyGroups
{
    public int SortIndex { get; set; } = 0;

    public Task<Dictionary<string, string>> GetPropertyGroupsToUpsertAsync()
    {
        return this.GetFilesToUpsertAsync(folderPath);
    }
}