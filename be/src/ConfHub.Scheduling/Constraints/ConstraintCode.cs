namespace ConfHub.Scheduling.Constraints;

// Mã ràng buộc cứng. Giao diện dịch thông báo theo mã này.
public enum ConstraintCode
{
    // Hai phiên chồng giờ có chung một diễn giả.
    H1,

    // Tại một mốc, số phiên đang diễn ra vượt số phòng.
    H2,

    // Hai phiên chồng giờ được gán cùng một phòng.
    H3,

    // Sức chứa phòng nhỏ hơn quy mô phiên.
    H4,

    // Vị trí không hợp lệ: ngoài cửa sổ ngày, qua khung nghỉ hoặc diễn giả không rảnh.
    H5,
}
