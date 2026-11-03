using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Generation;

// Dựng bài toán sau biến động cho test và thực nghiệm T2.4.
// Chỉ thay phòng hoặc giờ rảnh của một diễn giả; dữ liệu sinh ban đầu không bị sửa.
public static class ChangeScenario
{
    // ---------- MẤT PHÒNG ----------
    // Mô phỏng một phòng không còn dùng được.
    public static SchedulingProblem RemoveRoom(GeneratedData data, int roomId)
    {
        ValidateData(data);
        var problem = data.Problem;
        problem.GetRoom(roomId);
        var rooms = new List<SchedulingRoom>();
        foreach (var room in problem.Rooms)
        {
            if (room.Id != roomId)
            {
                rooms.Add(room);
            }
        }

        return new SchedulingProblem(problem.Sessions, rooms, problem.Days, problem.SpeakerAvailabilities, data.Interests, problem.SlotMinutes);
    }

    // ---------- DIỄN GIẢ BẬN MỘT NGÀY ----------
    // Loại ngày bận khỏi giờ rảnh của diễn giả.
    public static SchedulingProblem MakeSpeakerUnavailable(GeneratedData data, int speakerId, int dayIndex)
    {
        ValidateData(data);
        var problem = data.Problem;
        if (dayIndex < 0 || dayIndex >= problem.Days.Count)
        {
            throw new ArgumentException("Ngày bận không tồn tại.", nameof(dayIndex));
        }

        var hasSpeaker = false;
        foreach (var session in problem.Sessions)
        {
            if (session.SpeakerIds.Contains(speakerId))
            {
                hasSpeaker = true;
                break;
            }
        }

        if (!hasSpeaker)
        {
            throw new ArgumentException("Diễn giả không có phiên trong bài toán.", nameof(speakerId));
        }

        var availabilities = new List<SpeakerAvailability>();
        var hasDeclaredAvailability = false;
        var remainingAvailabilityCount = 0;
        foreach (var availability in problem.SpeakerAvailabilities)
        {
            if (availability.SpeakerId != speakerId)
            {
                availabilities.Add(availability);
                continue;
            }

            hasDeclaredAvailability = true;
            if (availability.DayIndex != dayIndex)
            {
                availabilities.Add(availability);
                remainingAvailabilityCount++;
            }
        }

        // Không khai nghĩa là rảnh mọi lúc: phải khai tường minh các ngày còn lại.
        if (!hasDeclaredAvailability)
        {
            for (var remainingDayIndex = 0; remainingDayIndex < problem.Days.Count; remainingDayIndex++)
            {
                if (remainingDayIndex == dayIndex)
                {
                    continue;
                }

                var day = problem.Days[remainingDayIndex];
                availabilities.Add(new SpeakerAvailability(speakerId, remainingDayIndex, day.StartMinute, day.EndMinute));
                remainingAvailabilityCount++;
            }
        }

        if (remainingAvailabilityCount == 0)
        {
            throw new ArgumentException("Diễn giả không còn ngày rảnh; không thể biểu diễn bằng danh sách rỗng.", nameof(speakerId));
        }

        return new SchedulingProblem(problem.Sessions, problem.Rooms, problem.Days, availabilities, data.Interests, problem.SlotMinutes);
    }

    // ---------- KIỂM DỮ LIỆU ----------
    // Helper của RemoveRoom / MakeSpeakerUnavailable (cùng lớp): cần danh sách nguyện vọng để dựng lại
    // ma trận quan tâm; GeneratedData tự dựng không kèm nguyện vọng thì bài toán mới sẽ sai.
    private static void ValidateData(GeneratedData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Interests.Count == 0)
        {
            throw new ArgumentException("Dữ liệu sinh phải kèm danh sách nguyện vọng.", nameof(data));
        }
    }
}
