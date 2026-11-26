# EbizReports

Module **report designer / engine** tự xây cho EbizOp (Mức 2), thiết kế để **plug-in** khi có source.
Trạng thái: **P0 xong** (engine render PDF/XLSX/CSV) và **P1 đến bước 22** (designer Blazor + MudBlazor: kéo/thả, nhóm, tham số, biểu đồ, bảng, báo cáo con, định dạng có điều kiện, ảnh/logo, sắp xếp & nhóm dữ liệu, bảng trường dữ liệu, xem trước WYSIWYG, bảng con theo nhóm, sao lưu/khôi phục & nhân bản, expression builder có xem trước, phân trang/keep-together, nhật ký thao tác, nguồn dữ liệu SQL SQLite + SQL Server với nút tạo báo cáo mới, **giao diện giống DevExpress Reports** — Property Grid, Toolbox, bố cục 3 panel, kéo giãn chiều cao band, thước mm + zoom 50–200%, menu chuột phải, thanh công cụ dạng tab).

## Cấu trúc

```
EbizReports.sln
├─ src/EbizReports.Core        (net8.0;net9.0)  model ReportDefinition + expression engine + grouping
├─ src/EbizReports.Data        (net8.0;net9.0)  nguồn dữ liệu SQL (ADO.NET: SQLite + SQL Server)
├─ src/EbizReports.Rendering   (net8.0;net9.0)  renderer QuestPDF / MiniExcel / CSV (font Lato nhúng sẵn)
├─ src/EbizReports.Designer    (net8.0;net9.0)  UI designer (Razor Class Library, MudBlazor)
├─ samples/EbizReports.Demo    (net8.0)         console demo: render hóa đơn + báo cáo nhóm
└─ samples/EbizReports.Host    (net8.0)         Blazor Server host chạy designer (mở trên browser)
```

## Chạy engine (console demo)

```powershell
cd EbizReports
dotnet run --project samples/EbizReports.Demo
```

Kết quả ghi vào `out/`:
- `hoa-don.pdf` / `.xlsx` / `.csv` —— hóa đơn A5 (page header/detail/footer, tổng, số trang)
- `ban-hang-theo-nhom.pdf` / `.xlsx` / `.csv` —— báo cáo A4 ngang, group header/footer + aggregate
- `chart-bar.png` / `chart-line.png` / `chart-pie.png` —— kiểm chứng biểu đồ (bar / line / pie)
- `table-demo.png` —— kiểm chứng item Bảng (tiêu đề + sọc + dòng tổng)
- `image-demo.png` —— kiểm chứng item Ảnh (logo SVG + PNG nhúng + nguồn lỗi bỏ qua)
- `sort-group.png` —— kiểm chứng sắp xếp & nhóm (nhóm tăng dần, trong nhóm giảm dần)
- `service-hoa-don.pdf` —— render qua lớp tích hợp `ReportService`
- `store/` —— kho file JSON (`FileReportStore`) kiểm chứng ghi/đọc
- `hoa-don.json` —— định nghĩa report dạng JSON (round-trip)

## Chạy designer (Blazor)

```powershell
cd EbizReports
dotnet run --project samples/EbizReports.Host
```

Mở `http://localhost:5199`. Chọn báo cáo ở ô trên cùng (Hóa đơn · Bán hàng theo nhóm · Top sản phẩm),
hoặc `/designer/ban-hang-theo-nhom`, `/designer/top-san-pham`.

> Chạy trực tiếp bằng `dotnet run` cần **Development** để phục vụ static web assets (`_content/*`).
> `Properties/launchSettings.json` đã đặt sẵn `ASPNETCORE_ENVIRONMENT=Development`, nên chạy
> `dotnet run --project samples/EbizReports.Host` (không kèm `--no-launch-profile`) là đủ.
> Nếu chạy `--no-launch-profile`, phải set `ASPNETCORE_ENVIRONMENT=Development` thủ công,
> nếu không CSS/JS từ NuGet & RCL sẽ trả 404.

## Đã hỗ trợ (P0)

| Thành phần | Trạng thái |
|---|---|
| ReportDefinition JSON (paper/margins/bands/items/grouping/params) | ✅ |
| Expression engine (parser an toàn, không eval code) | ✅ |
| Hàm: Sum, Avg, Count, Min, Max, IIF, Upper, Lower, Trim, Concat, Round, Abs, Format, Today, Now, Len, Page, Pages | ✅ |
| Interpolation `{Field}` trong text tĩnh | ✅ |
| Bands: ReportHeader, PageHeader, GroupHeader, Detail, GroupFooter, PageFooter, ReportFooter | ✅ |
| Items: Text, Field, Expression, PageNumber, Line, Rect, Image, **Barcode, QrCode** | ✅ |
| Mã vạch (Code128/Code39/EAN13/EAN8/UPCA/ITF) + QRCode, vẽ **vector SVG** | ✅ |
| Grouping + sorting + aggregate theo nhóm | ✅ |
| Renderer PDF (QuestPDF, font Lato nhúng) | ✅ |
| Renderer XLSX (MiniExcel) / CSV | ✅ (bảng phẳng theo band Detail) |
| Page number `{Page}/{Pages}` | ✅ |

## Đã hỗ trợ (P1 — Designer UI)

Toàn bộ đã kiểm thử bằng browser tự động (Playwright + Chrome headless), có assertion.

### Bước 1 — khung designer

| Thành phần | Trạng thái |
|---|---|
| Cây cấu trúc band → item | ✅ |
| Canvas giấy (đúng khổ mm), band + item định vị tuyệt đối | ✅ |
| Chọn band/item (trên cây và trên canvas) | ✅ |
| Property panel: X/Y/W/H, nội dung/Binding/Expression/Format | ✅ |
| Style: cỡ chữ, đậm/nghiêng/khung, căn lề, màu | ✅ |
| Thêm/xoá item (Text, Field, Expression, Line, Số trang) | ✅ |
| Khổ giấy: A4/A5/A6/Letter/Custom + Portrait/Landscape | ✅ |
| Lưu JSON (tải file) & Mở JSON (nạp file) | ✅ |
| Xem PDF (render lại từ definition đang sửa, xem ngay trong trang) | ✅ |

### Bước 2 — thao tác trực tiếp trên canvas

| Thành phần | Trạng thái |
|---|---|
| **Kéo-thả** item để đổi vị trí (mm, mượt — JS lo drag, không round-trip mỗi pointermove) | ✅ |
| **Resize** bằng 8 tay cầm (nw/n/ne/e/se/s/sw/w) | ✅ |
| **Thước mm** ngang/dọc (vạch 1/5/10 mm, nhãn mỗi 10 mm) | ✅ |
| **Lưới** nền giấy bật/tắt | ✅ |
| **Snap** theo bước 0.5/1/2/5/10 mm, bật/tắt nhanh trên toolbar | ✅ |
| **Undo/redo** (toolbar + Ctrl+Z / Ctrl+Y / Ctrl+Shift+Z), gộp thao tác sửa liên tiếp | ✅ |
| **Multi-select** (Ctrl/Shift + click trên canvas) | ✅ |
| **Di chuyển bằng bàn phím** (mũi tên ±1 mm, Shift ±10 mm) | ✅ |
| **Z-order**: lên đầu / lên / xuống / xuống cuối | ✅ |
| **Delete/Esc** xoá item đã chọn / bỏ chọn | ✅ |
| Vị trí/kích thước kéo-thả phản ánh vào property panel và file JSON | ✅ |

### Bước 3 — Mã vạch & QRCode

| Thành phần | Trạng thái |
|---|---|
| Item **Barcode** (Code128 mặc định; Code39/EAN13/EAN8/UPCA/ITF) | ✅ |
| Item **QRCode** (ECC L/M/Q/H) | ✅ |
| Vẽ **vector SVG** (sắc nét khi in, `.Svg()` của QuestPDF) — không cần thư viện ảnh | ✅ |
| Tuỳ chọn: hiện số đọc dưới barcode, vùng lặng (quiet zone) | ✅ |
| Nguồn giá trị: Binding → Expression → Text nội suy `{}` | ✅ |
| Xem trước mã ngay trên canvas designer (SVG thật) | ✅ |
| Panel thuộc tính: định dạng / ECC / hiện số / vùng lặng | ✅ |
| **Ctrl+D** nhân bản item đã chọn | ✅ |
| XLSX/CSV: xuất giá trị mã dạng chữ (không nhúng ảnh) | ✅ |

> Kiểm chứng: Demo tự **giải mã lại** mã sau khi mã hoá (encode → raster → decode) và so khớp —
> Code128, Code39, EAN13, QR (L/M/H, kể cả UTF-8 tiếng Việt) đều **OK**.

### Bước 4 — Hoàn thiện designer

| Thành phần | Trạng thái |
|---|---|
| Quản lý band: thêm (chọn loại còn thiếu), nhân bản, xoá, đổi thứ tự lên/xuống | ✅ |
| Clipboard: **Ctrl+C / Ctrl+X / Ctrl+V** (và nút Sao chép/Cắt/Dán); dán lệch 3 mm mỗi lần | ✅ |
| **Ctrl+A** chọn tất cả item trong band | ✅ |
| Căn chỉnh nhiều item: trái / giữa ngang / phải / trên / giữa dọc / dưới | ✅ |
| Rải đều (distribute) ngang/dọc cho ≥3 item | ✅ |
| Đường dóng (alignment guides) khi kéo: mép/giữa item khác + mép/giữa trang, bắt dính vector | ✅ |
| Công tắc **Dóng** bật/tắt đường dóng | ✅ |

> Kiểm thử Playwright (tất cả PASS): nhân bản/xoá/đổi thứ tự band; Ctrl+C/V/X; Ctrl+A;
> căn trái → mọi X bằng nhau; căn trên → mọi Y bằng nhau; rải ngang; kéo item tới giữa trang
> → hiện đường dóng dọc rồi tự xoá khi thả chuột.

### Bước 5 — Tham số (parameters)

| Thành phần | Trạng thái |
|---|---|
| Định nghĩa tham số: tên, nhãn, kiểu (String/Int/Decimal/Date/Bool), bắt buộc, giá trị mặc định | ✅ |
| Quản lý tham số trong designer: thêm / xoá / chọn để sửa (có hoàn tác) | ✅ |
| Panel **“Tham số khi chạy”**: ô nhập theo kiểu (text / số / **date picker** / checkbox) | ✅ |
| Giá trị mặc định tự áp ở tầng engine (`ReportParameterDefaults.Apply`) — render được cả khi không có UI | ✅ |
| **Xem PDF** dùng giá trị tham số đã nhập; cảnh báo khi thiếu tham số bắt buộc | ✅ |
| Tham số dùng trong **Field / Expression / nội suy `{}`** (tra theo dòng → field → tham số) | ✅ |

> Kiểm chứng: Demo in `Tiêu đề = PHIẾU THANH TOÁN` (ghi đè mặc định) ra `hoa-don-thamso.png`;
> `EvaluateText("TieuDe")` trả đúng giá trị; tham số ngày `TuNgay` ra `DateTime` và format `dd/MM/yyyy`.
> Playwright (20 check PASS): thêm/đổi tên/xoá tham số, Ctrl+Z hoàn tác, giá trị mặc định điền sẵn,
> preview bật iframe, chặn preview khi thiếu tham số bắt buộc, và date picker cho báo cáo nhóm.

### Bước 6 — Nhóm item & khoá vị trí

| Thành phần | Trạng thái |
|---|---|
| **Nhóm**: gán item vào nhóm (**Ctrl+G** / nút **Nhóm**) — click 1 item thuộc nhóm chọn cả nhóm | ✅ |
| **Bỏ nhóm**: **Ctrl+Shift+G** / nút **Bỏ nhóm** | ✅ |
| Kéo 1 item trong nhóm → **cả nhóm di chuyển cùng** (cùng delta) | ✅ |
| **Khoá vị trí** (thuộc tính từng item): không kéo / resize / nudge / xoá từ canvas | ✅ |
| Khoá/mở khoá nhanh: checkbox **Khoá vị trí** + nút **Khoá đã chọn** / **Mở khoá** | ✅ |
| Item khoá hiện **🔒**, ẩn tay cầm resize; nhãn **NHÓM** ở cây cấu trúc | ✅ |
| `Group` / `Locked` lưu trong JSON definition (làm tròn round-trip) | ✅ |

> Playwright (`designer.test10.cjs`, 17/17 PASS): gán cùng group id + badge **nhóm**; click item trong nhóm
> chọn cả nhóm; kéo nhóm → 2 item dịch chuyển đúng bằng nhau `d=(11,11)`; bỏ nhóm xoá id; sự kiện khoá →
> có `.locked`, ẩn tay cầm, **không kéo được**, bỏ qua nudge bằng mũi tên; **Mở khoá** rồi kéo lại bình thường;
> Ctrl+G / Ctrl+Shift+G nhóm–bỏ nhóm; Escape bỏ chọn; Ctrl+Z khôi phục cả nhóm lẫn vị trí.

### Bước 7 — Biểu đồ (Chart)

| Thành phần | Trạng thái |
|---|---|
| Item **Biểu đồ**: cột (bar) · đường (line) · tròn (pie) — vẽ **vector SVG tự dựng**, không cần thư viện vẽ | ✅ |
| Nguồn dữ liệu: **Category** × **Value** theo từng dòng detail; tổng hợp **Sum / Avg / Count / Min / Max** | ✅ |
| Tuỳ chọn: **Tiêu đề**, **chú giải** (không / phải / dưới), **hiện giá trị** trên cột/điểm | ✅ |
| Trục + lưới + nhãn trục (rút gọn K/M), nhãn xoay khi nhiều điểm; pie kèm % trong chú giải | ✅ |
| Designer xem trước bằng **dữ liệu mẫu**; **Xem PDF** dùng dữ liệu thật | ✅ |
| Xuất PDF sắc nét (QuestPDF render SVG); XLSX/CSV bỏ qua item biểu đồ | ✅ |

> Kiểm chứng: Demo `ChartSelfTest` — tổng hợp `Sum(LineTotal)` theo nhóm ra đúng
> `Gọng=3.440.000 · Tròng=3.400.000 · Phụ kiện=390.000`; sinh SVG bar/line/pie hợp lệ;
> render 3 loại ra `chart-bar.png` / `chart-line.png` / `chart-pie.png` (đã xem).
> Playwright (`designer.test11.cjs`, 13/13 PASS): thêm item Biểu đồ, cây hiện **Chart**, preview bar có `<rect>`,
> đổi **Loại** → line có `<polyline>`, pie có `<path>`, Ctrl+Z quay lại; báo cáo nhóm có biểu đồ trong ReportFooter
> và **Xem PDF** ra iframe (blob) với biểu đồ thật ở cuối báo cáo.

### Bước 8 — Bảng (Table)

| Thành phần | Trạng thái |
|---|---|
| Item **Bảng**: các cột cấu hình (Tiêu đề · Binding/Expression · Format · Canh lề · Rộng mm) | ✅ |
| Dữ liệu lấy theo **các dòng detail của band chứa item** (mỗi dòng detail = 1 hàng) | ✅ |
| **Dòng tổng** ở cuối theo từng cột: Sum / Avg / Count / Min / Max | ✅ |
| Tuỳ chọn: **Tiêu đề cột** (lặp khi sang trang), **viền**, **sọc xen kẽ**, **màu nền tiêu đề** | ✅ |
| Giới hạn số dòng (`MaxRows`) hoặc tự động theo **chiều cao ô** | ✅ |
| Designer: panel thêm/xoá/đổi thứ tự cột + sửa từng trường; canvas xem trước khung bảng | ✅ |
| Xuất PDF sắc nét (QuestPDF Table); XLSX/CSV bỏ qua item bảng | ✅ |

> Kiểm chứng: Demo `TableSelfTest` — ô dòng 1 `Name = "Gọng nhựa trẻ em"`; tổng `SL = 33`,
> tổng `Doanh thu = 7.230.000`, `avg = 1.032.857`; render `table-demo.png` (đã xem).
> Báo cáo mẫu mới **Top sản phẩm bán chạy** (`top-san-pham`) dùng item Bảng ở `ReportHeader`.
> Playwright (`designer.test12.cjs`, 14/14 PASS): thêm item **Bảng** (3 cột mặc định), cây hiện **Table**,
> panel hiện **Cột (3)** với 3 card cột; **Thêm cột** → 4, Ctrl+Z → 3; sửa **Tiêu đề** → preview đổi ngay;
> báo cáo `top-san-pham` có bảng trong `ReportHeader` và **Xem PDF** ra iframe (blob) với bảng thật.

### Bước 9 — Lớp tích hợp plug-in (ReportService)

| Thành phần | Trạng thái |
|---|---|
| `IReportStore` — lưu/tải định nghĩa; có sẵn **`InMemoryReportStore`** và **`FileReportStore`** (mỗi report 1 file JSON) | ✅ |
| `IReportDataProvider` — nguồn dữ liệu (kèm **`DelegateReportDataProvider`** bọc delegate) | ✅ |
| `IReportAuthorizer` — quyền `report:render` / `report:design` (kèm **`AllowAllReportAuthorizer`**) | ✅ |
| **`ReportService`** — điều phối: phân quyền → nạp định nghĩa → áp tham số → lấy dữ liệu → engine render | ✅ |
| Lỗi tích hợp: `ReportNotFoundException`, `ReportForbiddenException` | ✅ |
| Host dùng **`FileReportStore`** (`App_Data/reports`) + nút **“Lưu vào kho”** trên designer | ✅ |

> Kiểm chứng: Demo `IntegrationSelfTest` — `List()` 2 report; `Find` theo mã; `Render` qua service ra PDF;
> ghi đè tham số; mã sai → `ReportNotFoundException`; `DenyAllAuthorizer` chặn cả render lẫn lưu →
> `ReportForbiddenException`; `FileReportStore` ghi rồi đọc lại từ đĩa (khớp `Code`/`Name`).
> Playwright (`designer.test13.cjs`, 10/10 PASS): danh sách report đọc từ kho (3 báo cáo); cả 3 mở được;
> **Lưu vào kho** báo `Đã lưu “top-san-pham” vào kho.`; kho ghi 3 file JSON (file `top-san-pham.json`
> chứa `Table`/`Columns`); **Xem PDF** vẫn chạy qua `ReportService` (blob).
> (Sửa kèm: `MainLayout` — app bar trước đây đè lên thanh công cụ do `pa-3` dùng `padding !important`;
> nay chuyển sang `Style="padding: 60px 12px 12px 12px"`.)

### Bước 10 — Báo cáo con (Subreport)

| Thành phần | Trạng thái |
|---|---|
| `ItemKind.Subreport` + `SubreportSpec` (`ReportCode`, `InheritParameters`, `ShowBorder`) | ✅ |
| `ReportRenderOptions.SubreportResolver` + `SubreportResolution` (renderer không tự đụng tới kho) | ✅ |
| Renderer: vẽ nội dung report con ngay trong khung item (bỏ page header/footer), chặn đệ quy (tối đa 4 tầng) | ✅ |
| `ReportService` tự nối resolver từ **kho + nguồn dữ liệu + phân quyền** (kế thừa tham số report cha) | ✅ |
| Designer: công cụ **Subreport**, xem trước khối nét đứt, panel chọn báo cáo con (`AvailableReports`) | ✅ |
| Host: báo cáo mẫu **`bao-cao-tong-hop`** nhúng `top-san-pham`; nút **Xoá khỏi kho** (kèm hộp thoại xác nhận) | ✅ |

> Báo cáo con không tìm thấy / không có quyền → render **placeholder** (khung nét đứt + ghi chú) thay vì ném lỗi,
> nhờ vậy một báo cáo thiếu quyền không làm hỏng cả bản in.
> Bề rộng item con được **kẹp theo vùng khả dụng** (trừ lề/viền) để tránh QuestPDF báo xung đột kích thước.
>
> Kiểm chứng: Demo `SubreportSelfTest` — resolver gọi đúng 1 lần/lượt; PDF 28.6KB; thiếu báo cáo con →
> placeholder 17.4KB; `ReportService` tự nối resolver (28.6KB); thiếu quyền report con → placeholder.
> Playwright (`designer.test14.cjs`, 11/11 PASS): kho có 4 report; canvas hiện khối báo cáo con trỏ `top-san-pham`;
> thêm item mới; panel chọn báo cáo con liệt kê đủ; đổi mục tiêu → canvas cập nhật; xuất JSON chứa `Subreport`;
> **Xem PDF** render báo cáo con (blob); huỷ hộp thoại xoá → kho không đổi.

### Bước 11 — Định dạng có điều kiện

| Thành phần | Trạng thái |
|---|---|
| `ReportItem.VisibleWhen` — biểu thức hiển thị (sai → bỏ item khỏi bản in) | ✅ |
| `ReportItem.StyleRules` + `StyleRule` — luật đổi Đậm/Nghiêng/Cỡ/Màu chữ/Nền/Khung theo điều kiện | ✅ |
| Renderer áp luật theo từng dòng (scope detail), lỗi biểu thức → bỏ qua an toàn | ✅ |
| Nền (`Background`) cho item chữ (trước đây chỉ Rect dùng) | ✅ |
| Designer: mục **Định dạng có điều kiện** (ô *Hiện khi* + danh sách luật thêm/xoá), badge **ĐK** trên canvas | ✅ |
| Report mẫu `ban-hang-theo-nhom`: Qty ≥ 5 nền cam, thành tiền ≥ 1 triệu đỏ + đậm | ✅ |

> Luật áp **lần lượt**, luật sau ghi đè; thuộc tính `null` = giữ nguyên kiểu gốc.
> `VisibleWhen` lỗi cú pháp được **coi như hiện** để tránh mất nội dung ngoài ý muốn.
> Định dạng/ẩn hiện áp cho **PDF**; bản XLSX/CSV vẫn xuất phẳng theo cột (bỏ qua luật kiểu).
>
> Kiểm chứng: Demo `ConditionalFormatSelfTest` — `VisibleWhen "Qty > 2"` làm PDF nhỏ hơn baseline
> (21.350B → 20.685B), thêm luật màu đổi nội dung PDF (22.504B); ảnh `cond-format.png` cho thấy
> ẩn tên khi `Qty <= 2` và tô đỏ+đậm khi `Qty > 5`.
> Playwright (`designer.test15.cjs`, 10/10 PASS): canvas có badge ĐK; panel hiện luật mẫu
> `LineTotal >= 1000000`; **Thêm luật** (1 → 2); đặt *Hiện khi* = `Qty >= 3`; xuất JSON giữ `StyleRules`
> + `VisibleWhen`; **Xem PDF** render đúng định dạng điều kiện.

### Bước 12 — Ảnh / Logo

| Thành phần | Trạng thái |
|---|---|
| Renderer vẽ `ItemKind.Image`: **SVG** (véc-tơ qua `.Svg()`) và **PNG/JPEG** (raster qua `.Image()`) | ✅ |
| Nguồn ảnh: **data URI** (base64 hoặc utf8) hoặc **đường dẫn tệp**; ảnh vừa khung, giữ tỷ lệ | ✅ |
| Nguồn lỗi (tệp không tồn tại / base64 hỏng) → bỏ qua an toàn, không làm hỏng trang | ✅ |
| Designer: nút **Ảnh**, ô *Nguồn ảnh*, **tải tệp từ máy** (tự nhúng base64), xem trước | ✅ |
| Canvas hiển thị chính ảnh (data URI) hoặc huy hiệu 🖼 + tên tệp | ✅ |
| Report mẫu `bao-cao-tong-hop`: logo EbizOp (SVG) ở góc phải PageHeader | ✅ |

> Ảnh nhúng thẳng vào definition dưới dạng data URI → JSON tự chứa, không phụ thuộc tệp ngoài khi
> chuyển máy. Tải tệp giới hạn 6 MB. XLSX/CSV **bỏ qua** item ảnh (giữ xuất phẳng theo cột).
>
> Kiểm chứng: Demo `ImageSelfTest` — có ảnh 49.418B vs không ảnh 16.023B (ảnh được vẽ thật),
> nguồn là đường dẫn tệp vẫn render, nguồn lỗi bị bỏ qua; ảnh `image-demo.png` cho thấy logo SVG
> (kính + “EbizOp”), PNG nhúng (thu nhỏ hóa đơn) và dòng “nguồn lỗi → bỏ qua”.
> Playwright (`designer.test16.cjs`, 11/11 PASS): canvas hiện logo mẫu; cây cấu trúc có item Image;
> panel có *Nguồn ảnh* + xem trước; nút **Ảnh** thêm item (1 → 2); dán data URI → item hiện ảnh;
> xuất JSON có `Kind=Image` + `Src` data URI; **Xem PDF** render bản có ảnh.

### Bước 13 — Sắp xếp & nhóm dữ liệu

| Thành phần | Trạng thái |
|---|---|
| `ReportDefinition.Sorting` — trước đây khai báo nhưng **không được dùng**, nay áp thật khi chia dòng | ✅ |
| Nhóm theo nhiều field (`Grouping`), mỗi nhóm giữ thứ tự theo `Sorting` | ✅ |
| Sắp xếp **ổn định** (giữ thứ tự gốc khi khoá bằng nhau), hỗ trợ số / ngày / chuỗi | ✅ |
| XLSX/CSV cũng nhận `Sorting` + `Grouping` (dùng chung `ReportGrouping.Build`) | ✅ |
| Designer: panel trái **Sắp xếp** (trường + chiều, thêm/xoá/đổi thứ tự) | ✅ |
| Designer: panel trái **Nhóm dữ liệu** (trường + chiều + *Lặp tiêu đề nhóm*, thêm/xoá/đổi thứ tự) | ✅ |

> Thứ tự áp dụng: nhóm theo `Grouping` (các nhóm liền nhau), rồi **trong mỗi nhóm** sắp theo `Sorting`.
> Không có `Grouping` → sắp toàn bộ dòng theo `Sorting`. Field không tồn tại → coi như null (lên đầu).
>
> Kiểm chứng: Demo `SortingSelfTest` — 3 nhóm `Gọng, Phụ kiện, Tròng`; trong mỗi nhóm `LineTotal`
> giảm dần; đủ 7 dòng không mất/lặp; ảnh `sort-group.png` + `sort-group.pdf`.
> Playwright (`designer.test17.cjs`, 15/15 PASS): mẫu hiện *Nhóm dữ liệu (1)* = `GroupName`;
> **+ Sắp xếp** thêm dòng, điền `LineTotal`, chọn *Giảm*; **+ Nhóm** thêm nhóm `Name`;
> JSON xuất có `Sorting=[{LineTotal, desc}]` + `Grouping=[GroupName, Name]`; **Xem PDF** render đúng.

### Bước 14 — Trường dữ liệu (Data panel)

| Thành phần | Trạng thái |
|---|---|
| `ReportFieldCatalog.Discover` — hợp nhất khoá của **mọi** dòng detail + field đơn + tham số | ✅ |
| Mỗi trường có **loại** (dòng / đơn / TS) và **giá trị mẫu** để gợi ý khi thiết kế | ✅ |
| Designer: panel trái **Trường dữ liệu (n)** — chip theo loại, có **ô tìm kiếm** | ✅ |
| Bấm chip → thêm item `Field` bind đúng trường vào band đang chọn | ✅ |
| **Kéo-thả** chip vào band (HTML5 DnD) → thêm item vào đúng band đó | ✅ |
| Chưa chọn band → tự dùng band `Detail`; item đặt ở vị trí trống kế tiếp | ✅ |

> Trường lấy từ `IReportDataProvider` của host (mẫu hoá qua `SampleReportDataProvider`), nên khi
> ghép EbizOp chỉ cần thay provider — designer không phụ thuộc tầng dữ liệu. Chip có màu theo loại
> (dòng / đơn / TS) và tooltip hiện `tên = giá trị mẫu`.
>
> Kiểm chứng: Demo `FieldCatalogSelfTest` — 7 trường `GroupName, Name, Qty, LineTotal (dòng)`,
> `StoreName (đơn)`, `TuNgay, TuNgay2 (TS)`; `LineTotal` chỉ có ở dòng 2 vẫn được hợp nhất;
> không trùng tên; giá trị mẫu `LineTotal = "640.000"`.
> Playwright (`designer.test18.cjs`, 16/16 PASS): panel hiện 6 trường; lọc "Line" → còn `LineTotal`;
> bấm chip `LineTotal` → thêm item; **kéo** chip `Qty` vào `Detail` → thêm item; JSON xuất có
> `Field(Binding=LineTotal)` + `Field(Binding=Qty, band=Detail)`.

### Bước 15 — Xem trước WYSIWYG (nhiều trang)

| Thành phần | Trạng thái |
|---|---|
| `QuestPdfReportRenderer.RenderPagesPng` — render **tất cả trang** ra ảnh PNG | ✅ |
| `ReportService.RenderPagesPng` — tái dùng phân quyền + tham số + dữ liệu + resolver báo cáo con | ✅ |
| Designer: nút **Xem trước** mở overlay hiển thị từng trang ở dạng ảnh (pixel-accurate) | ✅ |
| Điều hướng trang: **đầu / trước / sau / cuối** + chỉ số `Trang x/n` | ✅ |
| **Zoom** −/+ (25%–400%), nút **100%**, nút **Vừa khổ** (đo bề rộng khung) | ✅ |
| Phím tắt: `←/→` chuyển trang, `+/−` zoom, `Esc` đóng | ✅ |

> Không cần pdf.js: dùng chính engine QuestPDF render **ảnh từng trang** → WYSIWYG đúng 100% với bản in,
> bề rộng trang suy từ khổ giấy (mm → px) nên zoom chính xác và không phụ thuộc trình duyệt.
> Giữ lại nút **Xem PDF** (iframe, có thanh công cụ của Chrome) cho bản véc-tơ/in thử.
>
> Kiểm chứng: Demo `PngPagesSelfTest` — report 150 dòng → **4 trang**, report ngắn → **1 trang**,
> ảnh có chữ ký PNG hợp lệ (`preview-page1.png`, `preview-page2.png`).
> Playwright (`designer.test19.cjs`, 13/13 PASS): mở overlay; ảnh là PNG data URI; `Trang 1/1`;
> bề rộng ≈1122px (A4 ngang); zoom in → 125% và ảnh rộng hơn; **100%**/**Vừa khổ** đúng; nút trang bị
> khoá khi 1 trang; `Esc` và nút **X** đều đóng.

### Bước 16 — Bảng con theo nhóm (Item Table lồng trong GroupHeader)

| Thành phần | Trạng thái |
|---|---|
| `TableSpec.Scope` — `"group"` (mặc định, dòng của nhóm hiện tại) / `"all"` (toàn bộ dòng) | ✅ |
| `TableHelper.RowsFor` — chọn bộ dòng theo phạm vi, dùng chung cho render & designer | ✅ |
| Đặt item **Table** trong `GroupHeader`/`GroupFooter` → mỗi nhóm có bảng con riêng | ✅ |
| Bảng trong **Detail** chỉ vẽ **1 lần/nhóm** (không lặp theo từng dòng) | ✅ |
| Designer: nút **Bảng nhóm** — tự tạo/chọn `GroupHeader`, thêm bảng, nới chiều cao band | ✅ |
| Panel phải: combobox **Phạm vi dữ liệu** (`Nhóm hiện tại` / `Toàn bộ dòng`) | ✅ |

> Nhờ `Scope`, cùng một item Table dùng được ở 3 chỗ: `ReportHeader` = bảng toàn bộ báo cáo,
> `GroupHeader/GroupFooter` = bảng con của từng nhóm, `Detail` = bảng của nhóm vẽ một lần.
>
> Kiểm chứng: Demo `GroupTableSelfTest` — 3 nhóm (3, 2, 2 dòng); `Scope=group` → 3 dòng nhóm đầu,
> `Scope=all` → 7 dòng; bảng trong Detail **không lặp** (Δdetail ≈ Δheader); xuất `group-table.png`.
> Playwright (`designer.test20.cjs`, 13/13 PASS): bấm **Bảng nhóm** → `GroupHeader` thêm 1 Table
> (`Scope=group`, 3 cột, có dòng tổng); band được nới cao 57mm; đổi phạm vi → JSON `Scope=all`;
> canvas hiển thị khối bảng.

### Bước 17 — Sao lưu / khôi phục & nhân bản report

| Thành phần | Trạng thái |
|---|---|
| `ReportBackup` — gói sao lưu **một file JSON**: `SchemaVersion`, `ExportedAt`, `Reports[]` | ✅ |
| `ReportService.ExportBackup()` — xuất toàn bộ kho | ✅ |
| `ReportService.ImportBackup(json, overwrite)` — khôi phục; trả `ReportImportResult` (thêm/cập nhật/bỏ qua/lỗi) | ✅ |
| `ReportService.Clone(code, newCode?, newName?)` — nhân bản sâu, tự tránh trùng mã (`-copy`, `-copy-2`…) | ✅ |
| Host: nút **Nhân bản** (hỏi mã, lưu bản gốc rồi chuyển sang bản sao) | ✅ |
| Host: nút **Sao lưu kho** tải `reports-backup-*.json`; **Khôi phục** đọc file, hỏi ghi đè | ✅ |
| Chống lỗi: từ chối JSON hỏng / schema mới hơn; mục lỗi không chặn các mục khác | ✅ |

> Khôi phục mặc định **bỏ qua** mã đã có; chọn *ghi đè* thì thay nội dung. Bản sao là **sao chép sâu**
> (sửa bản sao không ảnh hưởng bản gốc) và đặt `Version = 1`. Toàn bộ nằm ở tầng service nên dùng được
> từ Blazor, Web API hay job mà không phụ thuộc UI.
>
> Kiểm chứng: Demo `BackupRestoreSelfTest` — xuất 2 báo cáo; khôi phục vào kho rỗng (thêm 2); khôi phục
> lại (bỏ qua 2); ghi đè khôi phục nội dung gốc; nhân bản → `bc-a-copy`, `bc-a-copy-2`, tên “(bản sao)”,
> `Version=1`, sao chép sâu; từ chối schema v99 / JSON hỏng / mã sai.
> Playwright (`designer.test21.cjs`, 17/17 PASS): nhân bản → URL `/designer/bc-clone-test`; sao lưu tải
> JSON (schema 1, ≥5 báo cáo, có bản sao); khôi phục thêm báo cáo mới (“Thêm 1, cập nhật 5”); mở được
> báo cáo vừa nhập; sao lưu lại → kho có 6 báo cáo.

### Bước 18 — Expression builder có xem trước

| Thành phần | Trạng thái |
|---|---|
| `ExpressionInspector` — danh mục **18 hàm** (`Name/Signature/Description/Snippet/MinArgs/MaxArgs`) | ✅ |
| `ExpressionInspector.Validate` — kiểm tra cú pháp **và** tên hàm/số đối số | ✅ |
| `ExpressionInspector.Evaluate` — tính thử trên dữ liệu (mặc định dùng dòng đầu) | ✅ |
| `ExpressionInspector.SampleData` — dựng `ReportData` mẫu từ trường + tham số mặc định | ✅ |
| Component `ExpressionBuilder` — ô nhập + **Kiểm tra** + **Xem kết quả** + chip chèn hàm/trường | ✅ |
| Chip hàm (18) và chip trường/tham số (theo `Category`: detail/field/param) | ✅ |
| Áp dụng cho: `Expression` item, `VisibleWhen`, `StyleRule.When`, `TableColumn.Expression` | ✅ |
| `ReportDesigner.PreviewExpression` — host xem trước trên dữ liệu thật (kèm tham số hiệu dụng) | ✅ |

> Ô biểu thức dùng lại chính `ExpressionCompiler` (không eval mã động). Sai cú pháp → báo ngay vị trí;
> gọi hàm lạ/sai số đối số → báo tên hàm; xem kết quả hiển thị `= <giá trị đã format>` (số theo vi-VN).
> Nếu host cung cấp `PreviewExpression` thì tính trên dữ liệu thật của report; nếu không, builder tự dựng
> dữ liệu mẫu từ panel “Trường dữ liệu” + giá trị mặc định của tham số.
>
> Kiểm chứng: Demo `ExpressionBuilderSelfTest` (30 mục — danh mục hàm, `Validate` đúng/sai, `Sum/Avg/Count/`
> `Min/Max/Iif/Upper/Round/Format/Len`, dữ liệu mẫu vi-VN + ghép dòng/field/param).
> Playwright (`designer.test22.cjs`, 14/14 PASS): chọn item Expression → có builder; **Xem kết quả**
> `Sum(LineTotal)` = `7.230.000`; **Kiểm tra** hợp lệ/lỗi; chip `Count()` chèn đúng; chip trường nối vào biểu
> thức; xuất JSON chứa biểu thức vừa nhập.

### Bước 19 — Phân trang & keep-together

| Thành phần | Trạng thái |
|---|---|
| `ReportGroup.KeepTogether` — giữ **cả nhóm** (header + dòng + footer) trên một trang nếu vừa | ✅ |
| `ReportBand.PageBreakBefore` — ép sang trang mới trước khi vẽ band | ✅ |
| Renderer dùng `PreventPageBreak()` / `PageBreak()` của QuestPDF, tách hàm `RenderBandItem` | ✅ |
| Designer: checkbox **Giữ nhóm trên một trang** (panel Nhóm) | ✅ |
| Designer: checkbox **Ngắt trang trước band** (panel Band) | ✅ |

> Keep-together chỉ áp dụng khi báo cáo **có khai báo nhóm** (tránh biến cả báo cáo thành một khối).
> Nhóm không vừa phần còn lại của trang sẽ được đẩy nguyên sang trang kế; nếu nhóm dài hơn một trang
> thì vẫn cho phép ngắt bình thường (theo ngữ nghĩa `PreventPageBreak` của QuestPDF).
>
> Kiểm chứng: Demo `PaginationSelfTest` — 5 nhóm × 100 mm: không giữ → **2 trang**, giữ nhóm → **3 trang**
> (nhóm thứ 3 không vừa được đẩy sang trang mới); `PageBreakBefore` trên ReportFooter thêm đúng **1 trang**.
> Playwright (`designer.test23.cjs`, 13/13 PASS): mặc định 2 cờ `false`; bật checkbox → JSON
> `KeepTogether=true` và `PageBreakBefore=true`; xem trước WYSIWYG vẫn render (Trang 1/2).

### Bước 20 — Nhật ký thao tác (audit)

| Thành phần | Trạng thái |
|---|---|
| `ReportAuditEntry` — thời gian, thao tác, mã, phiên bản, người dùng, chi tiết | ✅ |
| `IReportAuditStore` — cổng ghi nhật ký (thay bằng DB/Serilog khi tích hợp) | ✅ |
| `InMemoryReportAuditStore` — thread-safe, giới hạn sức chứa, trả “mới nhất trước” | ✅ |
| `ReportService` ghi nhật ký: `save` · `delete` · `clone` · `import` · `render` · `preview` | ✅ |
| Host: nút **Nhật ký** mở hộp thoại bảng thao tác gần đây | ✅ |
| Lỗi nơi lưu nhật ký **không** chặn thao tác chính | ✅ |

> Nhật ký là **hook pluggable**: mặc định in-memory cho demo; khi tích hợp EbizOp chỉ cần đăng ký một
> `IReportAuditStore` khác (bảng `report_runs`/audit trail) mà không sửa `ReportService`.
>
> Kiểm chứng: Demo `AuditSelfTest` — `save > clone > render > delete` đúng thứ tự mới nhất trước, có mã +
> người dùng; `clone` ghi “nguồn → đích”; `import` ghi tóm tắt “thêm/cập nhật/bỏ qua/lỗi”; kho nhật ký cắt
> bớt theo sức chứa và giữ mục mới nhất.
> Playwright (`designer.test24.cjs`, 8/8 PASS): nhật ký ban đầu trống → xem trước WYSIWYG → mở lại thấy
> dòng `preview · ban-hang-theo-nhom · 1 · PC · 1 trang`.

### Bước 21 — Nguồn dữ liệu SQL (SQLite + SQL Server) & tạo báo cáo mới

| Thành phần | Trạng thái |
|---|---|
| `EbizReports.Data` — project riêng, ADO.NET thuần (`Microsoft.Data.Sqlite` + `Microsoft.Data.SqlClient`) | ✅ |
| `SqlConnectionRegistry` / `SqlConnectionOptions` — host đăng ký kết nối; report chỉ tham chiếu **theo tên** (không lộ chuỗi kết nối vào JSON) | ✅ |
| `SqlDataSourceExecutor` — `Probe` (chạy thử) + `Execute` (lấy dữ liệu); bind `@ThamSo` trong SQL từ tham số báo cáo | ✅ |
| `SqlReportDataProvider : IReportDataProvider` — có `sql` thì chạy truy vấn, ngược lại **fallback** nguồn mẫu | ✅ |
| Model: `ReportDataSource.Connection`, `.Sql`, `Type = "service" \| "sql"` | ✅ |
| Designer: panel **Nguồn dữ liệu** (thêm/xoá, chọn kết nối, nhập SQL, nút **Chạy thử**) | ✅ |
| Cột trả về tự đổ vào panel **Trường dữ liệu** để kéo vào band | ✅ |
| CSDL demo SQLite `App_Data/ebizop-demo.db` (bảng `PhieuBanHang`) tự tạo + seed; tuỳ chọn SQL Server qua `Reports:SqlServer:ConnectionString` | ✅ |
| Nút **Tạo báo cáo mới** (hộp thoại) sinh bộ khung “Danh sách phiếu bán hàng” + tham số Từ/Đến ngày | ✅ |

> Kết nối CSDL do **host** giữ; định nghĩa báo cáo chỉ lưu `Connection` (tên) + `Sql`. Khi tích hợp EbizOp,
> đăng ký `SqlConnectionOptions` trỏ vào DB thật — không cần đổi model hay renderer.
>
> Kiểm chứng: Demo `SqlDataSourceSelfTest` — lọc theo `@TuNgay`/`@DenNgay` (2/5 dòng), `ORDER BY`, ánh xạ cột →
> field, “Chạy thử” trả cột + dòng mẫu, SQL sai trả lỗi (không ném ngoại lệ), report không có `sql` dùng nguồn
> dự phòng, render PDF từ dữ liệu SQL (→ `out/sql-datasource.pdf`). Playwright (`flow.test25.cjs`, 16/16 PASS):
> tạo báo cáo mới → thêm nguồn → chọn `Demo SQLite` → nhập SQL → Chạy thử (13 dòng · 6 cột) → cột vào panel →
> xem trước (Trang 1/1) → lưu kho.
>
> Video hướng dẫn: `out/huong-dan-nguon-du-lieu-sql.mp4` (quay bằng Playwright `recordVideo`, chuyển WebM→MP4
> bằng ffmpeg) minh hoạ toàn bộ luồng tạo báo cáo với tham số ngày và nguồn dữ liệu SQL.

### Bước 22 — Giao diện giống DevExpress Reports (Property Grid · Toolbox · 3 panel · Thước & Zoom · Menu chuột phải · Ribbon)

| Thành phần | Trạng thái |
|---|---|
| **Property Grid** (kiểu DevExpress) ở panel phải: ô **tìm kiếm** + các **nhóm gấp/mở** (Báo cáo, Band, Bố cục, Dữ liệu, Giao diện, Hành vi) | ✅ |
| **Toolbox** — lưới **icon** 12 điều khiển: Text, Field, Expr, Line, Số trang, Barcode, QRCode, Biểu đồ, Bảng, Bảng nhóm, Ảnh, Subreport | ✅ |
| **Bố cục 3 panel** — Trái: Hộp công cụ + Report Explorer (Cấu trúc) + Band; Giữa: canvas + thước; Phải: tab **Trường dữ liệu** / **Thuộc tính** | ✅ |
| **Band header + kéo giãn chiều cao** — header hiển thị loại band + chiều cao (mm); kéo tay ở đáy band để đổi chiều cao (có snap) | ✅ |
| **Thước mm + Zoom 50–200%** — thanh zoom góc dưới phải: `−`/`+`, nhãn %, preset 50/75/100/125/150/200%, **Vừa khung**; thước ngang/dọc và lưới phóng theo, kéo/thả & kéo giãn band vẫn quy đổi đúng mm | ✅ |
| **Menu chuột phải** — Cắt · Sao chép · Dán · Nhân bản · Xoá (kèm phím tắt); **Căn chỉnh** (trái/giữa/phải/trên/giữa↕/dưới), **Phân bố** ngang/dọc, **Nhóm/Bỏ nhóm**, **Khoá/Mở khoá vị trí**; tự chọn item được chuột phải | ✅ |
| **Ribbon dạng tab** — 3 tab **Trang chủ / Bố cục / Định dạng**: *Trang chủ* (Hoàn tác/Làm lại, Lưu–Mở JSON, Xem PDF, Xem trước), *Bố cục* (Snap + bước, Lưới, Dóng, Vừa khung), *Định dạng* (Đậm/Nghiêng/Khung, Cỡ chữ, Canh lề, Màu chữ — áp cho 1 hoặc nhiều item đang chọn) | ✅ |

> Kiểm chứng: Playwright `designer.test28.cjs` (14/14 PASS) — tab phải & chuyển tab, thêm Field từ tab,
> lưới thuộc tính có nhóm **Band**, kéo tay đổi chiều cao band (10 → 22 mm) và đồng bộ với lưới thuộc tính;
> `designer.test29.cjs` (18/18 PASS) — thanh zoom, preset 150% (trang 1.5×, thước/lưới phóng theo), nút `−`/`+`,
> **Vừa khung**, kéo giãn band ở 150% quy đổi đúng mm (Δ15 mm); `designer.test30.cjs` (26/26 PASS) — menu
> chuột phải (chọn item được chuột phải, Cắt/Sao chép/Dán/Nhân bản/Xoá, phím tắt), **Căn chỉnh** (Canh trên →
> cùng Y), **Phân bố ngang** (gaps −24/40 mm → 8/8 mm, giữ nguyên độ rộng), **Khoá/Mở khoá**, Esc đóng menu;
> `designer.test31.cjs` (24/24 PASS) — ribbon 3 tab, tab mặc định **Trang chủ**, **Bố cục** có Snap/Lưới/Dóng +
> Vừa khung, **Định dạng** áp Đậm/Nghiêng/Canh lề/Cỡ chữ/Màu chữ và đồng bộ với Property Grid (kể cả khi
> chọn nhiều item bằng `Ctrl+A`).
> Ảnh: `out/dx-03-layout-band.png`, `out/dx-04-zoom.png`, `out/dx-05-context-menu.png`,
> `out/dx-06-ribbon-home.png`, `out/dx-06-ribbon-format.png` (kèm `dx-01-property-grid.png`, `dx-02-toolbox.png`).

### Bước 23 — 3 tab cấp cao: Thông số · Thiết kế mẫu · Xem trước

Tách phần **dữ liệu/tham số/SQL** ra khỏi phần **vẽ giao diện** (giống cửa sổ MAU BAO CAO của EbizOp):

| Tab | Nội dung |
|---|---|
| **Thông số** | **Thông tin báo cáo**: Mã · Nhóm · Tên báo cáo · Diễn giải + 3 cờ (*Là mẫu của hệ thống*, *Lưu tại máy client*, *Dùng để in lưới số liệu*); **Các tham số cho báo cáo** dạng lưới (TT/R, Tham số, Tên tham số, Kiểu DL, Quyền, Câu SQL cho combobox, Field giá trị, Field hiển thị, Các Fields ẩn, Mặc định); **Nguồn dữ liệu** + 2 tab con **Câu SQL dùng báo cáo** / **Câu SQL dùng để thiết kế** (nút *Chạy thử*); **Sắp xếp**, **Nhóm dữ liệu** |
| **Thiết kế mẫu** | Toàn bộ phần vẽ: ribbon (Trang chủ/Bố cục/Định dạng), Hộp công cụ + Cấu trúc (band) + Band, canvas + thước mm + zoom, tab phải **Trường dữ liệu** / **Thuộc tính** (Property Grid) |
| **Xem trước** | Render WYSIWYG nhiều trang (PNG) ngay trong tab: chuyển trang, zoom, **Vừa khổ**, **Tạo xem trước**. Nút *Xem trước* trên ribbon vẫn mở overlay như trước |

Model bổ sung: `ReportDefinition.Group/Description/IsSystem/SaveToClient/ForGridPrint`;
`ReportParameter.Permission/ComboBoxSql/ValueField/DisplayField/HiddenFields`;
`ReportDataSource.DesignSql`. Tab mặc định khi mở designer = **Thiết kế mẫu**.

> Kiểm chứng: Playwright `designer.test32.cjs` (29/29 PASS) — 3 tab cấp cao, tab mặc định, metadata, lưới tham số
> (nhập Quyền, đổi tên, tick R, thêm/xoá dòng), nguồn dữ liệu + 2 tab con SQL, tab Xem trước render ảnh.
> Các bộ cũ vẫn xanh sau khi cập nhật luồng (`designer.test[11,12,14,15,16,18,20,23,26,27,28,29,30,31].cjs`,
> `flow.test25.cjs`). Ảnh: `out/dx-07-tab-thongso.png`, `out/dx-07-tab-preview.png`.

## Chưa làm (P3 — cần source EbizOp)

- Adapter dữ liệu thật (`IReportDataSource` → `ReportService`/`OpDbContext`)
- Trang `/settings/report-designer` + phân quyền `report:design` + bảng DB `report_definitions`, `report_runs`
- Template mặc định (hóa đơn, tem barcode, phiếu nhập/thu-chi, doanh thu)
- In qua Print Agent; gắn `IReportAuditStore` vào DB/audit trail thật

## Ghi chú kỹ thuật

- **Font**: Lato (`Lato-Regular/Bold/Italic/BoldItalic/Medium/SemiBold.ttf`) được nhúng vào `EbizReports.Rendering.dll` từ thư mục `LatoFont\` của EbizOp → PDF tiếng Việt đúng dấu, không phụ thuộc máy.
- **Toạ độ**: mọi X/Y/W/H tính bằng **mm**, đặt tuyệt đối trong band (dựng bằng QuestPDF `Layers`).
- **QuestPDF**: dùng license Community (miễn phí khi tổ chức có doanh thu < 1 triệu USD/năm — cần xác nhận).
- **Mã vạch/QR**: mã hoá bằng **ZXing.Net** (thuần managed, Apache-2.0) nhưng **vẽ vector SVG** tự sinh → không kéo theo System.Drawing/SkiaSharp/ảnh; PDF sắc nét, in tem tốt. Demo tự giải mã lại để kiểm chứng.
- **Build**: máy hiện có SDK 8/11, không có SDK 9; lib đa target `net8.0;net9.0` build tốt bằng SDK 11. Demo chạy `net8.0`.
