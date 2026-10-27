namespace ConfHub.Scheduling.Rooms;

// Giải bài toán phân công chữ nhật bằng thế vị và đường tăng, O(số hàng² × số cột).
// GroupRoomAssigner dùng hàng cho phiên, cột cho phòng; chỉ số trả về bắt đầu từ 0.
public static class HungarianAlgorithm
{
    // Chi phí thực tế tối đa cỡ 10^7 sau khi nhân 1000; 10^9 đủ lớn để đánh dấu ô không khả thi.
    // Chừa khoảng an toàn trong long khi cộng dồn thế vị qua nhiều hàng.
    public const long Infeasible = 1_000_000_000L;

    // ---------- GIẢI BÀI TOÁN PHÂN CÔNG ----------
    // Dùng ở: GroupRoomAssigner.TryAssign (tìm cách gán phòng có tổng chi phí nhỏ nhất).
    /*
        đang xét hàng i, ví dụ chọn cột j1 mà có thằng lấy rồi,
            thì coi như lấy j1 và sẽ chuyển cái thằng lấy j1 lúc trước sang cột j2,
            mà nếu j2 lại được lấy rồi thì cứ thế chuyển tiếp đến khi nhận được cái chưa ai lấy
    */
    public static int[] Solve(long[][] costs)
    {
        ArgumentNullException.ThrowIfNull(costs);
        ValidateCosts(costs);

        var n = costs.Length;

        if (n == 0)
        {
            return [];
        }

        var m = costs[0].Length;
        var u = new long[n + 1];
        var v = new long[m + 1];

        var p = new int[m + 1]; // p[j] là phiên đang dùng phòng j
        var way = new int[m + 1]; // lưu cột trước đó để truy vết trong đường tăng

        for (var i = 1; i <= n; i++)
        {
            var j0 = 0; // cột hiện tại
            p[j0] = i;

            var minv = new long[m + 1]; // minv[j] là chi phí tốt nhất để tới cột j
            Array.Fill(minv, long.MaxValue);

            var used = new bool[m + 1];
            do
            {
                used[j0] = true;
                var i0 = p[j0]; // hàng được gán của cột j0
                var delta = long.MaxValue; // min(minv[j])
                var j1 = 0; // cột tốt nhất để j0 nhảy đến

                for (var j = 1; j <= m; j++)
                {
                    if (used[j])
                    {
                        continue;
                    }

                    var cur = costs[i0 - 1][j - 1] - u[i0] - v[j];

                    if (cur < minv[j])
                    {
                        minv[j] = cur;
                        way[j] = j0;
                    }

                    if (minv[j] < delta)
                    {
                        delta = minv[j];
                        j1 = j;
                    }
                }

                // cập nhật thế vị
                for (var j = 0; j <= m; j++)
                {
                    if (used[j])
                    {
                        u[p[j]] += delta;
                        v[j] -= delta;
                    }
                    else
                    {
                        minv[j] -= delta;
                    }
                }

                // đi tới cột tốt nhất
                j0 = j1;
            }
            while (p[j0] != 0); // khi cột j0 đã được gán cho hàng nào đó thì vẫn lặp

            // lật ngược đường tăng, đổi phòng
            do
            {
                p[j0] = p[way[j0]];
                j0 = way[j0];
            }
            while (j0 != 0);
        }

        // Đổi p[cột] = hàng sang res[hàng] = cột
        var res = new int[n];
        for (var j = 1; j <= m; j++)
        {
            if (p[j] != 0)
            {
                res[p[j] - 1] = j - 1;
            }
        }

        return res; // res[i] = j là phiên i dùng phòng j (i và j chưa phải id)
    }

    // ---------- KIỂM TRA MA TRẬN ----------
    // Helper của Solve (cùng lớp): cần ma trận chữ nhật, đủ cột và chi phí trong miền an toàn.
    private static void ValidateCosts(long[][] costs)
    {
        if (costs.Length == 0)
        {
            return;
        }

        if (costs[0] is null || costs.Length > costs[0].Length)
        {
            throw new ArgumentException("Ma trận phải có số hàng không lớn hơn số cột.", nameof(costs));
        }

        foreach (var rowCosts in costs)
        {
            if (rowCosts is null || rowCosts.Length != costs[0].Length)
            {
                throw new ArgumentException("Các hàng của ma trận phải có cùng số cột.", nameof(costs));
            }

            foreach (var cost in rowCosts)
            {
                if (cost < 0 || cost > Infeasible)
                {
                    throw new ArgumentException("Chi phí phải nằm trong [0, Infeasible].", nameof(costs));
                }
            }
        }
    }
}
