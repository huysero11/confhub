using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Tests.Support;

// Ví dụ chạy tay trong PROJECT.md mục 6.5: 4 phiên, 2 phòng, 2 mốc (9h và 10h), 40 người tham dự.
//   A (42 chỗ, diễn giả Long), D (14, Long), B (39, Hoa), C (21, Tùng); P101 chứa 100, P102 chứa 40.
//   Người 1-35 quan tâm A và C; người 36-40 quan tâm A và B  =>  W[A][C] = 35, W[A][B] = 5.
public static class ExampleProblem
{
    public const int SessionA = 1;
    public const int SessionD = 2;
    public const int SessionB = 3;
    public const int SessionC = 4;
    public const int RoomP101 = 1;
    public const int RoomP102 = 2;
    public const int NineOClock = 0;
    public const int TenOClock = 1;
    private const int SpeakerLong = 1;
    private const int SpeakerHoa = 2;
    private const int SpeakerTung = 3;

    public static SchedulingProblem Create()
    {
        List<SchedulingSession> sessions =
        [
            TestData.SessionWithSize(SessionA, 60, 42, SpeakerLong),
            TestData.SessionWithSize(SessionD, 60, 14, SpeakerLong),
            TestData.SessionWithSize(SessionB, 60, 39, SpeakerHoa),
            TestData.SessionWithSize(SessionC, 60, 21, SpeakerTung),
        ];

        List<SchedulingRoom> rooms = [TestData.Room(RoomP101, 100), TestData.Room(RoomP102, 40)];

        var interests = new List<InterestEntry>();
        for (var attendeeId = 1; attendeeId <= 35; attendeeId++)
        {
            interests.Add(new InterestEntry(attendeeId, SessionA));
            interests.Add(new InterestEntry(attendeeId, SessionC));
        }

        for (var attendeeId = 36; attendeeId <= 40; attendeeId++)
        {
            interests.Add(new InterestEntry(attendeeId, SessionA));
            interests.Add(new InterestEntry(attendeeId, SessionB));
        }

        List<SchedulingDay> days = [TestData.Day(9 * 60, 11 * 60)];

        return TestData.CreateProblem(sessions, rooms, days, null, interests, 60);
    }

    // Lịch tham lam của ví dụ: A 9h P101, D 10h P102, B 10h P101, C 9h P102  =>  C = 35.
    public static Schedule CreateGreedySchedule()
    {
        var schedule = new Schedule();
        schedule.Place(SessionA, NineOClock, RoomP101);
        schedule.Place(SessionD, TenOClock, RoomP102);
        schedule.Place(SessionB, TenOClock, RoomP101);
        schedule.Place(SessionC, NineOClock, RoomP102);
        return schedule;
    }

    // Sau khi hoán đổi B và C: A 9h P101, B 9h P102, C 10h P101, D 10h P102  =>  C = 5.
    public static Schedule CreateSwappedSchedule()
    {
        var schedule = new Schedule();
        schedule.Place(SessionA, NineOClock, RoomP101);
        schedule.Place(SessionB, NineOClock, RoomP102);
        schedule.Place(SessionC, TenOClock, RoomP101);
        schedule.Place(SessionD, TenOClock, RoomP102);
        return schedule;
    }
}
