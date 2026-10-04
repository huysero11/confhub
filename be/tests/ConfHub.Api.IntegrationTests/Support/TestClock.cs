namespace ConfHub.Api.IntegrationTests.Support;

// Đồng hồ giả: chạy theo giờ thật nhưng cộng thêm một khoảng "tua" → test hết hạn token
// mà không phải chờ thật 24 giờ.
public sealed class TestClock : TimeProvider
{
    private long _offsetTicks;

    public void Advance(TimeSpan duration)
    {
        Interlocked.Add(ref _offsetTicks, duration.Ticks);
    }

    public override DateTimeOffset GetUtcNow()
    {
        return base.GetUtcNow() + TimeSpan.FromTicks(Interlocked.Read(ref _offsetTicks));
    }
}
