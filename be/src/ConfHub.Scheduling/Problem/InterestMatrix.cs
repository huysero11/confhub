namespace ConfHub.Scheduling.Problem;

// Ma trận đồng quan tâm W: W[a][b] = số người cùng quan tâm cả phiên a và phiên b.
// Đường chéo W[a][a] = số người quan tâm phiên a. Tính một lần khi dựng bài toán.
public sealed class InterestMatrix
{
    private readonly Dictionary<int, int> _indexBySessionId = [];
    private readonly int[][] _w;

    // Dùng ở: constructor SchedulingProblem (dựng một lần, sau đó chỉ tra).
    public InterestMatrix(IReadOnlyList<SchedulingSession> sessions, IReadOnlyList<InterestEntry> interests)
    {
        /*
            Khởi tạo kích cỡ của _w
            VD: có 3 phiên thì _w[3][3]
        */
        _w = new int[sessions.Count][];
        for (var i = 0; i < sessions.Count; i++)
        {
            _w[i] = new int[sessions.Count];
        }

        /*
            Đánh chỉ số cho các phiên <=> dùng map
            để giảm độ lớn của map, ví dụ id của các phiên là 10, 50, 100 => map chỉ cần 3 cái là 0, 1, 2
        */
        for (var i = 0; i < sessions.Count; i++)
        {
            _indexBySessionId[sessions[i].Id] = i;
        }

        var sessionsByAttendee = GroupSessionsByAttendee(interests);
        CalculateMatrix(sessionsByAttendee);
    }

    // Tổng W[a][b] trên mọi cặp a < b: cận trên của số xung đột C, dùng để chuẩn hóa.
    // Dùng ở: ObjectiveCalculator.Evaluate (mẫu số của Cn).
    public int TotalCoInterest { get; private set; }

    // Tra W[a][b] theo mã phiên; Get(a, a) = số người quan tâm phiên a.
    // Dùng ở: SchedulingProblem.GetSessionSize (đường chéo), ObjectiveCalculator.CalculateConflictCount (C);
    // T2.2 dùng khi chọn mốc ít xung đột.
    // Dùng ở: ConflictContribution.Calculate (cộng W vào đóng góp của hai phiên chồng giờ).
    public int Get(int firstSessionId, int secondSessionId)
    {
        var firstIndex = GetIndex(firstSessionId);
        var secondIndex = GetIndex(secondSessionId);
        return _w[firstIndex][secondIndex];
    }

    // Helper của constructor (cùng lớp): gom các phiên mỗi người quan tâm, bỏ trùng. Ví dụ kết quả:
    /*
        sessionsByAttendee
        {
            1 → [0, 1]
            2 → [0, 2]
            3 → [0, 1, 2]
            4 → [1, 2]
        }
    */
    private Dictionary<int, List<int>> GroupSessionsByAttendee(IReadOnlyList<InterestEntry> interests)
    {
        var sessionsByAttendee = new Dictionary<int, List<int>>();
        foreach (var interest in interests)
        {
            var attendeeId = interest.AttendeeId;
            var sessionIndex = GetIndex(interest.SessionId);

            if (!sessionsByAttendee.TryGetValue(attendeeId, out var list))
            {
                list = [];
                sessionsByAttendee.Add(attendeeId, list);
            }

            if (!list.Contains(sessionIndex))
            {
                list.Add(sessionIndex);
            }
        }

        return sessionsByAttendee;
    }

    // Helper của constructor (cùng lớp): mỗi người +1 vào đường chéo của từng phiên họ quan tâm
    // và +1 vào mỗi cặp phiên họ cùng quan tâm (đối xứng).
    private void CalculateMatrix(Dictionary<int, List<int>> sessionsByAttendee)
    {
        foreach (var sessions in sessionsByAttendee.Values)
        {
            for (var i = 0; i < sessions.Count; i++)
            {
                _w[sessions[i]][sessions[i]]++;
                for (var j = i + 1; j < sessions.Count; j++)
                {
                    _w[sessions[i]][sessions[j]]++;
                    _w[sessions[j]][sessions[i]]++;
                    TotalCoInterest++;
                }
            }
        }
    }

    // Helper của Get và GroupSessionsByAttendee (cùng lớp): đổi mã phiên sang chỉ số hàng / cột.
    private int GetIndex(int sessionId)
    {
        if (!_indexBySessionId.TryGetValue(sessionId, out var index))
        {
            throw new ArgumentException($"Không có phiên mã {sessionId}.");
        }

        return index;
    }
}
