namespace ConfHub.Scheduling.Solution;

// Lời giải một phần hoặc đầy đủ: phiên nào ở mốc nào, phòng nào.
// Lớp này KHÔNG tự kiểm ràng buộc; việc đó của ConstraintChecker.
public sealed class Schedule
{
    // Ánh xạ sessionId -> Placement
    private readonly Dictionary<int, Placement> _placements = [];

    // Thứ tự các phiên được đặt (đặt lại phiên cũ không đổi thứ tự).
    private readonly List<int> _placedSessionIds = [];

    // Các phiên đã đặt, theo thứ tự đặt lần đầu.
    public IReadOnlyList<int> PlacedSessionIds => _placedSessionIds;

    // Thêm hoặc ghi đè vị trí của phiên.
    // schedule.Place(1, 0, 101); // AI → 8:00, phòng 101
    public void Place(int sessionId, int slotIndex, int? roomId = null)
    {
        if (!_placedSessionIds.Contains(sessionId))
        {
            _placedSessionIds.Add(sessionId);
        }

        _placements[sessionId] = new Placement(slotIndex, roomId);
    }

    // Gán hoặc đổi phòng cho phiên đã có giờ (giai đoạn 1 chọn giờ trước, gán phòng sau).
    public void SetRoom(int sessionId, int? roomId)
    {
        if (!_placements.TryGetValue(sessionId, out var placement))
        {
            throw new InvalidOperationException($"Phiên {sessionId} chưa được đặt giờ nên không gán phòng được.");
        }

        _placements[sessionId] = new Placement(placement.SlotIndex, roomId);
    }

    // Bỏ phiên khỏi lịch.
    public void Remove(int sessionId)
    {
        if (_placements.Remove(sessionId))
        {
            _placedSessionIds.Remove(sessionId);
        }
    }

    // Phiên đã có giờ chưa.
    public bool IsPlaced(int sessionId)
    {
        return _placements.ContainsKey(sessionId);
    }

    // Lấy vị trí của phiên (false nếu chưa đặt).
    /*
        schedule.Place(1, 0, 101);

        bool found = schedule.TryGetPlacement(1, out var placement);

        Console.WriteLine(found);               // true
        Console.WriteLine(placement.SlotIndex); // 0
        Console.WriteLine(placement.RoomId);    // 101
    */
    public bool TryGetPlacement(int sessionId, out Placement placement)
    {
        return _placements.TryGetValue(sessionId, out placement);
    }

    // Sao chép sâu: sửa bản sao không ảnh hưởng bản gốc (thử biến đổi rồi bỏ nếu không tốt hơn).
    public Schedule Clone()
    {
        var copy = new Schedule();
        foreach (var sessionId in _placedSessionIds)
        {
            var placement = _placements[sessionId];
            copy.Place(sessionId, placement.SlotIndex, placement.RoomId);
        }

        return copy;
    }
}
