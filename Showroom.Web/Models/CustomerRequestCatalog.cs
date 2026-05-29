using Microsoft.AspNetCore.Mvc.Rendering;

namespace Showroom.Web.Models;

public static class CustomerRequestCatalog
{
    public const string ViewCar = "ViewCar";
    public const string Consultation = "Consultation";
    public const string Deposit = "Deposit";
    public const string TestDrive = "TestDrive";

    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ViewCar,
        Consultation,
        Deposit,
        TestDrive
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        Pending,
        Confirmed,
        Cancelled
    };

    public static IReadOnlyList<SelectListItem> GetTypeSelectList()
        => new[]
        {
            new SelectListItem { Value = ViewCar, Text = "Đặt lịch xem xe" },
            new SelectListItem { Value = Consultation, Text = "Tư vấn" },
            new SelectListItem { Value = Deposit, Text = "Đặt cọc trước" },
            new SelectListItem { Value = TestDrive, Text = "Lịch lái thử" }
        };

    public static string GetTypeLabel(string type)
        => type switch
        {
            ViewCar => "Đặt lịch xem xe",
            Consultation => "Tư vấn",
            Deposit => "Đặt cọc trước",
            TestDrive => "Lịch lái thử",
            _ => type
        };

    public static string GetStatusLabel(string status)
        => status switch
        {
            Pending => "Chờ xác nhận",
            Confirmed => "Đã xác nhận",
            Cancelled => "Đã hủy",
            _ => status
        };

    public static string GetPublicDescription(string type)
        => type switch
        {
            ViewCar => "Đăng ký lịch đến showroom để xem nội thất, ngoại thất và nghe giới thiệu chi tiết về dòng xe bạn quan tâm.",
            Consultation => "Nhận tư vấn online hoặc trực tiếp về nhu cầu sử dụng, ngân sách, tính năng, chi phí lăn bánh và mua trả góp.",
            TestDrive => "Đăng ký lái thử để cảm nhận động cơ, hệ thống treo, khả năng cách âm và các tính năng an toàn của xe.",
            Deposit => "Đặt cọc giữ xe hoặc đặt cọc ký hợp đồng khi bạn đã chọn được mẫu xe, màu sắc và điều kiện giao xe.",
            _ => string.Empty
        };

    public static IReadOnlyList<string> GetStaffChecklist(string type)
        => type switch
        {
            ViewCar => new[]
            {
                "CSKH tiếp nhận tên, số điện thoại, dòng xe, ngày giờ dự kiến đến.",
                "Chuyển thông tin cho trưởng nhóm bán hàng để chỉ định Sales phụ trách.",
                "Sales gọi hoặc nhắn tin xác nhận lịch, chuẩn bị catalog và kiểm tra xe trưng bày.",
                "Khi khách đến, Sales đón tiếp tại quầy và dẫn đến khu vực trưng bày."
            },
            Consultation => new[]
            {
                "Xác định mục đích sử dụng: gia đình, công việc, chạy dịch vụ.",
                "Hỏi ngân sách và các tính năng ưu tiên: tiết kiệm nhiên liệu, an toàn, thiết kế.",
                "Gợi ý phiên bản phù hợp, phân tích thông số, tiện nghi và tính năng nổi bật.",
                "Lập chi phí lăn bánh và tư vấn trả góp nếu khách có nhu cầu.",
                "Cập nhật khuyến mãi, quà tặng phụ kiện và ưu đãi dịch vụ trong tháng."
            },
            TestDrive => new[]
            {
                "Kiểm tra khách có bằng lái ô tô hợp lệ và CMND/CCCD.",
                "Kiểm tra xe demo: nhiên liệu, vệ sinh, áp suất lốp và hồ sơ lái thử.",
                "Cho khách ký biên bản đăng ký lái thử và lưu thông tin bằng lái.",
                "Sales lái trước để giới thiệu thao tác và lộ trình quy định.",
                "Khách đổi lái, Sales ngồi ghế phụ để hướng dẫn tính năng và hỗ trợ tình huống.",
                "Sau khi hoàn thành, ghi nhận phản hồi của khách về xe."
            },
            Deposit => new[]
            {
                "Thống nhất phiên bản xe, màu ngoại thất/nội thất, giá cuối, phụ kiện và lịch giao xe.",
                "Lập hợp đồng mua bán hoặc phiếu đặt cọc với thông tin xe, tiền cọc và điều khoản xử lý cọc.",
                "Hướng dẫn khách thanh toán tiền cọc tại kế toán hoặc tài khoản công ty.",
                "Kế toán xuất phiếu thu hoặc xác nhận giao dịch thành công.",
                "Cập nhật trạng thái xe sang Đã đặt cọc hoặc gửi lệnh đặt hàng về nhà máy nếu xe chờ giao."
            },
            _ => Array.Empty<string>()
        };

    public static string GetEmailNextSteps(string type)
        => type switch
        {
            ViewCar => "Sales phụ trách sẽ chuẩn bị catalog, kiểm tra xe trưng bày và đón tiếp bạn tại showroom theo lịch hẹn.",
            Consultation => "Nhân viên tư vấn sẽ liên hệ để trao đổi nhu cầu sử dụng, ngân sách, chi phí lăn bánh, gói trả góp và ưu đãi hiện có.",
            TestDrive => "Khi đến lái thử, vui lòng mang bằng lái xe ô tô còn hạn và CMND/CCCD để hoàn tất biên bản đăng ký lái thử.",
            Deposit => "Nhân viên showroom sẽ liên hệ để thống nhất thông tin xe, số tiền cọc, điều khoản đặt cọc và hướng dẫn thanh toán vào tài khoản công ty.",
            _ => "Nhân viên showroom sẽ liên hệ để hỗ trợ bạn."
        };

    public static bool IsValidType(string type)
        => ValidTypes.Contains(type ?? string.Empty);

    public static bool IsValidStatus(string status)
        => ValidStatuses.Contains(status ?? string.Empty);
}
