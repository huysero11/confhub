namespace ConfHub.Scheduling.Problem;

// Bài toán xếp lịch đã được kiểm tra đầu vào. Bất biến sau khi dựng.
// Tầng thuật toán chỉ đọc dữ liệu từ đây (không tự tính lại mốc, ma trận W, giờ rảnh).
public sealed class SchedulingProblem
{
    private readonly Dictionary<int, SchedulingSession> _sessionsById = [];
    private readonly Dictionary<int, SchedulingRoom> _roomsById = [];

    // Giờ rảnh đã gộp: mã diễn giả -> ngày -> các khoảng rời nhau, xếp theo giờ bắt đầu.
    private readonly Dictionary<int, Dictionary<int, List<TimeInterval>>> _availabilityBySpeaker = [];
    private readonly Dictionary<int, List<int>> _validStartSlotsBySession = [];
    private readonly Dictionary<int, HashSet<int>> _validStartSetsBySession = [];

    // Dùng ở: tầng gọi thuật toán (T5.1 dựng từ dữ liệu CSDL), ProblemGenerator.BuildProblemWithPlaceableSessions, test.
    public SchedulingProblem(
        IReadOnlyList<SchedulingSession> sessions,
        IReadOnlyList<SchedulingRoom> rooms,
        IReadOnlyList<SchedulingDay> days,
        IReadOnlyList<SpeakerAvailability> speakerAvailabilities,
        IReadOnlyList<InterestEntry> interests,
        int slotMinutes = 15)
    {
        ArgumentNullException.ThrowIfNull(sessions);
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(speakerAvailabilities);
        ArgumentNullException.ThrowIfNull(interests);

        // ---------- KIỂM TRA ĐẦU VÀO ----------
        if (slotMinutes <= 0)
        {
            throw new ArgumentException("Độ rộng mốc thời gian phải lớn hơn 0.", nameof(slotMinutes));
        }

        ValidateDays(days);
        ValidateRooms(rooms);
        ValidateSessions(sessions, slotMinutes);
        ValidateSpeakerAvailabilities(speakerAvailabilities, days.Count);
        ValidateInterests(interests, sessions);

        // ---------- DỰNG DỮ LIỆU TRA CỨU ----------
        Sessions = new List<SchedulingSession>(sessions);
        Rooms = new List<SchedulingRoom>(rooms);
        Days = new List<SchedulingDay>(days);
        SpeakerAvailabilities = new List<SpeakerAvailability>(speakerAvailabilities);
        SlotMinutes = slotMinutes;

        foreach (var session in Sessions)
        {
            _sessionsById[session.Id] = session;
        }

        foreach (var room in Rooms)
        {
            _roomsById[room.Id] = room;
        }

        // Sinh các mốc 8:00, 8:30, 9:00... và bỏ những mốc trùng giờ nghỉ.
        Timeline = new Timeline(Days, SlotMinutes);

        // Đếm số người cùng quan tâm từng cặp phiên.
        InterestMatrix = new InterestMatrix(Sessions, interests);

        BuildAvailabilityBySpeaker();
        BuildValidStartSlots();
    }

    public IReadOnlyList<SchedulingSession> Sessions { get; }

    public IReadOnlyList<SchedulingRoom> Rooms { get; }

    public IReadOnlyList<SchedulingDay> Days { get; }

    public IReadOnlyList<SpeakerAvailability> SpeakerAvailabilities { get; }

    public int SlotMinutes { get; }

    public Timeline Timeline { get; }

    public InterestMatrix InterestMatrix { get; }

    // ---------- TRA CỨU ----------
    // Tra phiên theo mã, mã lạ thì báo lỗi.
    // Dùng ở: ConstraintChecker.AddSpeakerViolation (H1); trong lớp: GetSessionSize, GetSessionInterval.
    public SchedulingSession GetSession(int sessionId)
    {
        if (!_sessionsById.TryGetValue(sessionId, out var session))
        {
            throw new ArgumentException($"Không có phiên mã {sessionId}.");
        }

        return session;
    }

    // Tra phòng theo mã, mã lạ thì báo lỗi.
    // Dùng ở: ConstraintChecker.AddCapacityViolation (H4), ObjectiveCalculator.CalculateWastedSeats (S).
    public SchedulingRoom GetRoom(int roomId)
    {
        if (!_roomsById.TryGetValue(roomId, out var room))
        {
            throw new ArgumentException($"Không có phòng mã {roomId}.");
        }

        return room;
    }

    // Quy mô phiên (dùng cho H4 và S): ExpectedSize nếu có, ngược lại là số người quan tâm.
    // Dùng ở: ConstraintChecker.AddCapacityViolation (H4), ObjectiveCalculator (S và Smax); T2.2 khi gán phòng.
    public int GetSessionSize(int sessionId)
    {
        var session = GetSession(sessionId);

        if (session.ExpectedSize.HasValue)
        {
            return session.ExpectedSize.Value;
        }

        return InterestMatrix.Get(sessionId, sessionId);
    }

    // Khoảng thời gian phiên chiếm khi bắt đầu ở mốc slotIndex (chưa xét phiên có đặt được ở đó không).
    // Dùng ở: ConstraintChecker.BuildPlacedSession, ObjectiveCalculator.BuildPlacedSessions
    // (đổi mốc bắt đầu thành khoảng thời gian để so chồng giờ).
    public (int DayIndex, TimeInterval Interval) GetSessionInterval(int sessionId, int slotIndex)
    {
        var session = GetSession(sessionId);

        if (slotIndex < 0 || slotIndex >= Timeline.Slots.Count)
        {
            throw new ArgumentException($"Không có mốc thời gian số {slotIndex}.");
        }

        var slot = Timeline.Slots[slotIndex];
        var interval = new TimeInterval(slot.StartMinute, slot.StartMinute + session.DurationMinutes);
        return (slot.DayIndex, interval);
    }

    // Các mốc bắt đầu thỏa H5 (trong cửa sổ ngày, không qua khung nghỉ, mọi diễn giả đều rảnh).
    // Dùng ở: ProblemGenerator.BuildProblemWithPlaceableSessions (phát hiện phiên hết chỗ đặt);
    // T2.2 duyệt danh sách này để chọn mốc cho phiên.
    public IReadOnlyList<int> GetValidStartSlots(int sessionId)
    {
        if (!_validStartSlotsBySession.TryGetValue(sessionId, out var validSlots))
        {
            throw new ArgumentException($"Không có phiên mã {sessionId}.");
        }

        return validSlots;
    }

    // Hỏi nhanh một mốc có thỏa H5 không (tra HashSet).
    // Dùng ở: ConstraintChecker.CheckPlacement và Validate (kiểm H5).
    public bool IsValidStartSlot(int sessionId, int slotIndex)
    {
        if (!_validStartSetsBySession.TryGetValue(sessionId, out var validSet))
        {
            throw new ArgumentException($"Không có phiên mã {sessionId}.");
        }

        return validSet.Contains(slotIndex);
    }

    // ---------- KIỂM TRA ĐẦU VÀO (hàm tĩnh) ----------

    // Ngày phải có giờ bắt đầu trước giờ kết thúc, giờ nghỉ phải hợp lệ.
    // Helper của constructor (cùng lớp).
    private static void ValidateDays(IReadOnlyList<SchedulingDay> days)
    {
        if (days.Count == 0)
        {
            throw new ArgumentException("Cần ít nhất một ngày.", nameof(days));
        }

        foreach (var day in days)
        {
            if (day.StartMinute >= day.EndMinute)
            {
                throw new ArgumentException("Ngày phải có giờ bắt đầu trước giờ kết thúc.", nameof(days));
            }

            foreach (var dayBreak in day.Breaks)
            {
                if (dayBreak.StartMinute >= dayBreak.EndMinute)
                {
                    throw new ArgumentException("Khung nghỉ phải có giờ bắt đầu trước giờ kết thúc.", nameof(days));
                }

                if (dayBreak.StartMinute < day.StartMinute || dayBreak.EndMinute > day.EndMinute)
                {
                    throw new ArgumentException("Khung nghỉ phải nằm trong ngày.", nameof(days));
                }
            }
        }
    }

    // Có phòng, Id không trùng, sức chứa lớn hơn 0.
    // Helper của constructor (cùng lớp).
    private static void ValidateRooms(IReadOnlyList<SchedulingRoom> rooms)
    {
        if (rooms.Count == 0)
        {
            throw new ArgumentException("Cần ít nhất một phòng.", nameof(rooms));
        }

        var roomIds = new HashSet<int>();
        foreach (var room in rooms)
        {
            if (!roomIds.Add(room.Id))
            {
                throw new ArgumentException($"Mã phòng {room.Id} bị trùng.", nameof(rooms));
            }

            if (room.Capacity <= 0)
            {
                throw new ArgumentException($"Sức chứa phòng {room.Id} phải lớn hơn 0.", nameof(rooms));
            }
        }
    }

    // Có phiên, Id không trùng, thời lượng hợp lệ và chia hết cho slotMinutes.
    // Helper của constructor (cùng lớp).
    private static void ValidateSessions(IReadOnlyList<SchedulingSession> sessions, int slotMinutes)
    {
        if (sessions.Count == 0)
        {
            throw new ArgumentException("Cần ít nhất một phiên.", nameof(sessions));
        }

        var sessionIds = new HashSet<int>();
        foreach (var session in sessions)
        {
            if (!sessionIds.Add(session.Id))
            {
                throw new ArgumentException($"Mã phiên {session.Id} bị trùng.", nameof(sessions));
            }

            if (session.DurationMinutes <= 0 || session.DurationMinutes % slotMinutes != 0)
            {
                throw new ArgumentException(
                    $"Thời lượng phiên {session.Id} phải lớn hơn 0 và chia hết cho {slotMinutes} phút.",
                    nameof(sessions));
            }

            if (session.ExpectedSize.HasValue && session.ExpectedSize.Value < 0)
            {
                throw new ArgumentException($"Quy mô dự kiến của phiên {session.Id} không được âm.", nameof(sessions));
            }
        }
    }

    // Ngày và khoảng giờ rảnh phải hợp lệ.
    // Helper của constructor (cùng lớp).
    private static void ValidateSpeakerAvailabilities(
        IReadOnlyList<SpeakerAvailability> speakerAvailabilities,
        int dayCount)
    {
        foreach (var availability in speakerAvailabilities)
        {
            if (availability.DayIndex < 0 || availability.DayIndex >= dayCount)
            {
                throw new ArgumentException(
                    $"Giờ rảnh của diễn giả {availability.SpeakerId} trỏ tới ngày không có.",
                    nameof(speakerAvailabilities));
            }

            if (availability.StartMinute >= availability.EndMinute)
            {
                throw new ArgumentException(
                    $"Giờ rảnh của diễn giả {availability.SpeakerId} phải có giờ bắt đầu trước giờ kết thúc.",
                    nameof(speakerAvailabilities));
            }
        }
    }

    // Mỗi nguyện vọng phải trỏ đến một phiên tồn tại.
    // Helper của constructor (cùng lớp).
    private static void ValidateInterests(IReadOnlyList<InterestEntry> interests, IReadOnlyList<SchedulingSession> sessions)
    {
        var sessionIds = new HashSet<int>();
        foreach (var session in sessions)
        {
            sessionIds.Add(session.Id);
        }

        foreach (var interest in interests)
        {
            if (!sessionIds.Contains(interest.SessionId))
            {
                throw new ArgumentException(
                    $"Nguyện vọng của người {interest.AttendeeId} trỏ tới phiên {interest.SessionId} không có.",
                    nameof(interests));
            }
        }
    }

    // Helper của BuildAvailabilityBySpeaker (cùng lớp): gộp các khoảng liền nhau / chồng nhau
    // của một ngày, trả về danh sách đã sắp theo giờ bắt đầu.
    private static List<TimeInterval> MergeIntervals(List<TimeInterval> intervals)
    {
        var merge = new List<TimeInterval>();
        if (intervals.Count == 0)
        {
            return merge;
        }

        intervals.Sort((a, b) => a.StartMinute.CompareTo(b.StartMinute));

        var left = intervals[0].StartMinute;
        var right = intervals[0].EndMinute;
        foreach (var interval in intervals)
        {
            if (interval.StartMinute > right)
            {
                merge.Add(new TimeInterval(left, right));
                left = interval.StartMinute;
                right = interval.EndMinute;
            }
            else if (interval.EndMinute > right)
            {
                right = interval.EndMinute;
            }
        }

        merge.Add(new TimeInterval(left, right));
        return merge;
    }

    // ---------- DỰNG DỮ LIỆU TRA CỨU ----------
    // Gộp các khoảng rảnh liền nhau hoặc chồng nhau của cùng một diễn giả trong cùng một ngày.
    // Helper của constructor (cùng lớp); kết quả dùng ở AreAllSpeakersFree.
    /*
    _availabilityBySpeaker:
        {
            101 → {
                0 → [[480,600), [630,720)]
            },
            102 → {
                0 → [[540,720)]
            }
        }
    */
    private void BuildAvailabilityBySpeaker()
    {
        var unmergedAvailabilityBySpeaker = new Dictionary<int, Dictionary<int, List<TimeInterval>>>();

        // gom theo speakerId và dayIndex, chưa gộp khoảng
        foreach (var availability in SpeakerAvailabilities)
        {
            if (!unmergedAvailabilityBySpeaker.TryGetValue(availability.SpeakerId, out var availabilityByDay))
            {
                availabilityByDay = [];
                unmergedAvailabilityBySpeaker[availability.SpeakerId] = availabilityByDay;
            }

            if (!availabilityByDay.TryGetValue(availability.DayIndex, out var dailyIntervals))
            {
                dailyIntervals = [];
                availabilityByDay[availability.DayIndex] = dailyIntervals;
            }

            dailyIntervals.Add(new TimeInterval(availability.StartMinute, availability.EndMinute));
        }

        /*
            Đến đây thì dữ liệu trông như sau
            unmergedAvailabilityBySpeaker
            └── 101
                └── 0
                    ├── [480, 540)
                    ├── [540, 600)
                    └── [630, 720)
        */
        foreach (var availabilityBySpeakerId in unmergedAvailabilityBySpeaker)
        {
            var mergedAvailabilityByDay = new Dictionary<int, List<TimeInterval>>();
            var speakerId = availabilityBySpeakerId.Key;

            var availabilityByDay = availabilityBySpeakerId.Value;
            foreach (var dayAvailability in availabilityByDay)
            {
                var dayIndex = dayAvailability.Key;
                var dailyIntervals = dayAvailability.Value;
                mergedAvailabilityByDay[dayIndex] = MergeIntervals(dailyIntervals);
            }

            _availabilityBySpeaker[speakerId] = mergedAvailabilityByDay;
        }
    }

    // Với mỗi phiên, lọc ra các mốc bắt đầu thỏa H5. Tính một lần để thuật toán chỉ việc tra.
    // Helper của constructor (cùng lớp); kết quả dùng ở GetValidStartSlots, IsValidStartSlot.
    private void BuildValidStartSlots()
    {
        foreach (var session in Sessions)
        {
            var validSlots = new List<int>();
            var validSet = new HashSet<int>();

            foreach (var slot in Timeline.Slots)
            {
                if (!Timeline.CanHost(slot.Index, session.DurationMinutes))
                {
                    continue;
                }

                var interval = new TimeInterval(slot.StartMinute, slot.StartMinute + session.DurationMinutes);
                if (AreAllSpeakersFree(session, slot.DayIndex, interval))
                {
                    validSlots.Add(slot.Index);
                    validSet.Add(slot.Index);
                }
            }

            _validStartSlotsBySession[session.Id] = validSlots;
            _validStartSetsBySession[session.Id] = validSet;
            /*
            _validStartSlotsBySession
            {
                1 → [0, 1, 2, 4, 5]
            }
            */
        }
    }

    // Diễn giả không khai dòng nào: rảnh mọi lúc. Đã khai: phải nằm trọn trong một khoảng đã khai.
    // Helper của BuildValidStartSlots (cùng lớp).
    private bool AreAllSpeakersFree(SchedulingSession session, int dayIndex, TimeInterval interval)
    {
        foreach (var speakerId in session.SpeakerIds)
        {
            // Không tìm thấy list thời gian rảnh tức là rảnh full
            if (!_availabilityBySpeaker.TryGetValue(speakerId, out var byDay))
            {
                continue;
            }

            var isFree = false;
            if (byDay.TryGetValue(dayIndex, out var dayIntervals))
            {
                foreach (var freeInterval in dayIntervals)
                {
                    if (freeInterval.Contains(interval))
                    {
                        isFree = true;
                        break;
                    }
                }
            }

            if (!isFree)
            {
                return false;
            }
        }

        return true;
    }
}
