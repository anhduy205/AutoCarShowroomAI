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
            new SelectListItem { Value = ViewCar, Text = "Dat lich xem xe" },
            new SelectListItem { Value = Consultation, Text = "Tu van" },
            new SelectListItem { Value = Deposit, Text = "Dat coc truoc" },
            new SelectListItem { Value = TestDrive, Text = "Lich lai thu" }
        };

    public static string GetTypeLabel(string type)
        => type switch
        {
            ViewCar => "Dat lich xem xe",
            Consultation => "Tu van",
            Deposit => "Dat coc truoc",
            TestDrive => "Lich lai thu",
            _ => type
        };

    public static string GetStatusLabel(string status)
        => status switch
        {
            Pending => "Cho xac nhan",
            Confirmed => "Da xac nhan",
            Cancelled => "Da huy",
            _ => status
        };

    public static string GetPublicDescription(string type)
        => type switch
        {
            ViewCar => "Dang ky lich den showroom de xem noi that, ngoai that va nghe gioi thieu chi tiet ve dong xe ban quan tam.",
            Consultation => "Nhan tu van online hoac truc tiep ve nhu cau su dung, ngan sach, tinh nang, chi phi lan banh va mua tra gop.",
            TestDrive => "Dang ky lai thu de cam nhan dong co, he thong treo, kha nang cach am va cac tinh nang an toan cua xe.",
            Deposit => "Dat coc giu xe hoac dat coc ky hop dong khi ban da chon duoc mau xe, mau sac va dieu kien giao xe.",
            _ => string.Empty
        };

    public static IReadOnlyList<string> GetStaffChecklist(string type)
        => type switch
        {
            ViewCar => new[]
            {
                "CSKH tiep nhan ten, so dien thoai, dong xe, ngay gio du kien den.",
                "Chuyen thong tin cho truong nhom ban hang de chi dinh Sales phu trach.",
                "Sales goi hoac nhan tin xac nhan lich, chuan bi catalog va kiem tra xe trung bay.",
                "Khi khach den, Sales don tiep tai quay va dan den khu vuc trung bay."
            },
            Consultation => new[]
            {
                "Xac dinh muc dich su dung: gia dinh, cong viec, chay dich vu.",
                "Hoi ngan sach va cac tinh nang uu tien: tiet kiem nhien lieu, an toan, thiet ke.",
                "Goi y phien ban phu hop, phan tich thong so, tien nghi va tinh nang noi bat.",
                "Lap chi phi lan banh va tu van tra gop neu khach co nhu cau.",
                "Cap nhat khuyen mai, qua tang phu kien va uu dai dich vu trong thang."
            },
            TestDrive => new[]
            {
                "Kiem tra khach co bang lai oto hop le va CMND/CCCD.",
                "Kiem tra xe demo: nhien lieu, ve sinh, ap suat lop va ho so lai thu.",
                "Cho khach ky bien ban dang ky lai thu va luu thong tin bang lai.",
                "Sales lai truoc de gioi thieu thao tac va lo trinh quy dinh.",
                "Khach doi lai, Sales ngoi ghe phu de huong dan tinh nang va ho tro tinh huong.",
                "Sau khi hoan thanh, ghi nhan phan hoi cua khach ve xe."
            },
            Deposit => new[]
            {
                "Thong nhat phien ban xe, mau ngoai that/noi that, gia cuoi, phu kien va lich giao xe.",
                "Lap hop dong mua ban hoac phieu dat coc voi thong tin xe, tien coc va dieu khoan xu ly coc.",
                "Huong dan khach thanh toan tien coc tai ke toan hoac tai khoan cong ty.",
                "Ke toan xuat phieu thu hoac xac nhan giao dich thanh cong.",
                "Cap nhat trang thai xe sang Da dat coc hoac gui lenh dat hang ve nha may neu xe cho giao."
            },
            _ => Array.Empty<string>()
        };

    public static string GetEmailNextSteps(string type)
        => type switch
        {
            ViewCar => "Sales phu trach se chuan bi catalog, kiem tra xe trung bay va don tiep ban tai showroom theo lich hen.",
            Consultation => "Nhan vien tu van se lien he de trao doi nhu cau su dung, ngan sach, chi phi lan banh, goi tra gop va uu dai hien co.",
            TestDrive => "Khi den lai thu, vui long mang bang lai xe oto con han va CMND/CCCD de hoan tat bien ban dang ky lai thu.",
            Deposit => "Nhan vien showroom se lien he de thong nhat thong tin xe, so tien coc, dieu khoan dat coc va huong dan thanh toan vao tai khoan cong ty.",
            _ => "Nhan vien showroom se lien he de ho tro ban."
        };

    public static bool IsValidType(string type)
        => ValidTypes.Contains(type ?? string.Empty);

    public static bool IsValidStatus(string status)
        => ValidStatuses.Contains(status ?? string.Empty);
}
