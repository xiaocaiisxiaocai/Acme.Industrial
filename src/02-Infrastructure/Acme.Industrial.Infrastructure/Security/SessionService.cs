using System.Collections.Concurrent;
using Acme.Industrial.Core.Logging;

namespace Acme.Industrial.Infrastructure.Security;

/// <summary>
/// 用户会话信息。
/// </summary>
public class UserSession
{
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Email { get; init; }
    public string? IpAddress { get; init; }
    public DateTime LoginTime { get; init; } = DateTime.Now;
    public DateTime LastActivityTime { get; set; } = DateTime.Now;
    public Dictionary<string, object?> Items { get; init; } = new();
    public bool IsAuthenticated { get; set; } = true;
}

/// <summary>
/// 会话服务接口。
/// </summary>
public interface ISessionService
{
    UserSession CreateSession(string userId, string userName, string? displayName = null, string? email = null);
    UserSession? GetSession(string sessionId);
    void UpdateSession(string sessionId);
    void RemoveSession(string sessionId);
    IReadOnlyList<UserSession> GetActiveSessions();
    bool ValidateSession(string sessionId);
}

/// <summary>
/// 内存会话服务实现。
/// </summary>
public class InMemorySessionService : ISessionService, IDisposable
{
    private readonly ConcurrentDictionary<string, UserSession> _sessions = new();
    private readonly IAppLogger _logger;
    private readonly System.Threading.Timer _cleanupTimer;
    private readonly TimeSpan _sessionTimeout;
    private bool _disposed;

    public InMemorySessionService(IAppLogger logger, TimeSpan? sessionTimeout = null)
    {
        _logger = logger;
        _sessionTimeout = sessionTimeout ?? TimeSpan.FromMinutes(30);
        _cleanupTimer = new System.Threading.Timer(CleanupExpiredSessions, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public UserSession CreateSession(string userId, string userName, string? displayName = null, string? email = null)
    {
        var session = new UserSession
        {
            UserId = userId,
            UserName = userName,
            DisplayName = displayName,
            Email = email,
            IpAddress = GetClientIpAddress()
        };

        _sessions[session.SessionId] = session;
        _logger.Info($"会话已创建: UserId={userId}, SessionId={session.SessionId}");

        return session;
    }

    public UserSession? GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            if (IsSessionExpired(session))
            {
                RemoveSession(sessionId);
                return null;
            }

            session.LastActivityTime = DateTime.Now;
            return session;
        }

        return null;
    }

    public void UpdateSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.LastActivityTime = DateTime.Now;
        }
    }

    public void RemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            _logger.Info($"会话已移除: SessionId={sessionId}, UserId={session.UserId}");
        }
    }

    public IReadOnlyList<UserSession> GetActiveSessions()
    {
        return _sessions.Values
            .Where(s => !IsSessionExpired(s))
            .ToList();
    }

    public bool ValidateSession(string sessionId)
    {
        var session = GetSession(sessionId);
        return session != null && session.IsAuthenticated;
    }

    private bool IsSessionExpired(UserSession session)
    {
        return DateTime.Now - session.LastActivityTime > _sessionTimeout;
    }

    private void CleanupExpiredSessions(object? state)
    {
        var expired = _sessions.Values
            .Where(IsSessionExpired)
            .Select(s => s.SessionId)
            .ToList();

        foreach (var sessionId in expired)
        {
            RemoveSession(sessionId);
        }

        if (expired.Count > 0)
        {
            _logger.Debug($"已清理 {expired.Count} 个过期会话");
        }
    }

    private static string GetClientIpAddress()
    {
        return "127.0.0.1";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cleanupTimer.Dispose();
        _sessions.Clear();
    }
}
