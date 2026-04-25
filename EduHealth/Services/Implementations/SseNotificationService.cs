using System.Collections.Concurrent;
using System.Text.Json;
using EduHealth.Helpers;
using EduHealth.Services.Interfaces;

namespace EduHealth.Services.Implementations
{
    public sealed class SseNotificationService : ISseNotificationService
    {
        private sealed record ClientConnection(
            int UserId,
            StreamWriter Writer,
            CancellationToken CancellationToken,
            DateTime ConnectedAt);

        private readonly ConcurrentDictionary<int, ClientConnection> _activeConnections = new();
        private readonly SemaphoreSlim _connectionLock = new(1, 1);
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public async Task AddClientAsync(int userId, IAsyncEnumerable<string> channel, CancellationToken cancellationToken)
        {
            // This is called when client connects to SSE stream
            // The actual StreamWriter is set by the controller
            await Task.Delay(0); // Placeholder for future tracking if needed
        }

        public async Task RemoveClientAsync(int userId)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if (_activeConnections.TryRemove(userId, out var connection))
                {
                    try
                    {
                        await connection.Writer.FlushAsync();
                        connection.Writer.Dispose();
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public async Task BroadcastNotificationCreatedAsync(int notificationId, int[] recipientUserIds, CancellationToken cancellationToken = default)
        {
            var @event = new SseEventDto
            {
                EventType = "NOTIFICATION_CREATED",
                Timestamp = VietnamTimeHelper.Now,
                Data = new { notificationId, recipientCount = recipientUserIds.Length }
            };

            await BroadcastToUsersAsync(recipientUserIds, @event, cancellationToken);
        }

        public async Task BroadcastNotificationReadAsync(int userId, int notificationId, CancellationToken cancellationToken = default)
        {
            var @event = new SseEventDto
            {
                EventType = "NOTIFICATION_READ",
                Timestamp = VietnamTimeHelper.Now,
                Data = new { notificationId, userId }
            };

            await BroadcastToUsersAsync(new[] { userId }, @event, cancellationToken);
        }

        public async Task BroadcastAllNotificationsReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var @event = new SseEventDto
            {
                EventType = "ALL_NOTIFICATIONS_READ",
                Timestamp = VietnamTimeHelper.Now,
                Data = new { userId }
            };

            await BroadcastToUsersAsync(new[] { userId }, @event, cancellationToken);
        }

        public async Task BroadcastUnreadCountChangeAsync(int userId, int unreadCount, CancellationToken cancellationToken = default)
        {
            var @event = new SseEventDto
            {
                EventType = "UNREAD_COUNT_CHANGED",
                Timestamp = VietnamTimeHelper.Now,
                Data = new { userId, unreadCount }
            };

            await BroadcastToUsersAsync(new[] { userId }, @event, cancellationToken);
        }

        internal async Task RegisterConnectionAsync(int userId, StreamWriter writer, CancellationToken cancellationToken)
        {
            await _connectionLock.WaitAsync(cancellationToken);
            try
            {
                var connection = new ClientConnection(userId, writer, cancellationToken, VietnamTimeHelper.Now);
                _activeConnections.AddOrUpdate(userId, connection, (_, _) => connection);
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        private async Task BroadcastToUsersAsync(int[] userIds, SseEventDto @event, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(@event, JsonOptions);
            var message = FormatSseMessage(json);

            foreach (var userId in userIds.Distinct())
            {
                if (_activeConnections.TryGetValue(userId, out var connection))
                {
                    try
                    {
                        await connection.Writer.WriteAsync(message);
                        await connection.Writer.FlushAsync();
                    }
                    catch
                    {
                        // Connection failed, remove it
                        await RemoveClientAsync(userId);
                    }
                }
            }
        }

        private static string FormatSseMessage(string data)
        {
            // SSE format: "data: {json}\n\n"
            return $"data: {data}\n\n";
        }
    }

    internal sealed class SseEventDto
    {
        public string EventType { get; set; } = null!;
        public DateTime Timestamp { get; set; }
        public object? Data { get; set; }
    }
}
