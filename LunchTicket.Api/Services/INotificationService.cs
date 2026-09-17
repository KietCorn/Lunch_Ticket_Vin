using System.Threading.Channels;
using LunchTicket.Api.DTOs;

namespace LunchTicket.Api.Services;

public interface INotificationService
{
    ChannelReader<NotificationEvent> Subscribe();
    void Unsubscribe(ChannelReader<NotificationEvent> reader);
    Task PublishAsync(NotificationEvent notificationEvent);
}
