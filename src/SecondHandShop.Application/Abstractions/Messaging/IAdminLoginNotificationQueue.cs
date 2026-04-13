namespace SecondHandShop.Application.Abstractions.Messaging;

public interface IAdminLoginNotificationQueue
{
    void Enqueue(AdminLoginNotificationMessage message);
}
