/* ============================================================
   HỆ THỐNG BÁN VÉ TÀU HỎA TRỰC TUYẾN
   CSDL: TrainTicketDB — khớp 100% đặc tả DOCX (16 bảng)
   ============================================================ */
USE master;
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
    [status]       VARCHAR(10) NOT NULL CONSTRAINT DF_users_status DEFAULT 'active',
    created_at     DATETIME2 NOT NULL CONSTRAINT DF_users_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_users_role CHECK ([role] IN ('customer','admin')),
    CONSTRAINT CK_users_status CHECK ([status] IN ('active','locked'))
);

CREATE TABLE stations (
    station_id     INT IDENTITY(1,1) CONSTRAINT PK_stations PRIMARY KEY,
    station_code   VARCHAR(10) NOT NULL CONSTRAINT UQ_stations_code UNIQUE,
    station_name   NVARCHAR(100) NOT NULL,
    province       NVARCHAR(50) NULL,
    is_active      BIT NOT NULL CONSTRAINT DF_stations_active DEFAULT 1
);

CREATE TABLE routes (
    route_id          INT IDENTITY(1,1) CONSTRAINT PK_routes PRIMARY KEY,
    route_name        NVARCHAR(100) NOT NULL,
    start_station_id  INT NOT NULL CONSTRAINT FK_routes_start REFERENCES stations(station_id),
    end_station_id    INT NOT NULL CONSTRAINT FK_routes_end REFERENCES stations(station_id),
    CONSTRAINT CK_routes_endpoints CHECK (start_station_id <> end_station_id)
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
    capacity       INT NOT NULL CONSTRAINT CK_coach_types_cap CHECK (capacity > 0),
    description    NVARCHAR(255) NULL
);

CREATE TABLE coaches (
    coach_id       INT IDENTITY(1,1) CONSTRAINT PK_coaches PRIMARY KEY,
    train_id       INT NOT NULL CONSTRAINT FK_coaches_train REFERENCES trains(train_id),
    coach_number   INT NOT NULL,
    coach_type_id  INT NOT NULL CONSTRAINT FK_coaches_type REFERENCES coach_types(coach_type_id),
    CONSTRAINT UQ_coaches UNIQUE (train_id, coach_number)
);

CREATE TABLE seats (
    seat_id         INT IDENTITY(1,1) CONSTRAINT PK_seats PRIMARY KEY,
    coach_id        INT NOT NULL CONSTRAINT FK_seats_coach REFERENCES coaches(coach_id),
    seat_number     VARCHAR(5) NOT NULL,
    compartment_no  INT NULL,
    berth_level     TINYINT NULL,
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
    effective_from   DATE NOT NULL,
    effective_to     DATE NULL,
    CONSTRAINT UQ_fares UNIQUE (from_station_id, to_station_id, coach_type_id),
    CONSTRAINT CK_fares_stations CHECK (from_station_id <> to_station_id)
);

CREATE TABLE passenger_types (
    passenger_type_id  INT IDENTITY(1,1) CONSTRAINT PK_passenger_types PRIMARY KEY,
    type_name          NVARCHAR(30) NOT NULL CONSTRAINT UQ_passenger_types_name UNIQUE,
    discount_percent   DECIMAL(5,2) NOT NULL CONSTRAINT DF_pt_discount DEFAULT 0,
    min_age            INT NULL,
    max_age            INT NULL,
    id_label           NVARCHAR(30) NULL,
    is_active          BIT NOT NULL CONSTRAINT DF_pt_active DEFAULT 1,
    CONSTRAINT CK_pt_discount CHECK (discount_percent BETWEEN 0 AND 100)
);

CREATE TABLE bookings (
    booking_id       INT IDENTITY(1,1) CONSTRAINT PK_bookings PRIMARY KEY,
    booking_code     VARCHAR(12) NOT NULL CONSTRAINT UQ_bookings_code UNIQUE,
    user_id          INT NOT NULL CONSTRAINT FK_bookings_user REFERENCES users(user_id),
    trip_type        VARCHAR(10) NOT NULL CONSTRAINT DF_bookings_trip_type DEFAULT 'one_way',
    contact_email    NVARCHAR(100) NOT NULL,
    contact_phone    VARCHAR(15) NOT NULL,
    total_amount     DECIMAL(12,0) NOT NULL CONSTRAINT DF_bookings_total DEFAULT 0,
    [status]         VARCHAR(15) NOT NULL CONSTRAINT DF_bookings_status DEFAULT 'holding',
    hold_expires_at  DATETIME2 NOT NULL,
    created_at       DATETIME2 NOT NULL CONSTRAINT DF_bookings_created DEFAULT SYSDATETIME(),
    CONSTRAINT CK_bookings_trip_type CHECK (trip_type IN ('one_way','round_trip')),
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
    discount_amount    DECIMAL(12,0) NOT NULL CONSTRAINT DF_tickets_discount DEFAULT 0,
    final_price        DECIMAL(12,0) NOT NULL,
    [status]           VARCHAR(15) NOT NULL CONSTRAINT DF_tickets_status DEFAULT 'holding',
    created_at         DATETIME2 NOT NULL CONSTRAINT DF_tickets_created DEFAULT SYSDATETIME(),
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
    created_at       DATETIME2 NOT NULL CONSTRAINT DF_payments_created DEFAULT SYSDATETIME(),
    paid_at          DATETIME2 NULL,
    CONSTRAINT CK_payments_method CHECK (method IN ('vnpay','momo','bank_qr')),
    CONSTRAINT CK_payments_status CHECK ([status] IN ('pending','success','failed','refunded'))
);

CREATE TABLE refunds (
    refund_id       INT IDENTITY(1,1) CONSTRAINT PK_refunds PRIMARY KEY,
    ticket_id       INT NOT NULL CONSTRAINT FK_refunds_ticket REFERENCES tickets(ticket_id),
    payment_id      INT NULL CONSTRAINT FK_refunds_payment REFERENCES payments(payment_id),
    reason          NVARCHAR(255) NULL,
    refund_fee      DECIMAL(12,0) NOT NULL CONSTRAINT DF_refunds_fee DEFAULT 0,
    refund_amount   DECIMAL(12,0) NOT NULL,
    [status]        VARCHAR(15) NOT NULL CONSTRAINT DF_refunds_status DEFAULT 'requested',
    requested_at    DATETIME2 NOT NULL CONSTRAINT DF_refunds_requested DEFAULT SYSDATETIME(),
    processed_by    INT NULL CONSTRAINT FK_refunds_admin REFERENCES users(user_id),
    processed_at    DATETIME2 NULL,
    CONSTRAINT CK_refunds_status CHECK ([status] IN ('requested','approved','rejected','completed'))
);
GO

/* ===================== 2. INDEX GỢI Ý (theo DOCX) ===================== */
CREATE INDEX IX_trips_departure ON trips (departure_date);
CREATE INDEX IX_bookings_user_status ON bookings (user_id, [status]);
CREATE INDEX IX_bookings_hold ON bookings (hold_expires_at);
GO

/* ===================== 3. DỮ LIỆU MẪU ===================== */

-- Người dùng (mật khẩu thật, đã băm bcrypt)
INSERT INTO users (full_name, email, phone, password_hash, [role]) VALUES
 (N'Quản trị viên', 'admin@example.com', '0900000001', '$2a$12$ZirZwQ4WNX0xvWALXhlECeq9X0IAyBKQ.ezysiu50XCoJ2Zz6GZSq', 'admin'),
 (N'Nguyễn Văn A',  'khach@example.com', '0900000002', '$2a$12$pXzqyAsahebnhg6lAJ7BPO3gquQCGUo2FlKEdzvg10fAru75.2872', 'customer');

-- Ga (có tỉnh/thành)
INSERT INTO stations (station_code, station_name, province) VALUES
 ('HAN', N'Hà Nội',     N'Hà Nội'),
 ('VIN', N'Vinh',       N'Nghệ An'),
 ('HUE', N'Huế',        N'Thừa Thiên Huế'),
 ('DAD', N'Đà Nẵng',    N'Đà Nẵng'),
 ('NTR', N'Nha Trang',  N'Khánh Hòa'),
 ('SGN', N'Sài Gòn',    N'TP. Hồ Chí Minh');

-- Tuyến (có ga đầu / ga cuối)
INSERT INTO routes (route_name, start_station_id, end_station_id)
SELECT N'Hà Nội – TP.HCM', (SELECT station_id FROM stations WHERE station_code='HAN'),
       (SELECT station_id FROM stations WHERE station_code='SGN');
INSERT INTO routes (route_name, start_station_id, end_station_id)
SELECT N'TP.HCM – Hà Nội', (SELECT station_id FROM stations WHERE station_code='SGN'),
       (SELECT station_id FROM stations WHERE station_code='HAN');

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

-- Loại toa
INSERT INTO coach_types (type_name, capacity, description) VALUES
 (N'Ghế ngồi mềm',        20, N'Ghế ngồi hạng thường, toa mở'),
 (N'Giường nằm khoang 4', 12, N'Giường nằm, mỗi khoang 4 giường'),
 (N'Giường nằm khoang 6', 18, N'Giường nằm, mỗi khoang 6 giường');

-- Mỗi tàu 4 toa: 1-2 ghế mềm, 3 giường khoang 6, 4 giường khoang 4
INSERT INTO coaches (train_id, coach_number, coach_type_id)
SELECT t.train_id, x.n, x.ct
FROM trains t CROSS JOIN (VALUES (1,1),(2,1),(3,3),(4,2)) AS x(n, ct);

-- Sinh ghế/giường (số 1..30, không dùng bảng hệ thống)
;WITH nums(n) AS (
  SELECT n FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
                        (11),(12),(13),(14),(15),(16),(17),(18),(19),(20),
                        (21),(22),(23),(24),(25),(26),(27),(28),(29),(30)) v(n)
)
INSERT INTO seats (coach_id, seat_number, compartment_no, berth_level)
SELECT c.coach_id,
       RIGHT('0' + CAST(v.n AS VARCHAR(3)), 2),
       CASE WHEN c.coach_type_id IN (2,3)
            THEN ((v.n - 1) / 4) + 1
            ELSE NULL END,
       CASE c.coach_type_id
            WHEN 2 THEN ((v.n - 1) % 4) + 1
            WHEN 3 THEN ((v.n - 1) % 6) + 1
            ELSE NULL END
FROM coaches c
JOIN coach_types ct ON ct.coach_type_id = c.coach_type_id
JOIN nums v ON v.n <= ct.capacity;

-- Chuyến tàu: 14 ngày kể từ ngày mai cho mỗi tàu
;WITH nums(n) AS (
  SELECT n FROM (VALUES (1),(2),(3),(4),(5),(6),(7),(8),(9),(10),
                        (11),(12),(13),(14),(15)) v(n)
)
INSERT INTO trips (train_id, departure_date)
SELECT t.train_id, CAST(DATEADD(DAY, v.n, SYSDATETIME()) AS DATE)
FROM trains t CROSS JOIN nums v;

-- Giờ đến/đi từng ga (55 km/h, dừng 5 phút)
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

-- Bảng giá: mọi cặp ga (theo khoảng cách), 3 loại toa
INSERT INTO fares (from_station_id, to_station_id, coach_type_id, base_price, effective_from, effective_to)
SELECT a.station_id, b.station_id, ct.coach_type_id,
       CAST(ROUND(ABS(b.distance_km - a.distance_km) *
            CASE ct.coach_type_id WHEN 1 THEN 700 WHEN 2 THEN 1200 ELSE 1000 END / 1000.0, 0) * 1000
            AS DECIMAL(12,0)),
       CAST('2026-01-01' AS DATE), NULL
FROM route_stations a
JOIN route_stations b ON b.route_id = a.route_id AND b.station_id <> a.station_id
CROSS JOIN coach_types ct
WHERE a.route_id = 1;

-- Đối tượng hành khách (có độ tuổi, giấy tờ, trạng thái)
INSERT INTO passenger_types (type_name, discount_percent, min_age, max_age, id_label, is_active) VALUES
 (N'Người lớn',      0,  NULL, NULL, NULL,      1),
 (N'Trẻ em',         25, NULL, 10,   NULL,      1),
 (N'Sinh viên',      10, NULL, NULL, N'Mã SV',  1),
 (N'Người cao tuổi', 15, 60,   NULL, N'CCCD',   1);
GO

/* ===================== 4. ĐƠN + VÉ MẪU (đã thanh toán) ===================== */
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
DECLARE @tid INT;

INSERT INTO bookings (booking_code, user_id, trip_type, contact_email, contact_phone, total_amount, [status], hold_expires_at)
VALUES ('BK000001', @uid, 'one_way', 'khach@example.com', '0900000002', @price, 'paid', DATEADD(MINUTE, 15, SYSDATETIME()));
SET @bid = SCOPE_IDENTITY();

INSERT INTO tickets (ticket_code, booking_id, trip_id, seat_id, from_station_id, to_station_id,
                     passenger_type_id, passenger_name, id_number, original_price, discount_amount, final_price, [status])
VALUES ('TK0000001', @bid, @trip, @seat, @han, @dad, @adult, N'Nguyễn Văn A', '012345678901', @price, 0, @price, 'paid');
SET @tid = SCOPE_IDENTITY();

INSERT INTO payments (booking_id, method, amount, transaction_ref, [status], paid_at)
VALUES (@bid, 'vnpay', @price, 'SIM-0001', 'success', SYSDATETIME());
GO

/* ===================== 5. KIỂM TRA NHANH ===================== */
SELECT 'users' AS bang, COUNT(*) AS so_dong FROM users UNION ALL
SELECT 'stations', COUNT(*) FROM stations UNION ALL
SELECT 'routes', COUNT(*) FROM routes UNION ALL
SELECT 'route_stations', COUNT(*) FROM route_stations UNION ALL
SELECT 'trains', COUNT(*) FROM trains UNION ALL
SELECT 'coach_types', COUNT(*) FROM coach_types UNION ALL
SELECT 'coaches', COUNT(*) FROM coaches UNION ALL
SELECT 'seats', COUNT(*) FROM seats UNION ALL
SELECT 'trips', COUNT(*) FROM trips UNION ALL
SELECT 'trip_stops', COUNT(*) FROM trip_stops UNION ALL
SELECT 'fares', COUNT(*) FROM fares UNION ALL
SELECT 'passenger_types', COUNT(*) FROM passenger_types UNION ALL
SELECT 'bookings', COUNT(*) FROM bookings UNION ALL
SELECT 'tickets', COUNT(*) FROM tickets UNION ALL
SELECT 'payments', COUNT(*) FROM payments UNION ALL
SELECT 'refunds', COUNT(*) FROM refunds;
GO
