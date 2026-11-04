using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;

namespace ConfHub.Scheduling.Tests.Support;

// Ca gán phòng chồng giờ một phần (T2.4 mục 3.4): mốc 30 phút, ngày 9h–11h, phòng P35 và P100;
// X (30 người) 9h00, Y (33) 9h30, Z (90) 10h00, mỗi phiên 60 phút. Lịch: X → P100, Y → P35, Z → P100.
public static class XyzCase
{
    public const int SessionX = 1;
    public const int SessionY = 2;
    public const int SessionZ = 3;
    public const int Room35 = 35;
    public const int Room100 = 100;

    public static SchedulingProblem CreateProblem()
    {
        return TestData.CreateProblem(
            [TestData.SessionWithSize(SessionX, 60, 30), TestData.SessionWithSize(SessionY, 60, 33), TestData.SessionWithSize(SessionZ, 60, 90)],
            [TestData.Room(Room35, 35), TestData.Room(Room100, 100)],
            [TestData.Day(540, 660)],
            slotMinutes: 30);
    }

    // S = 70 + 2 + 10 = 82.
    public static Schedule CreateSchedule()
    {
        var schedule = new Schedule();
        schedule.Place(SessionX, 0, Room100);
        schedule.Place(SessionY, 1, Room35);
        schedule.Place(SessionZ, 2, Room100);
        return schedule;
    }
}
