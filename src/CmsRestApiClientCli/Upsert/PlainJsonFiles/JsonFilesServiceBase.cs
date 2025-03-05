using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CmsRestApiClientCli.Upsert.PlainJsonFiles;

public class JsonFilesServiceBase
{
    public async Task<Dictionary<string, string>> GetFilesToUpsertAsync(string folderPath)
    {
        var di = new DirectoryInfo(folderPath);
        var files = di.EnumerateFiles("*.json", SearchOption.AllDirectories);

        var d = new Dictionary<string, string>();

        foreach (var fileInfo in files)
        {
            d.Add(
                fileInfo.Name.Replace(".json", string.Empty),
                await File.ReadAllTextAsync(fileInfo.FullName, Encoding.UTF8).ConfigureAwait(false));
        }

        return d;
    }
}