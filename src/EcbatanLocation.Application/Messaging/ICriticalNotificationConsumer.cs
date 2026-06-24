namespace EcbatanLocation.Application.Messaging;

public interface ICriticalNotificationConsumer<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
