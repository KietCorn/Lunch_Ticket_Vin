using System.Collections.Concurrent;
using System.Threading.Channels;
using LunchTicket.Api.DTOs;

namespace LunchTicket.Api.Services;

public class NotificationService : INotificationService
{
    private readonly ConcurrentDictionary<ChannelReader<NotificationEvent>, Channel<NotificationEvent>> _subscribers = new();

    public ChannelReader<NotificationEvent> Subscribe()
    {
        var channel = Channel.CreateUnbounded<NotificationEvent>();
        _subscribers[channel.Reader] = channel;
        return channel.Reader;
    }

    public void Unsubscribe(ChannelReader<NotificationEvent> reader)
    {
        _subscribers.TryRemove(reader, out _);
    }

    public async Task PublishAsync(NotificationEvent notificationEvent)
    {
        foreach (var channel in _subscribers.Values)
        {
            await channel.Writer.WriteAsync(notificationEvent);
        }
    }
}
