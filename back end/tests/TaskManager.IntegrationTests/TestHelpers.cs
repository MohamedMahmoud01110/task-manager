using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskManager.IntegrationTests;

public sealed record TestUser(string Id, string Email, HttpClient Client);

public static class TestHelpers
{
    public const string Password = "Passw0rd1";

    public static async Task<TestUser> RegisterAsync(this ApiFactory f, string? email = null)
    {
        email ??= $"{Guid.NewGuid():N}@test.com";
        var client = f.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register", new { name = "Test", email, password = Password });
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return new TestUser(json.GetProperty("user").GetProperty("id").GetString()!, email, client);
    }

    /// <summary>Logs in as the seeded admin (default credentials from appsettings.json).</summary>
    public static async Task<HttpClient> AdminClientAsync(this ApiFactory f)
    {
        var client = f.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@taskmanager.local", password = "Admin@12345" });
        res.EnsureSuccessStatusCode();
        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
        return client;
    }

    public static Task<HttpResponseMessage> CreateTaskAsync(this HttpClient c, string title, string priority = "Low") =>
        c.PostAsJsonAsync("/api/tasks", new { title, priority });

    public static async Task<string> CreateTaskIdAsync(this HttpClient c, string? title = null)
    {
        var res = await c.CreateTaskAsync(title ?? "Task " + Guid.NewGuid().ToString("N"));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString()!;
    }

    public static async Task<string> GetStatusAsync(this HttpClient c, string id)
    {
        var json = await c.GetFromJsonAsync<JsonElement>($"/api/tasks/{id}");
        return json.GetProperty("status").GetString()!;
    }

    public static async Task<string> WaitForStatusAsync(this HttpClient c, string id, string expected, int seconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        var status = await c.GetStatusAsync(id);
        while (status != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(300);
            status = await c.GetStatusAsync(id);
        }
        return status;
    }
}