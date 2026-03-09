using System.Data;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GitHubActivity;

class Program
{
    private static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: github-activity <username>");
            return;
        }

        string username = args[0];
        string url = $"https://api.github.com/users/{username}/events";

        try
        {   
            client.DefaultRequestHeaders.Add("User-Agent", "GitHubActivity");
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Error: User '{username}' not found.");
                return;
            }

            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            JsonElement root = doc.RootElement;

            if (root.GetArrayLength() == 0)
            {
                Console.WriteLine($"No recent activity found for {username}.");
                return;
            }

            Console.WriteLine($"Output:");
            foreach (JsonElement @event in root.EnumerateArray())
            {
                string type = @event.GetProperty("type").GetString() ?? "";
                string repoName = @event.GetProperty("repo").GetProperty("name").GetString() ?? "unknown repo";
                JsonElement payload = @event.GetProperty("payload");
                
                string activityDescription = type switch
                {
                    "PushEvent" => $"Pushed {(payload.TryGetProperty("size", out var s) ? s.GetInt32() : 0)} commits to {repoName}",
                    
                    "IssuesEvent" => $"{(payload.TryGetProperty("action", out var a) ? Capitalize(a.GetString()) : "Opened")} an issue in {repoName}",
                    
                    "WatchEvent" => $"Starred {repoName}",
                    
                    "CreateEvent" => $"Created {(payload.TryGetProperty("ref_type", out var r) ? r.GetString() : "resource")} in {repoName}",
                    
                    "ForkEvent" => $"Forked {repoName}",
                    
                    _ => $"{type.Replace("Event", "")} in {repoName}"
                };

                Console.WriteLine($"- {activityDescription}");
            }
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Network error: {e.Message}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }

    static string Capitalize(string? s) => 
        string.IsNullOrEmpty(s) ? "" : char.ToUpper(s[0]) + s.Substring(1);
}