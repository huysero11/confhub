using ConfHub.Scheduling.Generation;
using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Tests;

public class ProblemGeneratorTests
{
    // ---------- LẶP LẠI ĐƯỢC (SEED) ----------
    [Fact]
    public void SameSeedGivesIdenticalData()
    {
        var first = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = 42 });
        var second = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = 42 });

        AssertSameProblem(first.Problem, second.Problem);
        Assert.Equal(first.TopicClusterBySession.OrderBy(pair => pair.Key), second.TopicClusterBySession.OrderBy(pair => pair.Key));
    }

    [Fact]
    public void DifferentSeedGivesDifferentData()
    {
        var first = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = 1 });
        var second = ProblemGenerator.Generate(GeneratorOptions.Small with { Seed = 2 });

        Assert.NotEqual(first.Problem.InterestMatrix.TotalCoInterest, second.Problem.InterestMatrix.TotalCoInterest);
    }

    // ---------- BA QUY MÔ DỰNG SẴN ----------
    [Fact]
    public void SmallPresetHasExpectedSize()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Small).Problem;

        Assert.Equal(30, problem.Sessions.Count);
        Assert.Equal(3, problem.Rooms.Count);
        Assert.Equal(2, problem.Days.Count);
    }

    [Fact]
    public void MediumPresetHasExpectedSize()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Medium).Problem;

        Assert.Equal(60, problem.Sessions.Count);
        Assert.Equal(4, problem.Rooms.Count);
        Assert.Equal(3, problem.Days.Count);
    }

    [Fact]
    public void LargePresetHasExpectedSize()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Large).Problem;

        Assert.Equal(100, problem.Sessions.Count);
        Assert.Equal(7, problem.Rooms.Count);
        Assert.Equal(3, problem.Days.Count);
    }

    // ---------- TÍNH CHẤT CỦA DỮ LIỆU ----------
    [Fact]
    public void EverySessionFitsInAtLeastOneRoomAndHasAValidStartSlot()
    {
        foreach (var options in new[] { GeneratorOptions.Small, GeneratorOptions.Medium, GeneratorOptions.Large })
        {
            var problem = ProblemGenerator.Generate(options).Problem;
            var largestCapacity = problem.Rooms.Max(room => room.Capacity);

            foreach (var session in problem.Sessions)
            {
                Assert.True(problem.GetSessionSize(session.Id) <= largestCapacity);
                Assert.NotEmpty(problem.GetValidStartSlots(session.Id));
            }
        }
    }

    [Fact]
    public void SomeSessionsHaveTwoSpeakersAndSomeSpeakersHaveManySessions()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Medium).Problem;

        Assert.Contains(problem.Sessions, session => session.SpeakerIds.Count == 2);

        var sessionCountBySpeaker = new Dictionary<int, int>();
        foreach (var session in problem.Sessions)
        {
            foreach (var speakerId in session.SpeakerIds)
            {
                sessionCountBySpeaker.TryGetValue(speakerId, out var count);
                sessionCountBySpeaker[speakerId] = count + 1;
            }
        }

        Assert.Contains(sessionCountBySpeaker.Values, count => count >= 2);
    }

    [Fact]
    public void SomeSpeakersDeclareAvailability()
    {
        var problem = ProblemGenerator.Generate(GeneratorOptions.Medium).Problem;

        Assert.NotEmpty(problem.SpeakerAvailabilities);
    }

    [Fact]
    public void SessionsOfTheSameTopicClusterShareMoreAttendees()
    {
        var data = ProblemGenerator.Generate(GeneratorOptions.Medium);
        var sessions = data.Problem.Sessions;

        var sameClusterTotal = 0L;
        var sameClusterPairs = 0;
        var differentClusterTotal = 0L;
        var differentClusterPairs = 0;

        for (var first = 0; first < sessions.Count; first++)
        {
            for (var second = first + 1; second < sessions.Count; second++)
            {
                var coInterest = data.Problem.InterestMatrix.Get(sessions[first].Id, sessions[second].Id);

                if (data.TopicClusterBySession[sessions[first].Id] == data.TopicClusterBySession[sessions[second].Id])
                {
                    sameClusterTotal += coInterest;
                    sameClusterPairs++;
                }
                else
                {
                    differentClusterTotal += coInterest;
                    differentClusterPairs++;
                }
            }
        }

        var sameClusterAverage = sameClusterTotal / (double)sameClusterPairs;
        var differentClusterAverage = differentClusterTotal / (double)differentClusterPairs;

        Assert.True(sameClusterAverage >= 2 * differentClusterAverage);
    }

    // ---------- TÙY CHỌN SAI ----------
    [Fact]
    public void TooManySessionsForTheRoomsIsRejected()
    {
        var options = GeneratorOptions.Large with { RoomCount = 1, DayCount = 1 };

        Assert.Throws<ArgumentException>(() => ProblemGenerator.Generate(options));
    }

    [Fact]
    public void InvalidProbabilityIsRejected()
    {
        var options = GeneratorOptions.Small with { HomeClusterProbability = 1.5 };

        Assert.Throws<ArgumentException>(() => ProblemGenerator.Generate(options));
    }

    [Fact]
    public void MinInterestsGreaterThanMaxIsRejected()
    {
        var options = GeneratorOptions.Small with { MinInterestsPerAttendee = 9, MaxInterestsPerAttendee = 8 };

        Assert.Throws<ArgumentException>(() => ProblemGenerator.Generate(options));
    }

    // ---------- HÀM HỖ TRỢ ----------
    private static void AssertSameProblem(SchedulingProblem first, SchedulingProblem second)
    {
        Assert.Equal(first.Sessions.Count, second.Sessions.Count);
        for (var index = 0; index < first.Sessions.Count; index++)
        {
            Assert.Equal(first.Sessions[index].Id, second.Sessions[index].Id);
            Assert.Equal(first.Sessions[index].DurationMinutes, second.Sessions[index].DurationMinutes);
            Assert.Equal(first.Sessions[index].SpeakerIds.ToArray(), second.Sessions[index].SpeakerIds.ToArray());
        }

        Assert.Equal(first.Rooms, second.Rooms);
        Assert.Equal(first.SpeakerAvailabilities, second.SpeakerAvailabilities);
        Assert.Equal(first.InterestMatrix.TotalCoInterest, second.InterestMatrix.TotalCoInterest);

        foreach (var firstSession in first.Sessions)
        {
            foreach (var secondSession in first.Sessions)
            {
                Assert.Equal(
                    first.InterestMatrix.Get(firstSession.Id, secondSession.Id),
                    second.InterestMatrix.Get(firstSession.Id, secondSession.Id));
            }
        }
    }
}
