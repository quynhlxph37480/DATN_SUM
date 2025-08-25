using AppAPI.IRepository;
using AppData;
using AppData.ViewModel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace AppAPI.Repository
{
    public class ThongKeRepo : IThongKeRepo
    {
        private AppDbcontext _context;
        public ThongKeRepo(AppDbcontext appDbcontext)
        {
            _context = appDbcontext;
        }
        // Thông kê ngày
            public async Task<List<ThongKeNgay>> GetStatisticsByDate(DateTime date)
            {
                var startDate = date.Date;
                var endDate = startDate.AddDays(1);

                var hoaDonsTrongNgay = await _context.hoaDons
                    .Where(hd => hd.NgayTao >= startDate && hd.NgayTao < endDate)
                    .ToListAsync();
               var tongtien = await _context.hoaDons
    .Where(ls => ls.NgayTao >= startDate && ls.NgayTao < endDate
        && (ls.TrangThai == "Đã Xác Nhận" 
            || ls.TrangThai == "Đã Thanh Toán" 
            || ls.TrangThai == "Hoàn Thành"))
    .SumAsync(ls => (ls.TongTienDonHang - (ls.TienGiam ?? 0)));


                var result = hoaDonsTrongNgay
                    .GroupBy(hd => hd.NgayTao.Date)
                    .Select(g => new ThongKeNgay
                    {
                        Ngay = g.Key,
                        TongDonHang = g.Count(hd => hd.TrangThai != "Tạo đơn hàng"),
                        DonHangChoXacNhan = g.Count(hd => hd.TrangThai == "Chờ Xác Nhận"),
                        DonHangHuy = g.Count(hd => hd.TrangThai == "Đã Hủy"),
                        DonHangThanhCong = g.Count(hd => hd.TrangThai == "Hoàn Thành" || hd.TrangThai == "Đã Thanh Toán"),
                        TongTien = tongtien
                    })
                    .ToList();
                return result;
            }

        //Thống kê tuần
        public async Task<List<ThongKeNgay>> GetStatisticsByWeek(DateTime date)
        {
            // Lấy ngày bắt đầu và kết thúc tuần (bắt đầu từ Chủ Nhật)
            var startOfWeek = date.Date.AddDays(-(int)date.DayOfWeek);
            var endOfWeek = startOfWeek.AddDays(7);

            // Lấy tất cả đơn hàng trong tuần
            var hoaDonsTrongTuan = await _context.hoaDons
                .Where(hd => hd.NgayTao >= startOfWeek && hd.NgayTao < endOfWeek)
                .ToListAsync();

            // Tổng tiền chỉ tính các đơn đã xác nhận trở lên, có trừ TienGiam
            var tongtien = hoaDonsTrongTuan
                .Where(ls => ls.TrangThai == "Đã Xác Nhận"
                          || ls.TrangThai == "Đã Thanh Toán"
                          || ls.TrangThai == "Hoàn Thành")
                .Sum(ls => (ls.TongTienDonHang - (ls.TienGiam ?? 0)));

            // Gom lại thành 1 bản thống kê tổng cho cả tuần
            var result = new List<ThongKeNgay>
    {
        new ThongKeNgay
        {
            TongDonHang = hoaDonsTrongTuan.Count(hd => hd.TrangThai != "Tạo đơn hàng"),
            DonHangChoXacNhan = hoaDonsTrongTuan.Count(hd => hd.TrangThai == "Chờ Xác Nhận"),
            DonHangHuy = hoaDonsTrongTuan.Count(hd => hd.TrangThai == "Đã Hủy"),
            DonHangThanhCong = hoaDonsTrongTuan.Count(hd => hd.TrangThai == "Hoàn Thành" || hd.TrangThai == "Đã Thanh Toán"),
            TongTien = tongtien
        }
    };

            return result;
        }

        //Thống kê tháng
        public async Task<List<ThongKeNgay>> GetStatisticsByMonth(int year, int month)
        {
            // Xác định ngày bắt đầu và kết thúc tháng
            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            // Lấy tất cả hóa đơn trong tháng
            var hoaDonsTrongThang = await _context.hoaDons
                .Where(hd => hd.NgayTao >= startOfMonth && hd.NgayTao < endOfMonth)
                .ToListAsync();

            // Chỉ tính tổng tiền những đơn đã xác nhận trở lên, có trừ TienGiam
            var tongtien = hoaDonsTrongThang
                .Where(ls => ls.TrangThai == "Đã Xác Nhận"
                          || ls.TrangThai == "Đã Thanh Toán"
                          || ls.TrangThai == "Hoàn Thành")
                .Sum(ls => (ls.TongTienDonHang - (ls.TienGiam ?? 0)));

            // Gom tất cả thành 1 thống kê chung cho cả tháng
            var result = new List<ThongKeNgay>
    {
        new ThongKeNgay
        {
            TongDonHang = hoaDonsTrongThang.Count(hd => hd.TrangThai != "Tạo đơn hàng"),
            DonHangChoXacNhan = hoaDonsTrongThang.Count(hd => hd.TrangThai == "Chờ Xác Nhận"),
            DonHangHuy = hoaDonsTrongThang.Count(hd => hd.TrangThai == "Đã Hủy"),
            DonHangThanhCong = hoaDonsTrongThang.Count(hd => hd.TrangThai == "Hoàn Thành" || hd.TrangThai == "Đã Thanh Toán"),
            TongTien = tongtien
        }
    };

            return result;
        }


        //Thống kê năm
        public async Task<List<ThongKeThang>> GetStatisticsByYear(int year)
        {
            // Lấy tất cả hóa đơn trong năm
            var hoaDonsTrongNam = await _context.hoaDons
                .Where(hd => hd.NgayTao.Year == year)
                .ToListAsync();

            // Chỉ tính tổng tiền những đơn đã xác nhận trở lên, có trừ TienGiam (voucher)
            var tongTienTheoNam = await _context.hoaDons
                .Where(ls => ls.NgayTao.Year == year &&
                             (ls.TrangThai == "Đã Xác Nhận"
                              || ls.TrangThai == "Đã Thanh Toán"
                              || ls.TrangThai == "Hoàn Thành"))
                .SumAsync(ls => (ls.TongTienDonHang - (ls.TienGiam ?? 0)));

            // Gom tất cả lại thành một thống kê tổng cho cả năm
            var result = hoaDonsTrongNam
                .GroupBy(_ => true)
                .Select(g => new ThongKeThang
                {
                    TongDonHang = g.Count(hd => hd.TrangThai != "Tạo đơn hàng"),
                    DonHangChoXacNhan = g.Count(hd => hd.TrangThai == "Chờ Xác Nhận"),
                    DonHangHuy = g.Count(hd => hd.TrangThai == "Đã Hủy"),
                    DonHangThanhCong = g.Count(hd => hd.TrangThai == "Hoàn Thành" || hd.TrangThai == "Đã Thanh Toán"),
                    TongTien = tongTienTheoNam
                })
                .ToList();

            return result;
        }

        //Thông kê tổng quan
        public async Task<ThongKeTongQuan> GetTotalOrdersAndRevenue()
        {
            // Đếm tổng đơn (loại bỏ đơn mới tạo)
            int totalOrders = await _context.hoaDons
                .CountAsync(hd => hd.TrangThai != "Tạo đơn hàng");

            // Tính tổng doanh thu (chỉ lấy đơn đã xác nhận/thanh toán/hoàn thành)
            double totalRevenue = await _context.hoaDons
                .Where(hd => hd.TrangThai == "Đã Xác Nhận"
                          || hd.TrangThai == "Đã Thanh Toán"
                          || hd.TrangThai == "Hoàn Thành")
                .SumAsync(hd => (hd.TongTienDonHang - (hd.TienGiam ?? 0)));
            // nếu GiamGia có thể null thì dùng (?? 0)

            return new ThongKeTongQuan
            {
                TongDonHang = totalOrders,
                TongDoanhThu = totalRevenue
            };
        }


        //Lọc
        public async Task<List<ThongKeKhoangThoiGian>> GetStatisticsByTimeRange(DateTime startDate, DateTime endDate)
        {
            var hoaDonsTrongKhoang = await _context.hoaDons
                .Where(hd => hd.NgayTao.Date >= startDate.Date && hd.NgayTao.Date <= endDate.Date)
                .ToListAsync();

            // Tính doanh thu sau khi trừ voucher
            var tongTienTrongKhoang = await _context.hoaDons
                .Where(ls => ls.NgayTao.Date >= startDate.Date
                          && ls.NgayTao.Date <= endDate.Date
                          && ls.TrangThai != "Chờ Xác Nhận"
                          && ls.TrangThai != "Đã Hủy")
                .GroupBy(ls => ls.NgayTao.Date)
                .Select(g => new
                {
                    Ngay = g.Key,
                    TongTien = g.Sum(ls => (ls.TongTienDonHang - (ls.TienGiam ?? 0)))
                })
                .ToListAsync();

            // Thống kê theo ngày
            var result = hoaDonsTrongKhoang
                .GroupBy(hd => hd.NgayTao.Date)
                .Select(g => new ThongKeKhoangThoiGian
                {
                    Ngay = g.Key,
                    TongDonHang = g.Count(hd => hd.TrangThai != "Tạo đơn hàng"),
                    DonHangChoXacNhan = g.Count(hd => hd.TrangThai == "Chờ Xác Nhận"),
                    DonHangHuy = g.Count(hd => hd.TrangThai == "Đã Hủy"),
                    DonHangThanhCong = g.Count(hd => hd.TrangThai == "Đã Thanh Toán" || hd.TrangThai == "Hoàn Thành"),
                    TongDoanhThu = tongTienTrongKhoang.FirstOrDefault(t => t.Ngay == g.Key)?.TongTien ?? 0
                })
                .OrderBy(tk => tk.Ngay)
                .ToList();

            return result;
        }



        //sản phẩm bán chạy
        public async Task<List<TopSellingProductViewModel>> GetTopSellingProductsAsync(DateTime? startDate, DateTime? endDate)
        {
            var query = _context.hoaDonChiTiets
                .Include(hdct => hdct.SanPhamChiTiet)
                .ThenInclude(spct => spct.SanPham)
                .Where(hdct => hdct.HoaDon.NgayTao >= (startDate ?? DateTime.MinValue)
                            && hdct.HoaDon.NgayTao <= (endDate ?? DateTime.MaxValue)
                            && hdct.HoaDon.TrangThai != "Chờ xác nhận"  // Trạng thái khác "Chờ xác nhận"
                            && hdct.HoaDon.TrangThai != "Đã hủy")     // Trạng thái khác "Đã hủy"
                .GroupBy(hdct => hdct.IdSanPhamChiTiet)
                .Select(group => new TopSellingProductViewModel
                {
                    SanPhamId = group.Key,
                    TenSanPham = group.FirstOrDefault().SanPhamChiTiet.SanPham.TenSanPham,
                    SoLuongBan = group.Sum(g => g.SoLuong),
                    TongDoanhThu = group.Sum(g => g.SoLuong * g.DonGia)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(10);
            return await query.ToListAsync();
        }

        // Thống kê doanh thu
        public async Task<List<ThongKeDoanhThu>> GetRevenueStatisticsAsync(DateTime startDate, DateTime endDate)
        {
            var hoaDons = await _context.hoaDons
                .Where(hd => hd.NgayTao.Date >= startDate.Date &&
                     hd.NgayTao.Date <= endDate.Date &&
                     (hd.TrangThai != "Chờ Xác Nhận" && hd.TrangThai != "Đã Hủy"))
                .ToListAsync();

            var result = hoaDons
                .GroupBy(hd => hd.NgayTao.Date)
                .Select(g => new ThongKeDoanhThu
                {
                    Ngay = g.Key,
                    TongDoanhThu = g.Sum(hd => hd.TongTienDonHang)
                })
                .OrderBy(tk => tk.Ngay)
                .ToList();

            return result;
        }
    }
}
