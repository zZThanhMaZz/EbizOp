using Microsoft.Data.Sqlite;

namespace EbizReports.Host.Services;

/// <summary>
/// Cơ sở dữ liệu SQLite demo cho báo cáo "danh sách phiếu bán hàng".
/// Tự tạo bảng và seed dữ liệu mẫu ở lần chạy đầu — minh hoạ nguồn dữ liệu SQL thật sự.
/// </summary>
public static class DemoDatabase
{
    public static string EnsureCreated(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var cs = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();
        using var db = new SqliteConnection(cs);
        db.Open();

        using (var create = db.CreateCommand())
        {
            create.CommandText = """
                CREATE TABLE IF NOT EXISTS PhieuBanHang (
                    MaPhieu   TEXT PRIMARY KEY,
                    NgayBan   TEXT NOT NULL,      -- ISO yyyy-MM-dd
                    KhachHang TEXT NOT NULL,
                    NhanVien  TEXT,
                    TongTien  REAL NOT NULL,
                    TrangThai TEXT NOT NULL
                );
                """;
            create.ExecuteNonQuery();
        }

        using (var check = db.CreateCommand())
        {
            check.CommandText = "SELECT COUNT(*) FROM PhieuBanHang";
            if (Convert.ToInt64(check.ExecuteScalar()) == 0)
                Seed(db);
        }

        return cs;
    }

    private static void Seed(SqliteConnection db)
    {
        var rows = new (string Ma, string Ngay, string Khach, string Nv, double Tien, string Trang)[]
        {
            ("PBH-2026-0001", "2026-01-05", "Nguyễn Văn An",    "Lan",  2350000, "Đã thanh toán"),
            ("PBH-2026-0002", "2026-01-11", "Trần Thị Bích",    "Lan",   860000, "Đã thanh toán"),
            ("PBH-2026-0003", "2026-01-19", "Lê Hoàng Long",    "Hùng", 1720000, "Đã thanh toán"),
            ("PBH-2026-0004", "2026-01-27", "Phạm Thu Hà",      "Hùng",   430000, "Công nợ"),
            ("PBH-2026-0005", "2026-02-03", "Đỗ Minh Quân",     "Lan",  3150000, "Đã thanh toán"),
            ("PBH-2026-0006", "2026-02-09", "Vũ Thị Mai",       "Hùng",  1280000, "Đã thanh toán"),
            ("PBH-2026-0007", "2026-02-14", "Bùi Quốc Bảo",     "Lan",   640000, "Công nợ"),
            ("PBH-2026-0008", "2026-02-22", "Hoàng Kim Chi",    "Hùng", 2140000, "Đã thanh toán"),
            ("PBH-2026-0009", "2026-03-02", "Ngô Thanh Tùng",   "Lan",  1750000, "Đã thanh toán"),
            ("PBH-2026-0010", "2026-03-08", "Dương Bảo Ngọc",   "Hùng",   920000, "Đã thanh toán"),
            ("PBH-2026-0011", "2026-03-15", "Lý Nhật Nam",      "Lan",  2680000, "Công nợ"),
            ("PBH-2026-0012", "2026-03-21", "Trương Mỹ Linh",   "Hùng",  1340000, "Đã thanh toán"),
            ("PBH-2026-0013", "2026-03-29", "Phan Anh Khoa",    "Lan",  1970000, "Đã thanh toán"),
            // ngoài khoảng mặc định (để minh hoạ lọc theo ngày)
            ("PBH-2025-0101", "2025-12-24", "Khách cũ 2025",    "Lan",   500000, "Đã thanh toán"),
            ("PBH-2026-0101", "2026-04-18", "Khách tháng 4",    "Hùng",  810000, "Đã thanh toán"),
        };

        using var tx = db.BeginTransaction();
        using var cmd = db.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText =
            "INSERT INTO PhieuBanHang (MaPhieu, NgayBan, KhachHang, NhanVien, TongTien, TrangThai) " +
            "VALUES ($ma, $ngay, $khach, $nv, $tien, $trang)";
        var pMa = cmd.CreateParameter(); pMa.ParameterName = "$ma"; cmd.Parameters.Add(pMa);
        var pNgay = cmd.CreateParameter(); pNgay.ParameterName = "$ngay"; cmd.Parameters.Add(pNgay);
        var pKhach = cmd.CreateParameter(); pKhach.ParameterName = "$khach"; cmd.Parameters.Add(pKhach);
        var pNv = cmd.CreateParameter(); pNv.ParameterName = "$nv"; cmd.Parameters.Add(pNv);
        var pTien = cmd.CreateParameter(); pTien.ParameterName = "$tien"; cmd.Parameters.Add(pTien);
        var pTrang = cmd.CreateParameter(); pTrang.ParameterName = "$trang"; cmd.Parameters.Add(pTrang);

        foreach (var r in rows)
        {
            pMa.Value = r.Ma;
            pNgay.Value = r.Ngay;
            pKhach.Value = r.Khach;
            pNv.Value = r.Nv;
            pTien.Value = r.Tien;
            pTrang.Value = r.Trang;
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }
}
