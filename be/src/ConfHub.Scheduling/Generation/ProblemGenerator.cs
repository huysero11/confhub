using ConfHub.Scheduling.Problem;

namespace ConfHub.Scheduling.Generation;

// Sinh dữ liệu mô phỏng cho thực nghiệm. Dùng Random có seed nên chạy lại cho cùng kết quả.
// Thứ tự sinh: ngày -> phiên (thời lượng, cụm chủ đề) -> nguyện vọng -> phòng -> diễn giả -> giờ rảnh.
public static class ProblemGenerator
{
    private const int SlotMinutes = 15;
    private const int DayStartMinute = 8 * 60;
    private const int DayEndMinute = 17 * 60;
    private const int LunchStartMinute = (11 * 60) + 30;
    private const int LunchEndMinute = 13 * 60;
    private const int MinAvailabilityMinutes = 3 * 60;
    private const double MaxLoadRatio = 0.9;
    private const double SpeakerPoolRatio = 0.7;

    // Sinh một bài toán mô phỏng theo tùy chọn.
    // Dùng ở: T2.4 (chương trình thực nghiệm), test (dữ liệu Small / Medium / Large).
    public static GeneratedData Generate(GeneratorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ValidateOptions(options);

        var random = new Random(options.Seed);

        var days = BuildDays(options.DayCount);
        var durations = BuildDurations(options, random);
        CheckLoad(options, days, durations);

        var clusters = BuildTopicClusters(options, random);
        var interests = BuildInterests(options, clusters, random);
        var rooms = BuildRooms(options, interests);
        var speakersBySession = BuildSpeakers(options, random);
        var sessions = BuildSessions(durations, speakersBySession);
        var availabilities = BuildAvailabilities(options, sessions, random);

        var problem = BuildProblemWithPlaceableSessions(sessions, rooms, days, availabilities, interests);

        var clusterBySession = new Dictionary<int, int>();
        for (var index = 0; index < clusters.Length; index++)
        {
            clusterBySession[index + 1] = clusters[index];
        }

        return new GeneratedData(problem, clusterBySession);
    }

    // ---------- KIỂM TRA TÙY CHỌN ----------
    // Helper của Generate (cùng lớp): tùy chọn sai thì báo lỗi.
    private static void ValidateOptions(GeneratorOptions options)
    {
        if (options.SessionCount <= 0 || options.AttendeeCount <= 0 || options.DayCount <= 0
            || options.RoomCount <= 0 || options.TopicClusterCount <= 0)
        {
            throw new ArgumentException("Số phiên, người tham dự, ngày, phòng và cụm chủ đề phải lớn hơn 0.");
        }

        if (options.MinInterestsPerAttendee < 1 || options.MinInterestsPerAttendee > options.MaxInterestsPerAttendee)
        {
            throw new ArgumentException("Số nguyện vọng mỗi người phải thỏa 1 <= min <= max.");
        }

        if (!IsProbability(options.HomeClusterProbability)
            || !IsProbability(options.LongSessionProbability)
            || !IsProbability(options.SecondSpeakerProbability)
            || !IsProbability(options.ConstrainedSpeakerFraction))
        {
            throw new ArgumentException("Các xác suất và tỉ lệ phải nằm trong [0, 1].");
        }
    }

    // Helper của ValidateOptions (cùng lớp).
    private static bool IsProbability(double value)
    {
        return value >= 0.0 && value <= 1.0;
    }

    // ---------- NGÀY ----------
    // Mỗi ngày 08:00-17:00, nghỉ trưa 11:30-13:00 (450 phút dùng được mỗi phòng mỗi ngày).
    // Helper của Generate (cùng lớp).
    private static List<SchedulingDay> BuildDays(int dayCount)
    {
        var days = new List<SchedulingDay>();

        for (var index = 0; index < dayCount; index++)
        {
            var lunch = new TimeInterval(LunchStartMinute, LunchEndMinute);
            days.Add(new SchedulingDay(DayStartMinute, DayEndMinute, [lunch]));
        }

        return days;
    }

    // ---------- PHIÊN ----------
    // Phiên dài 90 phút với xác suất LongSessionProbability, ngược lại 60 phút.
    // Helper của Generate (cùng lớp).
    private static List<int> BuildDurations(GeneratorOptions options, Random random)
    {
        var durations = new List<int>();

        for (var index = 0; index < options.SessionCount; index++)
        {
            if (random.NextDouble() < options.LongSessionProbability)
            {
                durations.Add(90);
            }
            else
            {
                durations.Add(60);
            }
        }

        return durations;
    }

    // Tổng thời lượng phiên không được vượt 90% tổng số phút dùng được của mọi phòng.
    // Helper của Generate (cùng lớp).
    private static void CheckLoad(GeneratorOptions options, List<SchedulingDay> days, List<int> durations)
    {
        var timeline = new Timeline(days, SlotMinutes);
        var usableMinutes = timeline.Slots.Count * SlotMinutes * options.RoomCount;

        var totalDuration = 0;
        foreach (var duration in durations)
        {
            totalDuration += duration;
        }

        if (totalDuration > MaxLoadRatio * usableMinutes)
        {
            throw new ArgumentException("Quá nhiều phiên cho số phòng / ngày.");
        }
    }

    // Cụm chủ đề của từng phiên: chia đều cho các cụm rồi xáo trộn ngẫu nhiên (cụm nào cũng có phiên).
    // Helper của Generate (cùng lớp).
    private static int[] BuildTopicClusters(GeneratorOptions options, Random random)
    {
        var clusters = new int[options.SessionCount];

        for (var index = 0; index < clusters.Length; index++)
        {
            clusters[index] = index % options.TopicClusterCount;
        }

        random.Shuffle(clusters);
        return clusters;
    }

    // ---------- NGUYỆN VỌNG ----------
    // Mỗi người có một cụm chính; mỗi lần chọn phiên, xác suất HomeClusterProbability lấy phiên của cụm chính.
    // Nhờ vậy hai phiên cùng cụm có W lớn hơn hẳn hai phiên khác cụm.
    // Helper của Generate (cùng lớp).
    private static List<InterestEntry> BuildInterests(GeneratorOptions options, int[] clusters, Random random)
    {
        var sessionIdsByCluster = new List<int>[options.TopicClusterCount];
        for (var cluster = 0; cluster < sessionIdsByCluster.Length; cluster++)
        {
            sessionIdsByCluster[cluster] = [];
        }

        for (var index = 0; index < clusters.Length; index++)
        {
            sessionIdsByCluster[clusters[index]].Add(index + 1);
        }

        var interests = new List<InterestEntry>();

        for (var attendeeId = 1; attendeeId <= options.AttendeeCount; attendeeId++)
        {
            var homeCluster = random.Next(options.TopicClusterCount);
            var wantedCount = random.Next(options.MinInterestsPerAttendee, options.MaxInterestsPerAttendee + 1);
            wantedCount = Math.Min(wantedCount, options.SessionCount);

            var chosenSessionIds = ChooseSessionIds(options, sessionIdsByCluster[homeCluster], wantedCount, random);
            foreach (var sessionId in chosenSessionIds)
            {
                interests.Add(new InterestEntry(attendeeId, sessionId));
            }
        }

        return interests;
    }

    // Helper của BuildInterests (cùng lớp): chọn các phiên khác nhau cho một người.
    private static List<int> ChooseSessionIds(
        GeneratorOptions options,
        List<int> homeClusterSessionIds,
        int wantedCount,
        Random random)
    {
        var chosenSessionIds = new List<int>();

        // Giới hạn số lần thử để không lặp mãi khi cụm chính có ít phiên hơn số cần chọn.
        var attempts = 0;
        while (chosenSessionIds.Count < wantedCount && attempts < wantedCount * 50)
        {
            attempts++;

            int sessionId;
            if (homeClusterSessionIds.Count > 0 && random.NextDouble() < options.HomeClusterProbability)
            {
                sessionId = homeClusterSessionIds[random.Next(homeClusterSessionIds.Count)];
            }
            else
            {
                sessionId = random.Next(options.SessionCount) + 1;
            }

            if (!chosenSessionIds.Contains(sessionId))
            {
                chosenSessionIds.Add(sessionId);
            }
        }

        return chosenSessionIds;
    }

    // ---------- PHÒNG ----------
    // Chia bậc sức chứa theo phân vị quy mô phiên để tránh dữ liệu thiếu phòng đủ lớn.
    // Mỗi bậc ứng với khoảng SessionCount / RoomCount phiên; phòng đầu vừa phiên đông nhất.
    // Mỗi phòng dư 10%, làm tròn lên bội của 10 và tối thiểu 10 chỗ.
    // Helper của Generate (cùng lớp).
    private static List<SchedulingRoom> BuildRooms(GeneratorOptions options, List<InterestEntry> interests)
    {
        var interestCountBySession = new int[options.SessionCount + 1];
        foreach (var interest in interests)
        {
            interestCountBySession[interest.SessionId]++;
        }

        var sizes = new int[options.SessionCount];
        for (var sessionId = 1; sessionId <= options.SessionCount; sessionId++)
        {
            sizes[sessionId - 1] = interestCountBySession[sessionId];
        }

        Array.Sort(sizes);
        Array.Reverse(sizes);

        var rooms = new List<SchedulingRoom>();
        for (var index = 0; index < options.RoomCount; index++)
        {
            var capacity = Math.Max(10, RoundUpToTen(sizes[index * options.SessionCount / options.RoomCount] * 1.1));

            rooms.Add(new SchedulingRoom(index + 1, $"Phòng {index + 1}", capacity));
        }

        return rooms;
    }

    // Helper của BuildRooms (cùng lớp): làm tròn lên bội của 10.
    private static int RoundUpToTen(double value)
    {
        return (int)Math.Ceiling(value / 10.0) * 10;
    }

    // ---------- DIỄN GIẢ ----------
    // Mỗi phiên một diễn giả ngẫu nhiên, thêm diễn giả thứ hai (khác người) với xác suất SecondSpeakerProbability.
    // Số diễn giả ít hơn số phiên nên một diễn giả nhận nhiều phiên.
    // Helper của Generate (cùng lớp).
    private static List<List<int>> BuildSpeakers(GeneratorOptions options, Random random)
    {
        var speakerCount = Math.Max(1, (int)(options.SessionCount * SpeakerPoolRatio));
        var speakersBySession = new List<List<int>>();

        for (var index = 0; index < options.SessionCount; index++)
        {
            var firstSpeakerId = random.Next(speakerCount) + 1;
            var speakerIds = new List<int> { firstSpeakerId };

            if (speakerCount > 1 && random.NextDouble() < options.SecondSpeakerProbability)
            {
                var secondSpeakerId = random.Next(speakerCount - 1) + 1;
                if (secondSpeakerId >= firstSpeakerId)
                {
                    secondSpeakerId++;
                }

                speakerIds.Add(secondSpeakerId);
            }

            speakersBySession.Add(speakerIds);
        }

        return speakersBySession;
    }

    // Helper của Generate (cùng lớp): ghép thời lượng và diễn giả thành danh sách phiên.
    private static List<SchedulingSession> BuildSessions(List<int> durations, List<List<int>> speakersBySession)
    {
        var sessions = new List<SchedulingSession>();

        for (var index = 0; index < durations.Count; index++)
        {
            var sessionId = index + 1;
            sessions.Add(new SchedulingSession(sessionId, $"Phiên {sessionId}", durations[index], speakersBySession[index], null));
        }

        return sessions;
    }

    // ---------- GIỜ RẢNH ----------
    // Một phần diễn giả (có phiên) khai giờ rảnh; những người còn lại không khai gì, tức là rảnh mọi lúc.
    // Helper của Generate (cùng lớp).
    private static List<SpeakerAvailability> BuildAvailabilities(
        GeneratorOptions options,
        List<SchedulingSession> sessions,
        Random random)
    {
        var minutesBySpeaker = new SortedDictionary<int, int>();
        foreach (var session in sessions)
        {
            foreach (var speakerId in session.SpeakerIds)
            {
                minutesBySpeaker.TryGetValue(speakerId, out var minutes);
                minutesBySpeaker[speakerId] = minutes + session.DurationMinutes;
            }
        }

        // Xếp theo mã rồi xáo trộn bằng Random có seed để kết quả lặp lại được.
        var shuffledSpeakerIds = new int[minutesBySpeaker.Count];
        minutesBySpeaker.Keys.CopyTo(shuffledSpeakerIds, 0);
        random.Shuffle(shuffledSpeakerIds);

        var constrainedCount = (int)Math.Round(options.ConstrainedSpeakerFraction * shuffledSpeakerIds.Length);
        var availabilities = new List<SpeakerAvailability>();

        for (var index = 0; index < constrainedCount; index++)
        {
            var speakerId = shuffledSpeakerIds[index];
            availabilities.AddRange(BuildSpeakerAvailabilities(options, speakerId, minutesBySpeaker[speakerId], random));
        }

        return availabilities;
    }

    // 1-2 khoảng rảnh, mỗi khoảng >= 3 giờ, tổng >= 2 lần tổng thời lượng phiên của diễn giả.
    // Không thể thỏa điều kiện đó thì trả về rỗng (diễn giả không khai).
    // Helper của BuildAvailabilities (cùng lớp).
    private static List<SpeakerAvailability> BuildSpeakerAvailabilities(
        GeneratorOptions options,
        int speakerId,
        int totalSessionMinutes,
        Random random)
    {
        var availabilities = new List<SpeakerAvailability>();
        var windowMinutes = DayEndMinute - DayStartMinute;
        var neededMinutes = 2 * totalSessionMinutes;

        var intervalCount = 1;
        if (options.DayCount >= 2 && random.Next(2) == 1)
        {
            intervalCount = 2;
        }

        if (neededMinutes > intervalCount * windowMinutes)
        {
            intervalCount = 2;
        }

        if (intervalCount > options.DayCount || neededMinutes > intervalCount * windowMinutes)
        {
            return availabilities;
        }

        var perIntervalMinutes = (int)Math.Ceiling(neededMinutes / (double)intervalCount / SlotMinutes) * SlotMinutes;
        perIntervalMinutes = Math.Max(MinAvailabilityMinutes, perIntervalMinutes);

        // Hai khoảng nằm ở hai ngày khác nhau để không chồng lên nhau.
        var firstDayIndex = random.Next(options.DayCount);
        for (var interval = 0; interval < intervalCount; interval++)
        {
            var dayIndex = (firstDayIndex + interval) % options.DayCount;
            var startChoices = ((windowMinutes - perIntervalMinutes) / SlotMinutes) + 1;
            var startMinute = DayStartMinute + (random.Next(startChoices) * SlotMinutes);

            availabilities.Add(new SpeakerAvailability(speakerId, dayIndex, startMinute, startMinute + perIntervalMinutes));
        }

        return availabilities;
    }

    // ---------- DỰNG BÀI TOÁN ----------
    // Giờ rảnh được giữ chỉ khi mọi phiên của diễn giả vẫn đặt được ít nhất ở một mốc
    // (khoảng rảnh vắt qua giờ nghỉ trưa có thể làm một phiên không còn chỗ đặt).
    // Diễn giả nào làm phiên mất chỗ thì bỏ khai giờ rảnh, tức là rảnh mọi lúc.
    // Helper của Generate (cùng lớp).
    private static SchedulingProblem BuildProblemWithPlaceableSessions(
        List<SchedulingSession> sessions,
        List<SchedulingRoom> rooms,
        List<SchedulingDay> days,
        List<SpeakerAvailability> availabilities,
        List<InterestEntry> interests)
    {
        var problem = new SchedulingProblem(sessions, rooms, days, availabilities, interests, SlotMinutes);

        var speakersToRelease = new HashSet<int>();
        foreach (var session in sessions)
        {
            if (problem.GetValidStartSlots(session.Id).Count == 0)
            {
                foreach (var speakerId in session.SpeakerIds)
                {
                    speakersToRelease.Add(speakerId);
                }
            }
        }

        if (speakersToRelease.Count == 0)
        {
            return problem;
        }

        var keptAvailabilities = new List<SpeakerAvailability>();
        foreach (var availability in availabilities)
        {
            if (!speakersToRelease.Contains(availability.SpeakerId))
            {
                keptAvailabilities.Add(availability);
            }
        }

        return new SchedulingProblem(sessions, rooms, days, keptAvailabilities, interests, SlotMinutes);
    }
}
