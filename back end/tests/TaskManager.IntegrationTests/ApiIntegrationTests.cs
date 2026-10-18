using System.Net;
using System.Net.Http.Json;

namespace TaskManager.IntegrationTests;

public class ApiIntegrationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // ---------- Duplicate title (enforced by the database) ----------
    [Fact]
    public async Task Concurrent_identical_creates_give_exactly_one_201_and_the_rest_409()
    {
        var user = await factory.RegisterAsync();
        var title = "Race " + Guid.NewGuid().ToString("N");

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => user.Client.CreateTaskAsync(title)));
        var codes = responses.Select(r => (int)r.StatusCode).ToList();

        Assert.Equal(1, codes.Count(c => c == 201));
        Assert.Equal(9, codes.Count(c => c == 409));
    }

    [Fact]
    public async Task Duplicate_ignores_case_and_edge_spaces_but_is_per_user()
    {
        var alice = await factory.RegisterAsync();
        var bob = await factory.RegisterAsync();
        var title = "Buy milk " + Guid.NewGuid().ToString("N");

        Assert.Equal(HttpStatusCode.Created, (await alice.Client.CreateTaskAsync(title)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await alice.Client.CreateTaskAsync($"  {title.ToUpperInvariant()}  ")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await bob.Client.CreateTaskAsync(title)).StatusCode);
    }

    // ---------- Unique emails ----------
    [Fact]
    public async Task Emails_are_case_insensitive_even_for_concurrent_registers()
    {
        var local = Guid.NewGuid().ToString("N");

        var responses = await Task.WhenAll(new[] { $"{local}@Example.com", $"{local}@example.com", $"{local.ToUpperInvariant()}@EXAMPLE.COM" }
            .Select(email => factory.CreateClient().PostAsJsonAsync("/api/auth/register",
                new { name = "Ali", email, password = TestHelpers.Password })));
        var codes = responses.Select(r => (int)r.StatusCode).ToList();

        Assert.Equal(1, codes.Count(c => c == 201));
        Assert.Equal(2, codes.Count(c => c == 409));
    }

    // ---------- Ownership ----------
    [Fact]
    public async Task Another_users_task_is_404_for_get_update_and_delete()
    {
        var alice = await factory.RegisterAsync();
        var bob = await factory.RegisterAsync();
        var id = await alice.Client.CreateTaskIdAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await bob.Client.GetAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.Client.PutAsJsonAsync($"/api/tasks/{id}",
            new { title = "Hacked", priority = "Low" })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await bob.Client.DeleteAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await alice.Client.GetAsync($"/api/tasks/{id}")).StatusCode);
    }

    // ---------- Auth ----------
    [Fact]
    public async Task Token_issued_before_deletion_is_rejected_with_401_and_login_fails()
    {
        var user = await factory.RegisterAsync();
        var admin = await factory.AdminClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync("/api/auth/me")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/admin/users/{user.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await user.Client.GetAsync("/api/auth/me")).StatusCode);
        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login",
            new { email = user.Email, password = TestHelpers.Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_need_the_admin_role()
    {
        var user = await factory.RegisterAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await user.Client.GetAsync("/api/admin/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync("/api/admin/users")).StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_delete_own_account()
    {
        var admin = await factory.AdminClientAsync();
        var me = await admin.GetFromJsonAsync<System.Text.Json.JsonElement>("/api/auth/me");
        var id = me.GetProperty("id").GetString();

        Assert.Equal(HttpStatusCode.BadRequest, (await admin.DeleteAsync($"/api/admin/users/{id}")).StatusCode);
    }

    // ---------- Background worker ----------
    [Fact]
    public async Task Worker_moves_a_new_task_from_pending_to_in_progress()
    {
        var user = await factory.RegisterAsync();
        var id = await user.Client.CreateTaskIdAsync();

        Assert.Equal("Pending", await user.Client.GetStatusAsync(id));
        Assert.Equal("InProgress", await user.Client.WaitForStatusAsync(id, "InProgress"));
    }

    [Fact]
    public async Task Worker_does_not_overwrite_a_task_the_user_already_finished()
    {
        var user = await factory.RegisterAsync();
        var id = await user.Client.CreateTaskIdAsync();

        // The worker waits 2 seconds. The user finishes first.
        var patch = await user.Client.PatchAsJsonAsync($"/api/tasks/{id}/status", new { status = "Done" });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        await Task.Delay(TimeSpan.FromSeconds(4)); // the worker has run by now
        Assert.Equal("Done", await user.Client.GetStatusAsync(id));
    }

    // ---------- Workflow, validation, errors ----------
    [Fact]
    public async Task Status_cannot_move_backwards()
    {
        var user = await factory.RegisterAsync();
        var id = await user.Client.CreateTaskIdAsync();
        await user.Client.PatchAsJsonAsync($"/api/tasks/{id}/status", new { status = "Done" });

        var back = await user.Client.PatchAsJsonAsync($"/api/tasks/{id}/status", new { status = "Pending" });

        Assert.Equal(HttpStatusCode.BadRequest, back.StatusCode);
    }

    [Fact]
    public async Task Invalid_input_returns_400_problem_details_not_500()
    {
        var user = await factory.RegisterAsync();

        var badEnum = await user.Client.PostAsJsonAsync("/api/tasks", new { title = "x", priority = "Urgent" });
        var pastDue = await user.Client.PostAsJsonAsync("/api/tasks",
            new { title = "Past", priority = "Low", dueDate = DateTime.UtcNow.AddDays(-1) });
        var bigPage = await user.Client.GetAsync("/api/tasks?pageSize=51");
        var spaces = await user.Client.CreateTaskAsync("     ");

        Assert.All(new[] { badEnum, pastDue, bigPage, spaces }, r => Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode));
        Assert.Equal("application/problem+json", badEnum.Content.Headers.ContentType?.MediaType);
    }

    // ---------- Redis down ----------
    [Fact]
    public async Task Api_keeps_working_when_redis_is_down()
    {
        // This whole test host runs with Redis unreachable, and it still started and seeded.
        var user = await factory.RegisterAsync();
        var id = await user.Client.CreateTaskIdAsync();

        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync($"/api/tasks/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await user.Client.GetAsync($"/api/tasks/{id}")).StatusCode);
    }
}