using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TranTranHiep_2122110538.Data;
using WebPush;

namespace TranTranHiep_2122110538.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PushNotificationService> _log;

    public PushNotificationService(AppDbContext db, IConfiguration config, ILogger<PushNotificationService> log)
    {
        _db = db;
        _config = config;
        _log = log;
    }

    public async Task SendToUserAsync(int userId, string title, string body, CancellationToken ct = default)
    {
        var pub = _config["WebPush:PublicKey"];
        var prv = _config["WebPush:PrivateKey"];
        var subj = _config["WebPush:Subject"];
        if (string.IsNullOrWhiteSpace(pub) || string.IsNullOrWhiteSpace(prv))
        {
            _log.LogDebug("WebPush: bỏ qua (chưa cấu hình VAPID).");
            return;
        }

        if (string.IsNullOrWhiteSpace(subj))
            subj = "mailto:admin@localhost";

        var subs = await _db.PushSubscriptions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct);
        if (subs.Count == 0)
            return;

        var vapid = new VapidDetails(subj, pub, prv);
        var client = new WebPushClient();
        var payload = JsonConvert.SerializeObject(new { title, body });

        foreach (var s in subs)
        {
            try
            {
                var subscription = new PushSubscription(s.Endpoint, s.P256dh, s.Auth);
                await client.SendNotificationAsync(subscription, payload, vapid);
            }
            catch (WebPushException ex) when (ex.StatusCode == HttpStatusCode.Gone)
            {
                var entity = await _db.PushSubscriptions.FirstOrDefaultAsync(x => x.Id == s.Id, ct);
                if (entity != null)
                    _db.PushSubscriptions.Remove(entity);
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "WebPush lỗi gửi tới {Endpoint}", s.Endpoint);
            }
        }
    }
}
