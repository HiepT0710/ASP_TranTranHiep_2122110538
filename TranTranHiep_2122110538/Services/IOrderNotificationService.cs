using TranTranHiep_2122110538.Models;

namespace TranTranHiep_2122110538.Services;

public interface IOrderNotificationService
{
    Task BroadcastOrderAsync(Order order);
    void LogEmailStub(string subject, string body);
    void LogSmsStub(string phone, string body);
}
