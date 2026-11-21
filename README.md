# 🚂 Hệ Thống Bán Vé Tàu Hỏa Trực Tuyến

> Đồ án môn Lập trình Web — ASP.NET Core MVC 8 + ADO.NET thuần (không ORM/EF), cơ sở dữ liệu SQL Server.

Ứng dụng web bán vé tàu hỏa: tìm chuyến, chọn ghế trên sơ đồ toa, giữ chỗ 15 phút, áp mã giảm giá, thanh toán QR, hủy vé có tính phí theo thời gian, cùng hệ quản trị đầy đủ cho nhà xe.

**🌐 Demo trực tuyến:** <http://tickettrain-shop.somee.com/>

---

## 📋 Mục lục

- [Tính năng](#-tính-năng)
- [Công nghệ sử dụng](#-công-nghệ-sử-dụng)
- [Yêu cầu hệ thống](#-yêu-cầu-hệ-thống)
- [Cài đặt và chạy](#-cài-đặt-và-chạy)
- [Tài khoản đăng nhập](#-tài-khoản-đăng-nhập)
- [Hướng dẫn sử dụng — Khách hàng](#-hướng-dẫn-sử-dụng--khách-hàng)
- [Hướng dẫn sử dụng — Quản trị viên](#-hướng-dẫn-sử-dụng--quản-trị-viên)
- [Quy tắc nghiệp vụ](#-quy-tắc-nghiệp-vụ)
- [Cấu trúc dự án](#-cấu-trúc-dự-án)
- [Cơ sở dữ liệu](#-cơ-sở-dữ-liệu)
- [Triển khai lên hosting](#-triển-khai-lên-hosting)

---

## ✨ Tính năng

### Dành cho khách hàng

| Chức năng | Mô tả |
|---|---|
| 🔍 Tìm chuyến | Chọn ga đi, ga đến, ngày khởi hành, số lượng hành khách; hỗ trợ vé một chiều và khứ hồi |
| 💺 Chọn ghế | Sơ đồ từng toa với 3 trạng thái: trống / đang giữ / đã bán |
| ⏱️ Giữ chỗ 15 phút | Ghế được khóa tạm trong 15 phút; hết hạn tự động nhả |
| 👥 Khai báo hành khách | Họ tên, giấy tờ (CCCD/mã SV), đối tượng ưu đãi |
| 🎟️ Mã giảm giá | Áp dụng mã coupon trước khi thanh toán |
| 💳 Thanh toán | VietQR / MoMo / chuyển khoản ngân hàng |
| 📱 Vé điện tử | Mã QR hiển thị trên web để soát vé |
| 🧾 Quản lý đơn | Xem danh sách đơn, chi tiết đơn, xem vé |
| ↩️ Hủy vé | Tính phí tự động theo thời gian còn lại tới giờ tàu chạy |
| 🔔 Thông báo | Nhận thông báo real-time khi chuyến bị hoãn/hủy |
| 👤 Tài khoản | Đăng ký, đăng nhập, đổi mật khẩu, cập nhật hồ sơ |

### Dành cho quản trị viên

16 trang quản trị: Ga, Tuyến, Chi tiết tuyến, Tàu, Toa, Chuyến, Chi tiết chuyến, Giá vé, Đối tượng ưu đãi, Đơn đặt vé, Hoàn tiền, Mã giảm giá, Người dùng, Quỹ, Thông báo.

| Chức năng | Mô tả |
|---|---|
| 📍 Quản lý ga | Thêm/sửa ga, tỉnh thành, trạng thái hoạt động |
| 🛤️ Quản lý tuyến | Tuyến + danh sách ga dừng kèm khoảng cách |
| 🚆 Quản lý tàu | Tàu gắn với tuyến; cấu hình số toa và loại toa |
| 📅 Quản lý chuyến | Tạo chuyến đơn lẻ hoặc tạo hàng loạt theo khoảng ngày |
| 💺 Quản lý ghế | Sơ đồ toa, loại toa (ghế mềm, giường K4, giường K6) |
| 💰 Quản lý giá | Bảng giá theo cặp ga + loại toa |
| 🎫 Quản lý đơn | Lọc theo trạng thái, xem chi tiết đơn |
| 💸 Duyệt hoàn tiền | Duyệt/từ chối yêu cầu hủy vé của khách |
| 🏷️ Mã giảm giá | Tạo mã %, số tiền, bật/tắt, giới hạn lượt dùng |
| 👥 Người dùng | Khóa/mở khóa tài khoản |
| 📊 Quỹ | Doanh thu thật = tiền thu − tiền đã hoàn |
| ⚠️ Hoãn chuyến | Hoãn X phút, ghi chú lý do; khách thấy badge cảnh báo |
| 🛑 Hủy chuyến | Hoàn 100% + tặng mã xin lỗi cho khách bị ảnh hưởng |

---

## 🛠 Công nghệ sử dụng

| Thành phần | Công nghệ |
|---|---|
| Framework | ASP.NET Core MVC `net8.0` |
| Truy cập dữ liệu | **ADO.NET thuần** — `Microsoft.Data.SqlClient` 5.2.2, không ORM/Entity Framework |
| Cơ sở dữ liệu | Microsoft SQL Server (LocalDB hoặc SQL Server host) |
| Xác thực | Cookie Authentication + BCrypt (`BCrypt.Net-Next` 4.0.3) |
| Mã QR | `QRCoder` 1.6.0 |
| Real-time | SignalR (thông báo hoãn/hủy chuyến) |
| View | Razor `.cshtml`, Bootstrap 5 |
| Kiến trúc | Controller → Service → Repository → Database |

---

## 💻 Yêu cầu hệ thống

- **.NET SDK 8.0** trở lên ([tải tại đây](https://dotnet.microsoft.com/download/dotnet/8.0))
- **SQL Server** — một trong các lựa chọn:
  - SQL Server LocalDB (đi kèm Visual Studio / SQL Server Express)
  - SQL Server Express / Developer / Standard
- **Visual Studio 2022** (khuyến nghị) hoặc VS Code
- `sqlcmd` hoặc SQL Server Management Studio (SSMS) để chạy script

Kiểm tra môi trường:

```bash
dotnet --version        # phải >= 8.0
sqllocaldb info         # phải thấy MSSQLLocalDB
```

---

## 🚀 Cài đặt và chạy

### Bước 1 — Tạo cơ sở dữ liệu

Mở terminal tại thư mục gốc dự án và chạy:

```bash
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -i tao_csdl_ban_ve_tau.sql
```

Script này tạo database `TrainTicketDB` với **18 bảng**, index và dữ liệu mẫu (ga, tuyến, tàu, toa, ghế, giá, chuyến, tài khoản).

> Nếu dùng SQL Server đầy đủ thay vì LocalDB, thay `-S "(LocalDB)\MSSQLLocalDB" -E` bằng `-S .\SQLEXPRESS -U sa -P <mật_khẩu>`.

### Bước 2 — Kiểm tra chuỗi kết nối

Mở `TrainTicketWeb/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "TrainTicketDB": "Server=(LocalDB)\\MSSQLLocalDB;Database=TrainTicketDB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

Sửa lại nếu SQL Server của bạn dùng instance khác hoặc cần tài khoản/mật khẩu.

### Bước 3 — Chạy ứng dụng

```bash
cd TrainTicketWeb
dotnet run
```

Mở trình duyệt tại địa chỉ mà terminal in ra (thường là `https://localhost:7xxx` hoặc `http://localhost:5xxx`).

### Kiểm tra nhanh hệ thống

Truy cập `/Health` để xem chẩn đoán:

```
TrainTicketWeb - Health check
Environment: Development
Server: (LocalDB)\MSSQLLocalDB
Database: TrainTicketDB
Ket noi: OK
So bang: 18
So ga: 6
So ghe: 140
RESULT: OK
```

Nếu `Ket noi: FAIL` → chuỗi kết nối sai hoặc chưa chạy script ở Bước 1.

---

## 🔑 Tài khoản đăng nhập

| Vai trò | Email | Mật khẩu |
|---|---|---|
| Quản trị viên | `admin@example.com` | `Admin@123` |
| Khách hàng | `khach@example.com` | `Admin@123` |

> ⚠️ Đây là tài khoản demo cho đồ án. Trong môi trường thật phải đổi mật khẩu ngay sau khi triển khai.
>
> Nếu bạn tự tạo DB từ `tao_csdl_ban_ve_tau.sql`, mật khẩu của hai tài khoản mẫu có thể khác — hãy dùng chức năng **Đăng ký** để tạo tài khoản mới, hoặc cập nhật `password_hash` bằng hash BCrypt của bạn.

---

## 🧑‍💼 Hướng dẫn sử dụng — Khách hàng

### 1. Đăng ký / Đăng nhập

Vào **Đăng nhập** hoặc **Đăng ký** ở góc phải thanh menu.

- Đăng ký cần: họ tên, email, số điện thoại, mật khẩu (≥ 6 ký tự).
- Đăng nhập bằng email + mật khẩu. Tích *Ghi nhớ đăng nhập* để giữ phiên.

### 2. Tìm chuyến

Tại trang chủ, điền vào ô tìm kiếm:

| Trường | Ghi chú |
|---|---|
| Ga đi | Chọn trong danh sách 6 ga |
| Ga đến | Phải khác ga đi |
| Loại vé | Một chiều / Khứ hồi |
| Ngày đi | Ngày khởi hành mong muốn |
| Ngày về | Chỉ hiện khi chọn Khứ hồi |
| Số lượng | Người lớn, trẻ em, sinh viên, người cao tuổi |

Bấm **Tìm chuyến**. Kết quả hiển thị giờ chạy, thời gian hành trình và số chỗ còn trống.

> 🟠 Nếu chuyến bị hoãn, badge cam **"Hoãn X phút"** xuất hiện bên cạnh giờ tàu. Hệ thống hiện cả giờ gốc và giờ mới.

### 3. Chọn ghế

Bấm **Chọn ghế** trên chuyến muốn đi. Sơ đồ toa hiện ra với 3 màu:

- ⬜ **Trống** — chọn được
- 🟨 **Đang giữ** — người khác đang giữ chỗ
- ⬛ **Đã bán** — không chọn được

Chọn đủ số ghế theo số hành khách rồi bấm **Tiếp tục**.

> ⏱️ Ghế được giữ **15 phút**. Hết thời gian, hệ thống tự nhả ghế về trạng thái trống.

### 4. Khai báo hành khách

Nhập cho từng ghế: họ tên, số giấy tờ (CCCD/mã sinh viên — không bắt buộc), đối tượng.

Đối tượng ưu đãi được giảm giá tự động khi tính tiền.

### 5. Áp mã giảm giá và thanh toán

Ở trang **Xác nhận đặt vé**:

1. Nhập mã vào ô giảm giá → bấm **Áp dụng** để xem số tiền giảm ngay.
2. Chọn phương thức thanh toán: **VietQR**, **MoMo** hoặc **Chuyển khoản**.
3. Bấm **Thanh toán**.

Nếu nhập sai mã, bấm **Bỏ mã** để hủy áp dụng.

### 6. Nhận vé

Sau khi thanh toán thành công, trang **Kết quả** hiển thị mã đơn. Vào **Đơn của tôi** để:

- Xem danh sách đơn và trạng thái
- Bấm mã đơn để xem **chi tiết đơn**
- Bấm vào từng vé để mở **vé điện tử có mã QR** — đưa mã này cho nhân viên soát vé

### 7. Hủy vé

Trong chi tiết đơn, bấm **Hủy vé**. Hệ thống hiển thị ngay:

- Giá vé gốc
- Phí hủy (tính theo thời gian còn lại tới giờ tàu chạy)
- Số tiền sẽ được hoàn

Bấm xác nhận → yêu cầu được gửi tới quản trị viên. Khi admin duyệt, tiền được hoàn và trạng thái vé chuyển thành *Đã hủy*.

---

## 🛡 Hướng dẫn sử dụng — Quản trị viên

Đăng nhập bằng tài khoản `admin`, sau đó vào **Quản trị** hoặc truy cập `/quan-tri`.

### Quản lý ga và tuyến

**Ga** (`/quan-tri/Stations`) — thêm/sửa mã ga, tên ga, tỉnh thành, bật/tắt hoạt động.

**Tuyến** (`/quan-tri/Routes`) — tạo tuyến với ga đầu/ga cuối, sau đó vào **Chi tiết tuyến** để khai báo danh sách ga dừng và khoảng cách từng chặng.

### Quản lý tàu và toa

**Tàu** (`/quan-tri/Trains`) — tạo tàu gắn với tuyến.

Bấm **Cấu hình toa** để chọn loại toa và số lượng toa:

| Loại toa | Sức chứa |
|---|---|
| Ghế ngồi mềm | Theo cấu hình |
| Giường nằm K4 | 4 giường/khoang |
| Giường nằm K6 | 6 giường/khoang |

Hệ thống tự sinh ghế tương ứng.

### Quản lý chuyến

**Chuyến** (`/quan-tri/Trips`):

- **Tạo chuyến**: chọn tàu, ngày, giờ chạy
- **Tạo hàng loạt**: chọn khoảng ngày để sinh nhiều chuyến cùng lúc
- **Đổi trạng thái**: đang chạy / hoàn thành / hủy

#### Hoãn chuyến

Vào **Chi tiết chuyến** → **Hoãn chuyến**, nhập số phút và lý do.

Khách đã mua vé sẽ thấy badge cảnh báo và nhận thông báo real-time. Việc bán vé **vẫn tiếp tục** bình thường.

Bấm **Bỏ hoãn** để đưa chuyến về giờ gốc.

#### Hủy chuyến

**Chi tiết chuyến** → **Hủy chuyến**, nhập lý do.

Hệ thống tự động:

1. Hủy toàn bộ vé đã bán của chuyến
2. Hoàn **100%** tiền, **phí hủy = 0**
3. Tặng mỗi khách bị ảnh hưởng một mã giảm giá xin lỗi (`GIFF555` — giảm 10%)
4. Gửi thông báo real-time tới từng khách

### Quản lý giá vé

**Giá vé** (`/quan-tri/Fares`) — khai báo giá cơ bản theo cặp ga đi/ga đến và loại toa. Giá vé khách trả = giá cơ bản × hệ số đối tượng ưu đãi.

**Đối tượng ưu đãi** (`/quan-tri/PassengerTypes`) — cấu hình % giảm và độ tuổi tối thiểu cho người lớn, trẻ em, sinh viên, người cao tuổi.

### Duyệt hoàn tiền

**Hoàn tiền** (`/quan-tri/Refunds`) — danh sách yêu cầu hủy vé từ khách.

Với mỗi yêu cầu, bấm **Duyệt** hoặc **Từ chối**. Khi duyệt:

- Refund chuyển `completed`
- Payment tương ứng chuyển `refunded`
- Quỹ được cập nhật

### Quản lý mã giảm giá

**Mã giảm giá** (`/quan-tri/Coupons`) — tạo mã theo **phần trăm** hoặc **số tiền cố định**, đặt giới hạn lượt dùng, bật/tắt, xóa.

### Xem quỹ

**Quỹ** (`/quan-tri/Finance`) hiển thị số liệu **thật**, không phải ví ảo:

```
Quỹ = SUM(payments.amount WHERE status IN ('success','refunded'))
    − SUM(refunds.refund_amount WHERE status = 'completed')
```

Tức là: tổng tiền đã thu (kể cả đơn đã hoàn) trừ tổng tiền đã thực hoàn cho khách.

### Quản lý người dùng

**Người dùng** (`/quan-tri/Users`) — xem danh sách, khóa/mở khóa tài khoản. Tài khoản bị khóa không đăng nhập được.

---

## 📐 Quy tắc nghiệp vụ

### Giữ chỗ

Ghế được giữ tối đa **15 phút** kể từ lúc chọn. Quá hạn, hệ thống tự động nhả ghế.

### Phí hủy vé

Tính theo thời gian còn lại tới giờ tàu khởi hành:

| Thời gian trước giờ chạy | Phí hủy |
|---|---|
| ≥ 72 giờ | **0%** |
| ≥ 24 giờ | **10%** |
| ≥ 6 giờ | **20%** |
| < 6 giờ | **30%** |

Ví dụ: vé 1.000.000 ₫, hủy trước 30 giờ → phí 10% = 100.000 ₫, hoàn lại 900.000 ₫.

### Hủy chuyến (admin)

Khác với khách hủy vé: hoàn **100%**, **không tính phí**, cộng thêm mã giảm giá xin lỗi.

### Đối tượng ưu đãi

| Đối tượng | Giảm giá |
|---|---|
| Người lớn | 0% |
| Trẻ em | Theo cấu hình |
| Sinh viên | Theo cấu hình |
| Người cao tuổi | Theo cấu hình |

---

## 📁 Cấu trúc dự án

```
he_thong_ban_ve_tau/
├── tao_csdl_ban_ve_tau.sql          # Script tạo DB + dữ liệu mẫu
├── dac_ta_he_thong_ban_ve_tau.docx  # Đặc tả hệ thống
├── on-tap-giua-ky.pdf               # Đề bài
├── deploy_somee/                    # Script triển khai hosting
│   ├── TrainTicketDB_somee.sql      # Tạo bảng trên Somee
│   ├── va_default_somee.sql         # Vá DEFAULT constraint
│   ├── data-*.sql                   # Nạp dữ liệu mẫu
│   └── web.config.template          # Mẫu web.config IIS
└── TrainTicketWeb/                  # Project chính
    ├── TrainTicketWeb.csproj
    ├── Program.cs                   # Khởi động, DI, route, SignalR
    ├── appsettings.json             # Chuỗi kết nối (LocalDB)
    ├── appsettings.Production.json  # Template cho hosting
    ├── Controllers/
    │   ├── HomeController.cs        # Trang chủ, tìm chuyến
    │   ├── AccountController.cs     # Đăng ký, đăng nhập, hồ sơ
    │   ├── BookingController.cs     # Toàn bộ luồng đặt vé
    │   ├── QuanTriController.cs     # 16 trang quản trị
    │   └── NotificationController.cs
    ├── Services/                    # Nghiệp vụ
    │   ├── AuthService.cs           # BCrypt, đăng nhập
    │   ├── IBookingService.cs       # Giữ chỗ, thanh toán, hủy
    │   ├── IScheduleService.cs      # Chuyến, giờ chạy
    │   ├── ITrainService.cs
    │   ├── IStationService.cs
    │   ├── IRouteService.cs
    │   ├── IUserService.cs
    │   ├── CouponService.cs
    │   ├── TripAdminService.cs      # Hoãn/hủy chuyến
    │   ├── AppSettings.cs           # Hằng số nghiệp vụ
    │   └── QrHelper.cs              # Sinh mã QR
    ├── Repositories/                # ADO.NET thuần
    │   ├── IBookingRepository.cs
    │   ├── IScheduleRepository.cs
    │   ├── ITrainRepository.cs
    │   ├── IStationRepository.cs
    │   ├── IRouteRepository.cs
    │   ├── IUserRepository.cs
    │   ├── ICouponRepository.cs
    │   ├── IFinanceRepository.cs
    │   ├── INotificationRepository.cs
    │   └── IReportRepository.cs
    ├── Models/                      # Entity + ViewModel
    ├── ViewModels/
    ├── Views/
    │   ├── Home/                    # Trang chủ, tìm chuyến
    │   ├── Account/                 # Đăng nhập, đăng ký, hồ sơ
    │   ├── Booking/                 # Chọn ghế → thanh toán → vé
    │   ├── QuanTri/                 # 16 trang admin
    │   └── Shared/                  # Layout, partial
    ├── Hubs/NotificationHub.cs      # SignalR real-time
    ├── Filters/AccessFilters.cs
    └── wwwroot/                     # CSS, JS, ảnh
```

**Luồng dữ liệu:** `Controller` → `Service` (nghiệp vụ) → `Repository` (SQL) → `Database`

Không có ORM. Mọi truy vấn viết tay bằng `SqlCommand` + `SqlParameter`.

---

## 🗄 Cơ sở dữ liệu

**18 bảng** trong `TrainTicketDB`:

| Nhóm | Bảng |
|---|---|
| Danh mục | `users`, `stations`, `routes`, `route_stations` |
| Phương tiện | `trains`, `coach_types`, `coaches`, `seats` |
| Lịch chạy | `trips`, `trip_stops`, `fares`, `passenger_types` |
| Giao dịch | `bookings`, `tickets`, `payments`, `refunds` |
| Bổ trợ | `coupons`, `notifications` |

**Dữ liệu mẫu:** 6 ga, 2 tuyến, 2 tàu, 8 toa, 140 ghế, 53 chuyến, 90 mức giá.

Xem sơ đồ chi tiết trong `dac_ta_he_thong_ban_ve_tau.docx`.

---

## ☁️ Triển khai lên hosting

Dự án đã được triển khai thành công trên **Somee.com** (IIS, .NET 8).

### Yêu cầu hosting

- **App pool 32-bit** hoặc 64-bit — phải khớp với kiến trúc bản publish
- **ASP.NET Core 8 Runtime** (hoặc publish self-contained)
- **Hosting model:** `outofprocess`
- **CLR Version:** `No Managed Code`

### Các bước

1. **Tạo database** trên hosting, chạy `deploy_somee/TrainTicketDB_somee.sql`, sau đó `va_default_somee.sql`, rồi nạp `data-*.sql` theo thứ tự: `coaches` → `seats` → `fares` → `trips` → `trip_stops` → `bookings` → `tickets` → `payments` → `refunds`.

2. **Cấu hình chuỗi kết nối** — sửa `appsettings.Production.json` với thông tin database thật của bạn:

   ```json
   {
     "ConnectionStrings": {
       "TrainTicketDB": "workstation id=YOUR_SERVER.mssql.somee.com;user id=YOUR_SQL_USER;pwd=YOUR_SQL_PASSWORD;..."
     }
   }
   ```

3. **Publish** đúng kiến trúc:

   ```bash
   dotnet publish TrainTicketWeb/TrainTicketWeb.csproj -c Release -r win-x86 --self-contained true -o publish
   ```

4. **Tạo `web.config`** từ `deploy_somee/web.config.template`:

   ```xml
   <aspNetCore processPath=".\TrainTicketWeb.exe" stdoutLogEnabled="true"
               stdoutLogFile=".\logs\stdout" hostingModel="outofprocess" />
   ```

5. **Upload** toàn bộ thư mục `publish/` lên hosting qua FTP.

6. **Kiểm tra** `/Health` — phải hiện `RESULT: OK`.

### Lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| `HTTP Error 500.32` | Kiến trúc exe không khớp app pool | Đổi app pool sang 64-bit, hoặc publish `-r win-x86` |
| `HTTP Error 500.30` | App khởi động thất bại | Xem `logs/stdout*.log` |
| `HTTP Error 502.5` | Thiếu runtime hoặc sai đường dẫn exe | Publish self-contained, kiểm tra `processPath` |
| `Ket noi: FAIL` | Chuỗi kết nối sai | Kiểm tra server/user/password trong `appsettings.Production.json` |
| Chữ tiếng Việt bị lỗi | Script SQL lưu sai encoding | Lưu file `.sql` dạng UTF-8 **có BOM**, chạy bằng `sqlcmd -i` (không dùng `-Q`) |
| Link chi tiết đơn bị 302 | Thiếu route binding cho tham số `bookingId` | Đảm bảo action có `[Route("Booking/Detail/{bookingId:int}")]` |

---

## 📝 Ghi chú

- Đây là **đồ án học tập**, không dùng cho môi trường thương mại.
- `appsettings.Production.json` trong repo chỉ là **template** — không chứa thông tin đăng nhập thật.
- Thanh toán QR hiện là **mô phỏng** (không kết nối cổng thanh toán thật).
- Mọi thao tác đặt vé, thanh toán, hủy vé đều đã được kiểm thử trên database thật.

---

## 📄 Giấy phép

Dự án phục vụ mục đích học tập. Xem đề bài trong `on-tap-giua-ky.pdf` và đặc tả trong `dac_ta_he_thong_ban_ve_tau.docx`.
