using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Tests.Support;

// Hàm dựng dữ liệu nhỏ dùng chung cho các test: đỡ lặp lại việc tạo phiên, phòng, ngày.
public static class TestData
{
    public static SchedulingSession Session(int id, int durationMinutes, params int[] speakerIds)
    {
        return new SchedulingSession(id, $"Session {id}", durationMinutes, speakerIds, null);
    }

    public static SchedulingSession SessionWithSize(int id, int durationMinutes, int expectedSize, params int[] speakerIds)
    {
        return new SchedulingSession(id, $"Session {id}", durationMinutes, speakerIds, expectedSize);
    }

    public static SchedulingRoom Room(int id, int capacity)
    {
        return new SchedulingRoom(id, $"Room {id}", capacity);
    }

    public static SchedulingDay Day(int startMinute, int endMinute, params TimeInterval[] breaks)
    {
        return new SchedulingDay(startMinute, endMinute, breaks);
    }

    public static SchedulingProblem CreateProblem(
        IReadOnlyList<SchedulingSession> sessions,
        IReadOnlyList<SchedulingRoom> rooms,
        IReadOnlyList<SchedulingDay> days,
        IReadOnlyList<SpeakerAvailability>? availabilities = null,
        IReadOnlyList<InterestEntry>? interests = null,
        int slotMinutes = 15)
    {
        return new SchedulingProblem(
            sessions,
            rooms,
            days,
            availabilities ?? [],
            interests ?? [],
            slotMinutes);
    }

    // Chỉ số của mốc bắt đầu lúc startMinute của ngày dayIndex.
    public static int SlotIndexAt(SchedulingProblem problem, int dayIndex, int startMinute)
    {
        foreach (var slot in problem.Timeline.Slots)
        {
            if (slot.DayIndex == dayIndex && slot.StartMinute == startMinute)
            {
                return slot.Index;
            }
        }

        throw new InvalidOperationException($"Không có mốc {startMinute} của ngày {dayIndex}.");
    }
}
