use master;
GO
IF DB_ID(N'TrainTicketDB') IS NOT NULL
BEGIN
    ALTER DATABASE TrainTicketDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE TrainTicketDB;
END
GO
CREATE DATABASE TrainTicketDB;
GO
USE TrainTicketDB;
GO

/* ===================== 1. TẠO BẢNG ===================== */

CREATE TABLE users (
    user_id        INT IDENTITY(1,1) CONSTRAINT PK_users PRIMARY KEY,
    full_name      NVARCHAR(100) NOT NULL,
    email          NVARCHAR(100) NOT NULL CONSTRAINT UQ_users_email UNIQUE,
    phone          VARCHAR(15) NULL,
    password_hash  NVARCHAR(255) NOT NULL,
    [role]         VARCHAR(10) NOT NULL CONSTRAINT DF_users_role DEFAULT 'customer',
    is_locked      BIT NOT NULL CONSTRAINT DF_users_locked DEFAULT 0,
    created_at     DATETIME2 NOT NULL CONSTRAINT DF_users_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_users_role CHECK ([role] IN ('customer','admin'))
);

CREATE TABLE stations (
    station_id    INT IDENTITY(1,1) CONSTRAINT PK_stations PRIMARY KEY,
    station_code  VARCHAR(10) NOT NULL CONSTRAINT UQ_stations_code UNIQUE,
    station_name  NVARCHAR(100) NOT NULL,
    is_active     BIT NOT NULL CONSTRAINT DF_stations_active DEFAULT 1
);

CREATE TABLE routes (
    route_id    INT IDENTITY(1,1) CONSTRAINT PK_routes PRIMARY KEY,
    route_name  NVARCHAR(100) NOT NULL
);

CREATE TABLE route_stations (
    route_id     INT NOT NULL CONSTRAINT FK_rs_route REFERENCES routes(route_id),
    station_id   INT NOT NULL CONSTRAINT FK_rs_station REFERENCES stations(station_id),
    stop_order   INT NOT NULL,
    distance_km  DECIMAL(7,1) NOT NULL,
    CONSTRAINT PK_route_stations PRIMARY KEY (route_id, station_id),
    CONSTRAINT UQ_rs_order UNIQUE (route_id, stop_order)
);

CREATE TABLE trains (
    train_id    INT IDENTITY(1,1) CONSTRAINT PK_trains PRIMARY KEY,
    train_code  VARCHAR(10) NOT NULL CONSTRAINT UQ_trains_code UNIQUE,
    route_id    INT NOT NULL CONSTRAINT FK_trains_route REFERENCES routes(route_id),
    is_active   BIT NOT NULL CONSTRAINT DF_trains_active DEFAULT 1
);

CREATE TABLE coach_types (
    coach_type_id  INT IDENTITY(1,1) CONSTRAINT PK_coach_types PRIMARY KEY,
    type_name      NVARCHAR(50) NOT NULL CONSTRAINT UQ_coach_types_name UNIQUE,
    capacity       INT NOT NULL CONSTRAINT CK_coach_types_cap CHECK (capacity > 0)
);

CREATE TABLE coaches (
    coach_id       INT IDENTITY(1,1) CONSTRAINT PK_coaches PRIMARY KEY,
    train_id       INT NOT NULL CONSTRAINT FK_coaches_train REFERENCES trains(train_id),
    coach_number   INT NOT NULL,
    coach_type_id  INT NOT NULL CONSTRAINT FK_coaches_type REFERENCES coach_types(coach_type_id),
    CONSTRAINT UQ_coaches UNIQUE (train_id, coach_number)
);

CREATE TABLE seats (
    seat_id      INT IDENTITY(1,1) CONSTRAINT PK_seats PRIMARY KEY,
    coach_id     INT NOT NULL CONSTRAINT FK_seats_coach REFERENCES coaches(coach_id),
    seat_number  VARCHAR(5) NOT NULL,
    berth_level  TINYINT NULL,
    CONSTRAINT UQ_seats UNIQUE (coach_id, seat_number)
);

CREATE TABLE trips (
    trip_id         INT IDENTITY(1,1) CONSTRAINT PK_trips PRIMARY KEY,
    train_id        INT NOT NULL CONSTRAINT FK_trips_train REFERENCES trains(train_id),
    departure_date  DATE NOT NULL,
    [status]        VARCHAR(15) NOT NULL CONSTRAINT DF_trips_status DEFAULT 'scheduled',
    CONSTRAINT UQ_trips UNIQUE (train_id, departure_date),
    CONSTRAINT CK_trips_status CHECK ([status] IN ('scheduled','running','completed','cancelled'))
);

CREATE TABLE trip_stops (
    trip_id         INT NOT NULL CONSTRAINT FK_ts_trip REFERENCES trips(trip_id),
    station_id      INT NOT NULL CONSTRAINT FK_ts_station REFERENCES stations(station_id),
    stop_order      INT NOT NULL,
    arrival_time    DATETIME2 NULL,
    departure_time  DATETIME2 NULL,
    CONSTRAINT PK_trip_stops PRIMARY KEY (trip_id, station_id),
    CONSTRAINT UQ_ts_order UNIQUE (trip_id, stop_order)
);

CREATE TABLE fares (
    fare_id          INT IDENTITY(1,1) CONSTRAINT PK_fares PRIMARY KEY,
    from_station_id  INT NOT NULL CONSTRAINT FK_fares_from REFERENCES stations(station_id),
    to_station_id    INT NOT NULL CONSTRAINT FK_fares_to REFERENCES stations(station_id),
    coach_type_id    INT NOT NULL CONSTRAINT FK_fares_type REFERENCES coach_types(coach_type_id),
    base_price       DECIMAL(12,0) NOT NULL CONSTRAINT CK_fares_price CHECK (base_price > 0),
    CONSTRAINT UQ_fares UNIQUE (from_station_id, to_station_id, coach_type_id),
    CONSTRAINT CK_fares_stations CHECK (from_station_id <> to_station_id)
);

CREATE TABLE passenger_types (
    passenger_type_id  INT IDENTITY(1,1) CONSTRAINT PK_passenger_types PRIMARY KEY,
    type_name          NVARCHAR(30) NOT NULL CONSTRAINT UQ_passenger_types_name UNIQUE,
    discount_percent   DECIMAL(5,2) NOT NULL CONSTRAINT DF_pt_discount DEFAULT 0,
    requires_id        BIT NOT NULL CONSTRAINT DF_pt_requires DEFAULT 0,
    CONSTRAINT CK_pt_discount CHECK (discount_percent BETWEEN 0 AND 100)
);

CREATE TABLE bookings (
    booking_id       INT IDENTITY(1,1) CONSTRAINT PK_bookings PRIMARY KEY,
    booking_code     VARCHAR(12) NOT NULL CONSTRAINT UQ_bookings_code UNIQUE,
    user_id          INT NOT NULL CONSTRAINT FK_bookings_user REFERENCES users(user_id),
    contact_email    NVARCHAR(100) NOT NULL,
    contact_phone    VARCHAR(15) NOT NULL,
    total_amount     DECIMAL(12,0) NOT NULL CONSTRAINT DF_bookings_total DEFAULT 0,
    [status]         VARCHAR(15) NOT NULL CONSTRAINT DF_bookings_status DEFAULT 'holding',
    hold_expires_at  DATETIME2 NOT NULL,
    created_at       DATETIME2 NOT NULL CONSTRAINT DF_bookings_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_bookings_status CHECK ([status] IN ('holding','paid','expired','cancelled'))
);

CREATE TABLE tickets (
    ticket_id          INT IDENTITY(1,1) CONSTRAINT PK_tickets PRIMARY KEY,
    ticket_code        VARCHAR(15) NOT NULL CONSTRAINT UQ_tickets_code UNIQUE,
    booking_id         INT NOT NULL CONSTRAINT FK_tickets_booking REFERENCES bookings(booking_id),
    trip_id            INT NOT NULL CONSTRAINT FK_tickets_trip REFERENCES trips(trip_id),
    seat_id            INT NOT NULL CONSTRAINT FK_tickets_seat REFERENCES seats(seat_id),
    from_station_id    INT NOT NULL CONSTRAINT FK_tickets_from REFERENCES stations(station_id),
    to_station_id      INT NOT NULL CONSTRAINT FK_tickets_to REFERENCES stations(station_id),
    leg                VARCHAR(10) NOT NULL CONSTRAINT DF_tickets_leg DEFAULT 'outbound',
    passenger_type_id  INT NOT NULL CONSTRAINT FK_tickets_ptype REFERENCES passenger_types(passenger_type_id),
    passenger_name     NVARCHAR(100) NOT NULL,
    id_number          VARCHAR(20) NULL,
    original_price     DECIMAL(12,0) NOT NULL,
    final_price        DECIMAL(12,0) NOT NULL,
    [status]           VARCHAR(15) NOT NULL CONSTRAINT DF_tickets_status DEFAULT 'holding',
    cancelled_at       DATETIME2 NULL,
    refund_amount      DECIMAL(12,0) NULL,
    CONSTRAINT CK_tickets_leg CHECK (leg IN ('outbound','return')),
    CONSTRAINT CK_tickets_status CHECK ([status] IN ('holding','paid','cancelled','expired')),
    CONSTRAINT CK_tickets_stations CHECK (from_station_id <> to_station_id)
);
CREATE INDEX IX_tickets_trip_seat ON tickets (trip_id, seat_id, [status]);

CREATE TABLE payments (
    payment_id       INT IDENTITY(1,1) CONSTRAINT PK_payments PRIMARY KEY,
    booking_id       INT NOT NULL CONSTRAINT FK_payments_booking REFERENCES bookings(booking_id),
    method           VARCHAR(15) NOT NULL,
    amount           DECIMAL(12,0) NOT NULL,
    transaction_ref  VARCHAR(100) NULL,
    [status]         VARCHAR(15) NOT NULL CONSTRAINT DF_payments_status DEFAULT 'pending',
    paid_at          DATETIME2 NULL,
    CONSTRAINT CK_payments_method CHECK (method IN ('vnpay','momo','bank_qr')),
    CONSTRAINT CK_payments_status CHECK ([status] IN ('pending','success','failed','refunded'))
);
GO

/* ===================== 2. DỮ LIỆU MẪU ===================== */

-- Người dùng. LƯU Ý: password_hash chỉ là chữ giữ chỗ, chưa đăng nhập được.
-- Khi làm xong chức năng đăng ký, hãy tạo tài khoản qua web hoặc cập nhật lại hash cho 2 dòng này.
INSERT INTO users (full_name, email, phone, password_hash, [role]) VALUES
 (N'Quản trị viên', 'admin@example.com', '0900000001', 'CHANGE_ME', 'admin'),
 (N'Nguyễn Văn A',  'khach@example.com', '0900000002', 'CHANGE_ME', 'customer');

-- Ga
INSERT INTO stations (station_code, station_name) VALUES
 ('HAN', N'Hà Nội'), ('VIN', N'Vinh'), ('HUE', N'Huế'),
 ('DAD', N'Đà Nẵng'), ('NTR', N'Nha Trang'), ('SGN', N'Sài Gòn');

-- Tuyến: 1 = Hà Nội -> Sài Gòn, 2 = Sài Gòn -> Hà Nội
INSERT INTO routes (route_name) VALUES (N'Hà Nội – TP.HCM'), (N'TP.HCM – Hà Nội');

INSERT INTO route_stations (route_id, station_id, stop_order, distance_km)
SELECT 1, station_id, o, km FROM stations s
JOIN (VALUES ('HAN',1,0),('VIN',2,319),('HUE',3,688),('DAD',4,791),('NTR',5,1315),('SGN',6,1726)) v(code,o,km)
  ON v.code = s.station_code;

INSERT INTO route_stations (route_id, station_id, stop_order, distance_km)
SELECT 2, station_id, o, km FROM stations s
JOIN (VALUES ('SGN',1,0),('NTR',2,411),('DAD',3,935),('HUE',4,1038),('VIN',5,1407),('HAN',6,1726)) v(code,o,km)
  ON v.code = s.station_code;

-- Đoàn tàu
INSERT INTO trains (train_code, route_id) VALUES ('SE1', 1), ('SE2', 2);

-- Loại toa (số chỗ để nhỏ cho dễ demo; có thể sửa lại cho giống thực tế)
INSERT INTO coach_types (type_name, capacity) VALUES
 (N'Ghế ngồi mềm', 20),
 (N'Giường nằm khoang 4', 12),
 (N'Giường nằm khoang 6', 18);

-- Mỗi tàu 4 toa: toa 1-2 ghế mềm, toa 3 giường khoang 6, toa 4 giường khoang 4
INSERT INTO coaches (train_id, coach_number, coach_type_id)
SELECT t.train_id, x.n, x.ct
FROM trains t CROSS JOIN (VALUES (1,1),(2,1),(3,3),(4,2)) AS x(n, ct);

-- Sinh ghế/giường cho từng toa theo sức chứa của loại toa
INSERT INTO seats (coach_id, seat_number, berth_level)
SELECT c.coach_id,
       RIGHT('0' + CAST(v.number AS VARCHAR(3)), 2),
       CASE c.coach_type_id
            WHEN 2 THEN ((v.number - 1) % 2) + 1
            WHEN 3 THEN ((v.number - 1) % 3) + 1
            ELSE NULL END
FROM coaches c
JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
JOIN master.dbo.spt_values v ON v.type = 'P' AND v.number BETWEEN 1 AND ct.capacity;

-- Chuyến tàu: 7 ngày kể từ ngày mai cho mỗi tàu (SE1 chạy 19h, SE2 chạy 20h)
INSERT INTO trips (train_id, departure_date)
SELECT t.train_id, CAST(DATEADD(DAY, v.number, SYSDATETIME()) AS DATE)
FROM trains t
CROSS JOIN master.dbo.spt_values v
WHERE v.type = 'P' AND v.number BETWEEN 1 AND 7;

-- Giờ đến/đi từng ga (tốc độ trung bình 55 km/h, dừng mỗi ga 5 phút)
INSERT INTO trip_stops (trip_id, station_id, stop_order, arrival_time, departure_time)
SELECT tr.trip_id, rs.station_id, rs.stop_order,
       CASE WHEN rs.stop_order = 1 THEN NULL
            ELSE DATEADD(MINUTE, CAST(rs.distance_km * 60.0 / 55 AS INT), b.base) END,
       CASE WHEN rs.stop_order = mx.max_order THEN NULL
            WHEN rs.stop_order = 1 THEN b.base
            ELSE DATEADD(MINUTE, CAST(rs.distance_km * 60.0 / 55 AS INT) + 5, b.base) END
FROM trips tr
JOIN trains t ON t.train_id = tr.train_id
JOIN route_stations rs ON rs.route_id = t.route_id
CROSS APPLY (SELECT DATEADD(HOUR, CASE t.train_code WHEN 'SE1' THEN 19 ELSE 20 END,
                            CAST(tr.departure_date AS DATETIME2)) AS base) b
CROSS APPLY (SELECT MAX(stop_order) AS max_order FROM route_stations WHERE route_id = t.route_id) mx;

-- Bảng giá: mọi cặp ga, cả hai chiều. Giá = km x đơn giá/km, làm tròn đến nghìn đồng
INSERT INTO fares (from_station_id, to_station_id, coach_type_id, base_price)
SELECT a.station_id, b.station_id, ct.coach_type_id,
       CAST(ROUND(ABS(b.distance_km - a.distance_km) *
            CASE ct.coach_type_id WHEN 1 THEN 700 WHEN 2 THEN 1200 ELSE 1000 END / 1000.0, 0) * 1000
            AS DECIMAL(12,0))
FROM route_stations a
JOIN route_stations b ON b.route_id = a.route_id AND b.station_id <> a.station_id
CROSS JOIN coach_types ct
WHERE a.route_id = 1;

-- Đối tượng hành khách (tỷ lệ giảm chỉ là số mẫu, Admin có thể chỉnh)
INSERT INTO passenger_types (type_name, discount_percent, requires_id) VALUES
 (N'Người lớn', 0, 0),
 (N'Trẻ em', 25, 0),
 (N'Sinh viên', 10, 1),
 (N'Người cao tuổi', 15, 1);
GO

-- Một đơn mẫu đã thanh toán (để thử sơ đồ toa có ghế "Đã bán" và trang lịch sử vé)
DECLARE @han INT = (SELECT station_id FROM stations WHERE station_code = 'HAN');
DECLARE @dad INT = (SELECT station_id FROM stations WHERE station_code = 'DAD');
DECLARE @uid INT = (SELECT user_id FROM users WHERE email = 'khach@example.com');
DECLARE @trip INT = (SELECT TOP 1 tr.trip_id FROM trips tr
                     JOIN trains t ON t.train_id = tr.train_id
                     WHERE t.train_code = 'SE1' ORDER BY tr.departure_date);
DECLARE @seat INT = (SELECT TOP 1 s.seat_id FROM seats s
                     JOIN coaches c ON c.coach_id = s.coach_id
                     JOIN trains t ON t.train_id = c.train_id
                     WHERE t.train_code = 'SE1' AND c.coach_number = 1 AND s.seat_number = '01');
DECLARE @price DECIMAL(12,0) = (SELECT base_price FROM fares
                                WHERE from_station_id = @han AND to_station_id = @dad AND coach_type_id = 1);
DECLARE @adult INT = (SELECT passenger_type_id FROM passenger_types WHERE type_name = N'Người lớn');
DECLARE @bid INT;

INSERT INTO bookings (booking_code, user_id, contact_email, contact_phone, total_amount, [status], hold_expires_at)
VALUES ('BK000001', @uid, 'khach@example.com', '0900000002', @price, 'paid', DATEADD(MINUTE, 15, SYSDATETIME()));
SET @bid = SCOPE_IDENTITY();

INSERT INTO tickets (ticket_code, booking_id, trip_id, seat_id, from_station_id, to_station_id,
                     passenger_type_id, passenger_name, id_number, original_price, final_price, [status])
VALUES ('TK0000001', @bid, @trip, @seat, @han, @dad, @adult, N'Nguyễn Văn A', '012345678901', @price, @price, 'paid');

INSERT INTO payments (booking_id, method, amount, transaction_ref, [status], paid_at)
VALUES (@bid, 'vnpay', @price, 'SIM-0001', 'success', SYSDATETIME());
GO

/* ===================== 3. KIỂM TRA NHANH ===================== */
SELECT 'stations' AS bang, COUNT(*) AS so_dong FROM stations UNION ALL
SELECT 'route_stations', COUNT(*) FROM route_stations UNION ALL
SELECT 'trains', COUNT(*) FROM trains UNION ALL
SELECT 'coaches', COUNT(*) FROM coaches UNION ALL
SELECT 'seats', COUNT(*) FROM seats UNION ALL
SELECT 'trips', COUNT(*) FROM trips UNION ALL
SELECT 'trip_stops', COUNT(*) FROM trip_stops UNION ALL
SELECT 'fares', COUNT(*) FROM fares UNION ALL
SELECT 'tickets', COUNT(*) FROM tickets;
-- Kết quả mong đợi: stations 6, route_stations 12, trains 2, coaches 8, seats 140, trips 14, trip_stops 84, fares 90, tickets 1
GO
