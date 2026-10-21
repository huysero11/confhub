using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Problem;
using ConfHub.Scheduling.Solution;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// Mỗi ràng buộc được kiểm bằng cả hai đường: CheckPlacement (hỏi trước khi đặt) và Validate (kiểm cả lịch).
public class ConstraintCheckerTests
{
    private const int SpeakerShared = 1;

    // ---------- H1: CHUNG DIỄN GIẢ ----------
    [Fact]
    public void H1SameSpeakerAtSameTimeIsViolation()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock);

        var placementViolations = checker.CheckPlacement(schedule, ExampleProblem.SessionD, ExampleProblem.NineOClock, null);
        schedule.Place(ExampleProblem.SessionD, ExampleProblem.NineOClock);
        var scheduleViolations = checker.Validate(schedule);

        Assert.Contains(placementViolations, violation => violation.Code == ConstraintCode.H1);
        Assert.Contains(scheduleViolations, violation => violation.Code == ConstraintCode.H1);
    }

    [Fact]
    public void H1SameSpeakerBackToBackIsAllowed()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock);

        // D bắt đầu 10:00 đúng lúc A kết thúc: chạm đầu mút, không chồng giờ.
        var placementViolations = checker.CheckPlacement(schedule, ExampleProblem.SessionD, ExampleProblem.TenOClock, null);
        schedule.Place(ExampleProblem.SessionD, ExampleProblem.TenOClock);

        Assert.Empty(placementViolations);
        Assert.Empty(checker.Validate(schedule));
    }

    [Fact]
    public void H1PartiallyOverlappingSessionsWithSharedSpeakerAreViolation()
    {
        // Slot 30 phút: phiên 1 (60 phút) lúc 9:00, phiên 2 (90 phút) lúc 9:30 chồng nửa chừng.
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, SpeakerShared), TestData.Session(2, 90, SpeakerShared)],
            [TestData.Room(1, 50), TestData.Room(2, 50)],
            [TestData.Day(540, 720)],
            null,
            null,
            30);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(1, TestData.SlotIndexAt(problem, 0, 540));
        schedule.Place(2, TestData.SlotIndexAt(problem, 0, 570));

        var scheduleViolations = checker.Validate(schedule);

        Assert.Contains(scheduleViolations, violation => violation.Code == ConstraintCode.H1);
    }

    // ---------- H2: QUÁ SỐ PHÒNG ----------
    [Fact]
    public void H2MoreSessionsThanRoomsAtOneTimeIsViolation()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60), TestData.Session(3, 60)],
            [TestData.Room(1, 50), TestData.Room(2, 50)],
            [TestData.Day(540, 660)],
            null,
            null,
            60);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(1, 0);
        schedule.Place(2, 0);

        var placementViolations = checker.CheckPlacement(schedule, 3, 0, null);
        schedule.Place(3, 0);

        Assert.Contains(placementViolations, violation => violation.Code == ConstraintCode.H2);
        var scheduleViolation = Assert.Single(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H2);
        Assert.Equal(3, scheduleViolation.SessionIds.Count);
    }

    [Fact]
    public void H2PartiallyOverlappingLongSessionStillCounts()
    {
        // Slot 30 phút, 2 phòng: phiên 1 [9:00, 10:00), phiên 2 [9:30, 11:00), phiên 3 [9:30, 10:30) => 3 phiên cùng lúc.
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 90), TestData.Session(3, 60)],
            [TestData.Room(1, 50), TestData.Room(2, 50)],
            [TestData.Day(540, 720)],
            null,
            null,
            30);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(1, TestData.SlotIndexAt(problem, 0, 540));
        schedule.Place(2, TestData.SlotIndexAt(problem, 0, 570));

        var violations = checker.CheckPlacement(schedule, 3, TestData.SlotIndexAt(problem, 0, 570), null);

        Assert.Contains(violations, violation => violation.Code == ConstraintCode.H2);
    }

    [Fact]
    public void H2SessionsTouchingAtEndpointDoNotCount()
    {
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60), TestData.Session(2, 60), TestData.Session(3, 60)],
            [TestData.Room(1, 50), TestData.Room(2, 50)],
            [TestData.Day(540, 720)],
            null,
            null,
            60);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(1, 0);
        schedule.Place(2, 0);

        // Phiên 3 bắt đầu 10:00 khi hai phiên kia vừa kết thúc.
        var violations = checker.CheckPlacement(schedule, 3, 1, null);

        Assert.Empty(violations);
    }

    // ---------- H3: CÙNG PHÒNG ----------
    [Fact]
    public void H3SameRoomAtSameTimeIsViolation()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);

        var placementViolations = checker.CheckPlacement(
            schedule,
            ExampleProblem.SessionC,
            ExampleProblem.NineOClock,
            ExampleProblem.RoomP101);
        schedule.Place(ExampleProblem.SessionC, ExampleProblem.NineOClock, ExampleProblem.RoomP101);

        Assert.Contains(placementViolations, violation => violation.Code == ConstraintCode.H3);
        Assert.Contains(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H3);
    }

    [Fact]
    public void H3SameRoomAtDifferentTimesIsAllowed()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);

        var placementViolations = checker.CheckPlacement(
            schedule,
            ExampleProblem.SessionC,
            ExampleProblem.TenOClock,
            ExampleProblem.RoomP101);
        schedule.Place(ExampleProblem.SessionC, ExampleProblem.TenOClock, ExampleProblem.RoomP101);

        Assert.Empty(placementViolations);
        Assert.Empty(checker.Validate(schedule));
    }

    // ---------- H4: SỨC CHỨA ----------
    [Fact]
    public void H4RoomSmallerThanSessionSizeIsViolation()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();

        // A có 42 chỗ, P102 chỉ chứa 40.
        var placementViolations = checker.CheckPlacement(
            schedule,
            ExampleProblem.SessionA,
            ExampleProblem.NineOClock,
            ExampleProblem.RoomP102);
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP102);

        Assert.Contains(placementViolations, violation => violation.Code == ConstraintCode.H4);
        Assert.Contains(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H4);
    }

    [Fact]
    public void H4RoomExactlyFittingIsAllowed()
    {
        // Phiên D (14 chỗ) vào P102 (40 chỗ) thì vừa.
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);

        var violations = checker.CheckPlacement(new Schedule(), ExampleProblem.SessionD, ExampleProblem.NineOClock, ExampleProblem.RoomP102);

        Assert.Empty(violations);
    }

    // ---------- H5: VỊ TRÍ KHÔNG HỢP LỆ ----------
    [Fact]
    public void H5SessionCrossingLunchBreakIsViolation()
    {
        List<SchedulingDay> days = [TestData.Day(8 * 60, 17 * 60, new TimeInterval(690, 780))];
        var problem = TestData.CreateProblem([TestData.Session(1, 90)], [TestData.Room(1, 50)], days);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();

        var violations = checker.CheckPlacement(schedule, 1, TestData.SlotIndexAt(problem, 0, 660), null);
        schedule.Place(1, TestData.SlotIndexAt(problem, 0, 660));

        Assert.Contains(violations, violation => violation.Code == ConstraintCode.H5);
        Assert.Contains(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H5);
    }

    [Fact]
    public void H5SessionRunningPastEndOfDayIsViolation()
    {
        List<SchedulingDay> days = [TestData.Day(8 * 60, 17 * 60, new TimeInterval(690, 780))];
        var problem = TestData.CreateProblem([TestData.Session(1, 90)], [TestData.Room(1, 50)], days);
        var checker = new ConstraintChecker(problem);

        var violations = checker.CheckPlacement(new Schedule(), 1, TestData.SlotIndexAt(problem, 0, 1005), null);

        Assert.Contains(violations, violation => violation.Code == ConstraintCode.H5);
    }

    [Fact]
    public void H5SpeakerNotAvailableIsViolation()
    {
        List<SpeakerAvailability> availabilities = [new SpeakerAvailability(7, 0, 540, 600)];
        var problem = TestData.CreateProblem(
            [TestData.Session(1, 60, 7)],
            [TestData.Room(1, 50)],
            [TestData.Day(540, 720)],
            availabilities,
            null,
            60);
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();

        var violationsAtNine = checker.CheckPlacement(schedule, 1, 0, null);
        var violationsAtTen = checker.CheckPlacement(schedule, 1, 1, null);
        schedule.Place(1, 1);

        Assert.Empty(violationsAtNine);
        Assert.Contains(violationsAtTen, violation => violation.Code == ConstraintCode.H5);
        Assert.Contains(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H5);
    }

    [Fact]
    public void H5SlotIndexOutsideTimelineIsViolation()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, 99);

        var placementViolations = checker.CheckPlacement(new Schedule(), ExampleProblem.SessionA, 99, null);

        Assert.Contains(placementViolations, violation => violation.Code == ConstraintCode.H5);
        Assert.Contains(checker.Validate(schedule), violation => violation.Code == ConstraintCode.H5);
    }

    // ---------- PHIÊN CHƯA GÁN PHÒNG, DỜI PHIÊN ----------
    [Fact]
    public void SessionWithoutRoomSkipsH3AndH4ButKeepsOthers()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock);
        schedule.Place(ExampleProblem.SessionD, ExampleProblem.NineOClock);

        var violations = checker.Validate(schedule);

        // Chưa có phòng nên không có H3 / H4, nhưng A và D chung diễn giả vẫn là H1.
        Assert.DoesNotContain(violations, violation => violation.Code == ConstraintCode.H3);
        Assert.DoesNotContain(violations, violation => violation.Code == ConstraintCode.H4);
        Assert.Contains(violations, violation => violation.Code == ConstraintCode.H1);
    }

    [Fact]
    public void CheckPlacementIgnoresThePlacementOfTheSessionItself()
    {
        var problem = ExampleProblem.Create();
        var checker = new ConstraintChecker(problem);
        var schedule = new Schedule();
        schedule.Place(ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);

        // Dời A sang đúng chỗ cũ hoặc sang mốc khác đều không tự va chạm với chính nó.
        var sameSpot = checker.CheckPlacement(schedule, ExampleProblem.SessionA, ExampleProblem.NineOClock, ExampleProblem.RoomP101);
        var movedSpot = checker.CheckPlacement(schedule, ExampleProblem.SessionA, ExampleProblem.TenOClock, ExampleProblem.RoomP101);

        Assert.Empty(sameSpot);
        Assert.Empty(movedSpot);
    }

    [Fact]
    public void ValidScheduleOfTheExampleHasNoViolation()
    {
        var checker = new ConstraintChecker(ExampleProblem.Create());

        Assert.Empty(checker.Validate(ExampleProblem.CreateGreedySchedule()));
        Assert.Empty(checker.Validate(ExampleProblem.CreateSwappedSchedule()));
    }
}
