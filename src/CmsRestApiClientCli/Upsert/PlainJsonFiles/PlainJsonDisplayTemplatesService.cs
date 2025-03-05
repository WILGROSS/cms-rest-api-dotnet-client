using System.Collections.Generic;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert.PlainJsonFiles;

public class PlainJsonDisplayTemplatesService(string folderPath) : JsonFilesServiceBase, IUpsertDisplayTemplates
{
    public int SortIndex { get; set; } = 0;

    public Task<Dictionary<string, string>> GetDisplayTemplatesToUpsertAsync()
    {
        return this.GetFilesToUpsertAsync(folderPath);
    }
}