// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

using SimpleChat.Data;
using SimpleChat.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ---- Services -----------------------------------------------------------
builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatRepository>();

var app = builder.Build();

// ---- Middleware ---------------------------------------------------------
app.UseDefaultFiles();   // serves wwwroot/index.html at "/"
app.UseStaticFiles();

// ---- REST API endpoints -------------------------------------------------

// LOGIN: verify the user id exists in SQL. Returns the user, or 401.
app.MapPost("/api/login", async (LoginRequest req, ChatRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.UserName))
        return Results.BadRequest(new { message = "User ID is required." });

    var user = await repo.GetUserByUserNameAsync(req.UserName.Trim());
    if (user is null)
        return Results.Json(new { message = "User not found in database." }, statusCode: 401);

    return Results.Ok(user);
});

// SEARCH other users to chat with.
app.MapGet("/api/users/search", async (string? term, int me, ChatRepository repo) =>
{
    var results = await repo.SearchUsersAsync(term?.Trim() ?? string.Empty, me);
    return Results.Ok(results);
});

// LOAD conversation history between two users.
app.MapGet("/api/messages", async (int me, int other, ChatRepository repo) =>
{
    var history = await repo.GetConversationAsync(me, other);
    return Results.Ok(history);
});

// GET unread counts grouped by sender -> [{ senderId, count }]
app.MapGet("/api/unread", async (int me, ChatRepository repo) =>
{
    var counts = await repo.GetUnreadCountsAsync(me);
    return Results.Ok(counts);
});

// MARK a conversation as read
app.MapPost("/api/read", async (MarkReadRequest req, ChatRepository repo) =>
{
    await repo.MarkAsReadAsync(req.Me, req.Other);
    return Results.Ok();
});

// Users the current user has already chatted with
app.MapGet("/api/conversations", async (int me, ChatRepository repo) =>
{
var partners = await repo.GetConversationPartnersAsync(me);
return Results.Ok(partners);
});

// ---- SignalR hub --------------------------------------------------------
app.MapHub<ChatHub>("/chatHub");

app.Run();

// Request DTO for login
public record LoginRequest(string UserName);

public record MarkReadRequest(int Me, int Other);
