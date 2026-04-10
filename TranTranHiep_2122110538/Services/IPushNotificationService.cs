namespace TranTranHiep_2122110538.Services;

/// <summary>Gửi Web Push tới mọi thiết bị đã đăng ký của user (VAPID).</summary>
public interface IPushNotificationService
{
    Task SendToUserAsync(int userId, string title, string body, CancellationToken ct = default);
}
