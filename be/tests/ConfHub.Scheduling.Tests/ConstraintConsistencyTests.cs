using ConfHub.Scheduling.Constraints;
using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Tests.Support;

namespace ConfHub.Scheduling.Tests;

// CheckPlacement (hỏi trước khi đặt) và Validate (kiểm cả lịch) phải nhất quán với nhau:
// lịch dựng chỉ từ các vị trí mà CheckPlacement chấp nhận thì Validate phải báo 0 vi phạm.
public class ConstraintConsistencyTests
{
    [Fact]
    public void ScheduleBuiltFromAcceptedPlacementsHasNoViolation()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = seed }).Problem;
            var schedule = RandomScheduleBuilder.Build(problem, seed);

            var violations = new ConstraintChecker(problem).Validate(schedule);

            Assert.Empty(violations);
        }
    }

    [Fact]
    public void MoreThanHalfOfTheSessionsGetPlaced()
    {
        for (var seed = 1; seed <= 20; seed++)
        {
            var problem = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = seed }).Problem;
            var schedule = RandomScheduleBuilder.Build(problem, seed);

            Assert.True(schedule.PlacedSessionIds.Count * 2 > problem.Sessions.Count);
        }
    }
}
