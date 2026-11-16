namespace ConfHub.Api.IntegrationTests.Support;

// Đồng hồ giả: mặc định chạy theo giờ thật, có thể truyền mốc cố định cho test nghiệp vụ.
// Cộng thêm khoảng "tua" để kiểm hết hạn token mà không chờ thật 24 giờ.
public sealed class TestClock(DateTimeOffset? fixedUtcNow = null) : TimeProvider
{
    private long _offsetTicks;

    public void Advance(TimeSpan duration)
    {
        Interlocked.Add(ref _offsetTicks, duration.Ticks);
    }

    public override DateTimeOffset GetUtcNow()
    {
        var utcNow = fixedUtcNow ?? base.GetUtcNow();
        return utcNow + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));
    }
}
