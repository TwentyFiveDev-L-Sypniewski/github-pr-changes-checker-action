using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace GithubPrChangesChecker;

public class GithubClient
{
    private readonly HttpClient _httpClient;

    public GithubClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string[]> GetChangedProjectsNames(string owner, string name, string prNumber, string token)
    {
        AuthenticationHeaderValue authHeader = new("token", token);
        _httpClient.DefaultRequestHeaders.Authorization = authHeader;

        var requestUri = $"/repos/{owner}/{name}/pulls/{prNumber}/files";
        var githubResponse = new List<GithubFileChange>();

        var pageNo = 1;
        while (true)
        {
            var uriWithParams = $"{requestUri}?per_page=100&page={pageNo}";
            var pageResults = await _httpClient.GetFromJsonAsync<GithubFileChange[]>(uriWithParams);

            if (pageResults is null || pageResults.Length == 0)
            {
                break;
            }
            githubResponse.AddRange(pageResults ?? Array.Empty<GithubFileChange>());
            pageNo++;
        }

        var projectNames = githubResponse?.Select(x => x.Filename.Split('/').First()).Distinct().ToArray();
        return projectNames ?? Array.Empty<string>();
    }
}