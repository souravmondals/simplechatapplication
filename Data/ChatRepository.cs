// <author>Sourav Mondal/author>
// <summary>Chat application</summary>

using Dapper;
using Microsoft.Data.SqlClient;
using SimpleChat.Models;

namespace SimpleChat.Data;

/// <summary>
/// All SQL data access for the chat app, using Dapper (micro ORM).
/// </summary>
public class ChatRepository
{
    private readonly string _connectionString;

    public ChatRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ConnectionString")
            ?? throw new InvalidOperationException("ConnectionString is not configured.");
    }

    private SqlConnection Connection => new(_connectionString);

    // ---- LOGIN: verify the user id / username exists in SQL ------------
    public async Task<User?> GetUserByUserNameAsync(string userName)
    {
        const string sql = @"SELECT UserId, UserName, DisplayName
                             FROM dbo.Users
                             WHERE UserName = @userName;";
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { userName });
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        const string sql = @"SELECT UserId, UserName, DisplayName
                             FROM dbo.Users
                             WHERE UserId = @userId;";
        using var conn = Connection;
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { userId });
    }

    // ---- SEARCH other users to chat with ------------------------------
    public async Task<IEnumerable<User>> SearchUsersAsync(string term, int excludeUserId)
    {
        const string sql = @"SELECT TOP 20 UserId, UserName, DisplayName
                             FROM dbo.Users
                             WHERE UserId <> @excludeUserId
                               AND (@term = '' OR UserName LIKE '%' + @term + '%'
                                                OR DisplayName LIKE '%' + @term + '%')
                             ORDER BY DisplayName;";
        using var conn = Connection;
        return await conn.QueryAsync<User>(sql, new { term, excludeUserId });
    }

    // ---- SAVE a message -----------------------------------------------
    public async Task<long> SaveMessageAsync(int senderId, int receiverId, string content)
    {
        const string sql = @"INSERT INTO dbo.Messages (SenderId, ReceiverId, Content)
                             OUTPUT INSERTED.MessageId
                             VALUES (@senderId, @receiverId, @content);";
        using var conn = Connection;
        return await conn.ExecuteScalarAsync<long>(sql, new { senderId, receiverId, content });
    }

    // ---- LOAD conversation history between two users ------------------
    public async Task<IEnumerable<Message>> GetConversationAsync(int userA, int userB)
    {
        const string sql = @"SELECT TOP 200
                                m.MessageId, m.SenderId, m.ReceiverId, m.Content, m.SentAt,
                                u.DisplayName AS SenderName
                             FROM dbo.Messages m
                             JOIN dbo.Users u ON u.UserId = m.SenderId
                             WHERE (m.SenderId = @userA AND m.ReceiverId = @userB)
                                OR (m.SenderId = @userB AND m.ReceiverId = @userA)
                             ORDER BY m.SentAt ASC;";
        using var conn = Connection;
        return await conn.QueryAsync<Message>(sql, new { userA, userB });
    }

    // ---- UNREAD counts grouped by sender ------------------------------
    public async Task<IEnumerable<UnreadCount>> GetUnreadCountsAsync(int me)
    {
        const string sql = @"SELECT SenderId, COUNT(*) AS Count
                             FROM dbo.Messages
                             WHERE ReceiverId = @me AND IsRead = 0
                             GROUP BY SenderId;";
        using var conn = Connection;
        return await conn.QueryAsync<UnreadCount>(sql, new { me });
    }

    // ---- MARK a conversation as read ----------------------------------
    public async Task MarkAsReadAsync(int me, int other)
    {
        const string sql = @"UPDATE dbo.Messages
                             SET IsRead = 1
                             WHERE ReceiverId = @me AND SenderId = @other AND IsRead = 0;";
        using var conn = Connection;
        await conn.ExecuteAsync(sql, new { me, other });
    }


    // ---- Users the current user has chatted with -----------------------
    public async Task<IEnumerable<User>> GetConversationPartnersAsync(int me)
    {
        const string sql = @"SELECT u.UserId, u.UserName, u.DisplayName
                             FROM dbo.Users u
                             WHERE u.UserId <> @me
                               AND u.UserId IN (
                                   SELECT CASE WHEN m.SenderId = @me
                                               THEN m.ReceiverId ELSE m.SenderId END
                                   FROM dbo.Messages m
                                   WHERE m.SenderId = @me OR m.ReceiverId = @me
                               )
                             ORDER BY u.DisplayName;";
        using var conn = Connection;
        return await conn.QueryAsync<User>(sql, new { me });
    }


}

public class UnreadCount
{
    public int SenderId { get; set; }
    public int Count { get; set; }
}