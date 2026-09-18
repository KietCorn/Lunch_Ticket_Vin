using System.Text.Json;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/kitchen")]
[Authorize(Roles = "staff,admin")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";

        var reader = _notificationService.Subscribe();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var notificationEvent = await reader.ReadAsync(cancellationToken);
                var json = JsonSerializer.Serialize(notificationEvent);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // client disconnected
        }
        finally
        {
            _notificationService.Unsubscribe(reader);
        }
    }
}
