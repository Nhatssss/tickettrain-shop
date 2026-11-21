-- ============================================================================
-- TrainTicketDB - TAO CSDL DAY DU (schema that + du lieu goc) cho Somee
-- Sinh tu LocalDB (LocalDB)\MSSQLLocalDB / TrainTicketDB - 2026-11-17
-- 18 bang. KHONG chua du lieu kiem thu.
-- Chay: sqlcmd -S <server> -U <user> -P <pwd> -d TrainTicketDB -i TrainTicketDB_somee.sql
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ===== Xoa theo thu tu phu thuoc (cho phep chay lai) =====
IF OBJECT_ID('dbo.refunds','U') IS NOT NULL DROP TABLE dbo.refunds;
IF OBJECT_ID('dbo.payments','U') IS NOT NULL DROP TABLE dbo.payments;
IF OBJECT_ID('dbo.tickets','U') IS NOT NULL DROP TABLE dbo.tickets;
IF OBJECT_ID('dbo.notifications','U') IS NOT NULL DROP TABLE dbo.notifications;
IF OBJECT_ID('dbo.bookings','U') IS NOT NULL DROP TABLE dbo.bookings;
IF OBJECT_ID('dbo.trip_stops','U') IS NOT NULL DROP TABLE dbo.trip_stops;
IF OBJECT_ID('dbo.trips','U') IS NOT NULL DROP TABLE dbo.trips;
IF OBJECT_ID('dbo.seats','U') IS NOT NULL DROP TABLE dbo.seats;
IF OBJECT_ID('dbo.coaches','U') IS NOT NULL DROP TABLE dbo.coaches;
IF OBJECT_ID('dbo.coach_types','U') IS NOT NULL DROP TABLE dbo.coach_types;
IF OBJECT_ID('dbo.fares','U') IS NOT NULL DROP TABLE dbo.fares;
IF OBJECT_ID('dbo.trains','U') IS NOT NULL DROP TABLE dbo.trains;
IF OBJECT_ID('dbo.route_stations','U') IS NOT NULL DROP TABLE dbo.route_stations;
IF OBJECT_ID('dbo.routes','U') IS NOT NULL DROP TABLE dbo.routes;
IF OBJECT_ID('dbo.stations','U') IS NOT NULL DROP TABLE dbo.stations;
IF OBJECT_ID('dbo.coupons','U') IS NOT NULL DROP TABLE dbo.coupons;
IF OBJECT_ID('dbo.passenger_types','U') IS NOT NULL DROP TABLE dbo.passenger_types;
IF OBJECT_ID('dbo.users','U') IS NOT NULL DROP TABLE dbo.users;
GO

-- ============================ BANG GOC ============================

CREATE TABLE dbo.users (
    user_id       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
    full_name     NVARCHAR(100) NOT NULL,
    email         NVARCHAR(100) NOT NULL,
    phone         VARCHAR(15)   NULL,
    password_hash NVARCHAR(255) NOT NULL,
    role          VARCHAR(10)   NOT NULL,
    status        VARCHAR(10)   NOT NULL,
    created_at    DATETIME2     NOT NULL CONSTRAINT DF_users_created_at DEFAULT SYSDATETIME(),
    locked_reason NVARCHAR(300) NULL,
    locked_at     DATETIME2     NULL,
    CONSTRAINT UQ_users_email UNIQUE (email)
);
GO

CREATE TABLE dbo.passenger_types (
    passenger_type_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_passenger_types PRIMARY KEY,
    type_name         NVARCHAR(30) NOT NULL,
    discount_percent  DECIMAL(5,2) NOT NULL,
    min_age           INT NULL,
    max_age           INT NULL,
    id_label          NVARCHAR(30) NULL,
    is_active         BIT NOT NULL CONSTRAINT DF_passenger_types_active DEFAULT 1
);
GO

CREATE TABLE dbo.stations (
    station_id   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_stations PRIMARY KEY,
    station_code VARCHAR(10)   NOT NULL,
    station_name NVARCHAR(100) NOT NULL,
    province     NVARCHAR(50) NULL,
    is_active    BIT NOT NULL CONSTRAINT DF_stations_active DEFAULT 1
);
GO

CREATE TABLE dbo.routes (
    route_id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_routes PRIMARY KEY,
    route_name       NVARCHAR(100) NOT NULL,
    start_station_id INT NOT NULL,
    end_station_id   INT NOT NULL,
    CONSTRAINT FK_routes_start FOREIGN KEY (start_station_id) REFERENCES dbo.stations(station_id),
    CONSTRAINT FK_routes_end   FOREIGN KEY (end_station_id)   REFERENCES dbo.stations(station_id)
);
GO

CREATE TABLE dbo.route_stations (
    route_id    INT NOT NULL,
    station_id  INT NOT NULL,
    stop_order  INT NOT NULL,
    distance_km DECIMAL(7,1) NOT NULL,
    CONSTRAINT PK_route_stations PRIMARY KEY (route_id, station_id),
    CONSTRAINT FK_route_stations_route   FOREIGN KEY (route_id)   REFERENCES dbo.routes(route_id),
    CONSTRAINT FK_route_stations_station FOREIGN KEY (station_id) REFERENCES dbo.stations(station_id)
);
GO

CREATE TABLE dbo.trains (
    train_id   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trains PRIMARY KEY,
    train_code VARCHAR(10) NOT NULL,
    route_id   INT NOT NULL,
    is_active  BIT NOT NULL CONSTRAINT DF_trains_active DEFAULT 1,
    CONSTRAINT FK_trains_route FOREIGN KEY (route_id) REFERENCES dbo.routes(route_id),
    CONSTRAINT UQ_trains_code UNIQUE (train_code)
);
GO

CREATE TABLE dbo.coach_types (
    coach_type_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_coach_types PRIMARY KEY,
    type_name     NVARCHAR(50) NOT NULL,
    capacity      INT NOT NULL,
    description   NVARCHAR(255) NULL
);
GO

CREATE TABLE dbo.coaches (
    coach_id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_coaches PRIMARY KEY,
    train_id      INT NOT NULL,
    coach_number  INT NOT NULL,
    coach_type_id INT NOT NULL,
    CONSTRAINT FK_coaches_train FOREIGN KEY (train_id)      REFERENCES dbo.trains(train_id),
    CONSTRAINT FK_coaches_type  FOREIGN KEY (coach_type_id) REFERENCES dbo.coach_types(coach_type_id)
);
GO

CREATE TABLE dbo.seats (
    seat_id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_seats PRIMARY KEY,
    coach_id       INT NOT NULL,
    seat_number    VARCHAR(5) NOT NULL,
    compartment_no INT NULL,
    berth_level    TINYINT NULL,
    CONSTRAINT FK_seats_coach FOREIGN KEY (coach_id) REFERENCES dbo.coaches(coach_id)
);
GO

CREATE TABLE dbo.fares (
    fare_id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_fares PRIMARY KEY,
    from_station_id INT NOT NULL,
    to_station_id   INT NOT NULL,
    coach_type_id   INT NOT NULL,
    base_price      DECIMAL(12,0) NOT NULL,
    effective_from  DATE NOT NULL,
    effective_to    DATE NULL,
    CONSTRAINT FK_fares_from  FOREIGN KEY (from_station_id) REFERENCES dbo.stations(station_id),
    CONSTRAINT FK_fares_to    FOREIGN KEY (to_station_id)   REFERENCES dbo.stations(station_id),
    CONSTRAINT FK_fares_type  FOREIGN KEY (coach_type_id)   REFERENCES dbo.coach_types(coach_type_id)
);
GO

CREATE TABLE dbo.coupons (
    coupon_id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_coupons PRIMARY KEY,
    code           VARCHAR(30) NOT NULL,
    description    NVARCHAR(200) NULL,
    discount_type  VARCHAR(10) NOT NULL,
    discount_value DECIMAL(18,2) NOT NULL,
    max_discount   DECIMAL(18,2) NULL,
    min_order      DECIMAL(18,2) NOT NULL,
    max_uses       INT NULL,
    used_count     INT NOT NULL,
    per_user_limit INT NOT NULL,
    valid_from     DATE NOT NULL,
    valid_to       DATE NULL,
    is_active      BIT NOT NULL,
    created_at     DATETIME2 NOT NULL,
    CONSTRAINT UQ_coupons_code UNIQUE (code)
);
GO

CREATE TABLE dbo.trips (
    trip_id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_trips PRIMARY KEY,
    train_id       INT NOT NULL,
    departure_date DATE NOT NULL,
    status         VARCHAR(15) NOT NULL,
    delay_minutes  INT NOT NULL,
    cancel_reason  NVARCHAR(300) NULL,
    CONSTRAINT FK_trips_train FOREIGN KEY (train_id) REFERENCES dbo.trains(train_id)
);
GO

CREATE TABLE dbo.trip_stops (
    trip_id        INT NOT NULL,
    station_id     INT NOT NULL,
    stop_order     INT NOT NULL,
    arrival_time   DATETIME2 NULL,
    departure_time DATETIME2 NULL,
    CONSTRAINT PK_trip_stops PRIMARY KEY (trip_id, station_id),
    CONSTRAINT FK_trip_stops_trip    FOREIGN KEY (trip_id)    REFERENCES dbo.trips(trip_id),
    CONSTRAINT FK_trip_stops_station FOREIGN KEY (station_id) REFERENCES dbo.stations(station_id)
);
GO

CREATE TABLE dbo.bookings (
    booking_id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bookings PRIMARY KEY,
    booking_code    VARCHAR(12) NOT NULL,
    user_id         INT NOT NULL,
    trip_type       VARCHAR(10) NOT NULL,
    contact_email   NVARCHAR(100) NOT NULL,
    contact_phone   VARCHAR(15) NOT NULL,
    total_amount    DECIMAL(12,0) NOT NULL,
    status          VARCHAR(15) NOT NULL,
    hold_expires_at DATETIME2 NOT NULL,
    created_at      DATETIME2 NOT NULL,
    coupon_id       INT NULL,
    discount_amount DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_bookings_user   FOREIGN KEY (user_id)   REFERENCES dbo.users(user_id),
    CONSTRAINT FK_bookings_coupon FOREIGN KEY (coupon_id) REFERENCES dbo.coupons(coupon_id)
);
GO

CREATE TABLE dbo.tickets (
    ticket_id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_tickets PRIMARY KEY,
    ticket_code       VARCHAR(15) NOT NULL,
    booking_id        INT NOT NULL,
    trip_id           INT NOT NULL,
    seat_id           INT NOT NULL,
    from_station_id   INT NOT NULL,
    to_station_id     INT NOT NULL,
    leg               VARCHAR(10) NOT NULL,
    passenger_type_id INT NOT NULL,
    passenger_name    NVARCHAR(100) NOT NULL,
    id_number         VARCHAR(20) NULL,
    original_price    DECIMAL(12,0) NOT NULL,
    discount_amount   DECIMAL(12,0) NOT NULL,
    final_price       DECIMAL(12,0) NOT NULL,
    status            VARCHAR(15) NOT NULL,
    created_at        DATETIME2 NOT NULL,
    CONSTRAINT FK_tickets_booking FOREIGN KEY (booking_id)        REFERENCES dbo.bookings(booking_id),
    CONSTRAINT FK_tickets_trip    FOREIGN KEY (trip_id)           REFERENCES dbo.trips(trip_id),
    CONSTRAINT FK_tickets_seat    FOREIGN KEY (seat_id)           REFERENCES dbo.seats(seat_id),
    CONSTRAINT FK_tickets_from    FOREIGN KEY (from_station_id)   REFERENCES dbo.stations(station_id),
    CONSTRAINT FK_tickets_to      FOREIGN KEY (to_station_id)     REFERENCES dbo.stations(station_id),
    CONSTRAINT FK_tickets_ptype   FOREIGN KEY (passenger_type_id) REFERENCES dbo.passenger_types(passenger_type_id)
);
GO

CREATE TABLE dbo.payments (
    payment_id      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_payments PRIMARY KEY,
    booking_id      INT NOT NULL,
    method          VARCHAR(15) NOT NULL,
    amount          DECIMAL(12,0) NOT NULL,
    transaction_ref VARCHAR(100) NULL,
    status          VARCHAR(15) NOT NULL,
    created_at      DATETIME2 NOT NULL,
    paid_at         DATETIME2 NULL,
    CONSTRAINT FK_payments_booking FOREIGN KEY (booking_id) REFERENCES dbo.bookings(booking_id)
);
GO

CREATE TABLE dbo.refunds (
    refund_id     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_refunds PRIMARY KEY,
    ticket_id     INT NOT NULL,
    payment_id    INT NULL,
    reason        NVARCHAR(255) NULL,
    refund_fee    DECIMAL(12,0) NOT NULL,
    refund_amount DECIMAL(12,0) NOT NULL,
    status        VARCHAR(15) NOT NULL,
    requested_at  DATETIME2 NOT NULL,
    processed_by  INT NULL,
    processed_at  DATETIME2 NULL,
    CONSTRAINT FK_refunds_ticket    FOREIGN KEY (ticket_id)    REFERENCES dbo.tickets(ticket_id),
    CONSTRAINT FK_refunds_payment   FOREIGN KEY (payment_id)   REFERENCES dbo.payments(payment_id),
    CONSTRAINT FK_refunds_processed FOREIGN KEY (processed_by) REFERENCES dbo.users(user_id)
);
GO

CREATE TABLE dbo.notifications (
    notification_id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_notifications PRIMARY KEY,
    user_id         INT NOT NULL,
    type            VARCHAR(30) NOT NULL,
    title           NVARCHAR(200) NOT NULL,
    body            NVARCHAR(600) NULL,
    link_url        NVARCHAR(300) NULL,
    ref_trip_id     INT NULL,
    ref_coupon_code VARCHAR(30) NULL,
    is_read         BIT NOT NULL CONSTRAINT DF_notifications_read DEFAULT 0,
    created_at      DATETIME2 NOT NULL CONSTRAINT DF_notifications_created DEFAULT SYSDATETIME(),
    CONSTRAINT FK_notifications_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
);
GO
CREATE INDEX IX_notifications_user_unread ON dbo.notifications(user_id, is_read);
GO
CREATE UNIQUE INDEX UX_notifications_trip_type ON dbo.notifications(ref_trip_id, type)
    WHERE ref_trip_id IS NOT NULL;
GO

-- ============================ DU LIEU GOC ============================

SET IDENTITY_INSERT dbo.users ON;
INSERT INTO dbo.users (user_id, full_name, email, phone, password_hash, role, status) VALUES
(1,    N'Quản trị viên', 'admin@example.com',      '0900000001', '$2a$12$ZirZwQ4WNX0xvWALXhlECeq9X0IAyBKQ.ezysiu50XCoJ2Zz6GZSq', 'admin',    'active'),
(2,    N'Nguyễn Văn A',  'khach@example.com',      '0900000002', '$2a$12$ZirZwQ4WNX0xvWALXhlECeq9X0IAyBKQ.ezysiu50XCoJ2Zz6GZSq', 'customer', 'active'),
(1002, N'nhat',          'nhatmllamk2@gmail.com',  '0362353963', '$2a$12$ZirZwQ4WNX0xvWALXhlECeq9X0IAyBKQ.ezysiu50XCoJ2Zz6GZSq', 'customer', 'active');
SET IDENTITY_INSERT dbo.users OFF;
GO

SET IDENTITY_INSERT dbo.passenger_types ON;
INSERT INTO dbo.passenger_types (passenger_type_id, type_name, discount_percent, min_age, max_age, id_label, is_active) VALUES
(1, N'Người lớn',       0.00, 16, NULL, NULL, 1),
(2, N'Trẻ em',         50.00,  0,   5, N'Mã giảm 50%%', 1),
(3, N'Sinh viên',      20.00,  6,  22, N'Thẻ sinh viên', 1),
(4, N'Người cao tuổi', 30.00, 60, 130, N'CCCD', 1);
SET IDENTITY_INSERT dbo.passenger_types OFF;
GO

SET IDENTITY_INSERT dbo.stations ON;
INSERT INTO dbo.stations (station_id, station_code, station_name, province, is_active) VALUES
(1, 'HN',  N'Ga Hà Nội',     N'Hà Nội', 1),
(2, 'VIN', N'Ga Vinh',       N'Nghệ An', 1),
(3, 'HUE', N'Ga Huế',        N'Thừa Thiên Huế', 1),
(4, 'DN',  N'Ga Đà Nẵng',    N'Đà Nẵng', 1),
(5, 'NT',  N'Ga Nha Trang',  N'Khánh Hòa', 1),
(6, 'SG',  N'Ga Sài Gòn',    N'TP. Hồ Chí Minh', 1);
SET IDENTITY_INSERT dbo.stations OFF;
GO

SET IDENTITY_INSERT dbo.routes ON;
INSERT INTO dbo.routes (route_id, route_name, start_station_id, end_station_id) VALUES
(1, N'Hà Nội - Sài Gòn', 1, 6),
(2, N'Hà Nội - Đà Nẵng', 1, 4);
SET IDENTITY_INSERT dbo.routes OFF;
GO

SET IDENTITY_INSERT dbo.trains ON;
INSERT INTO dbo.trains (train_id, train_code, route_id, is_active) VALUES
(1, 'SE1', 1, 1),
(2, 'SE2', 1, 1);
SET IDENTITY_INSERT dbo.trains OFF;
GO

SET IDENTITY_INSERT dbo.coach_types ON;
INSERT INTO dbo.coach_types (coach_type_id, type_name, capacity, description) VALUES
(1, N'Ghế ngồi mềm', 40, N'Ghế ngồi mềm điều hòa'),
(2, N'Giường nằm K4', 32, N'Khoang 4 giường'),
(3, N'Giường nằm K6', 42, N'Khoang 6 giường');
SET IDENTITY_INSERT dbo.coach_types OFF;
GO

SET IDENTITY_INSERT dbo.coupons ON;
INSERT INTO dbo.coupons (coupon_id, code, description, discount_type, discount_value, max_discount, min_order, max_uses, used_count, per_user_limit, valid_from, valid_to, is_active, created_at) VALUES
(1, 'GIFF999', N'Giảm 5%% cho đơn hàng',   'percent',  5.00, NULL, 0.00, NULL, 0, 1, CONVERT(date, GETDATE()), NULL, 1, SYSDATETIME()),
(2, 'GIFF555', N'Giảm 10%% cho đơn hàng',  'percent', 10.00, NULL, 0.00, NULL, 0, 1, CONVERT(date, GETDATE()), NULL, 1, SYSDATETIME());
SET IDENTITY_INSERT dbo.coupons OFF;
GO

PRINT '=== SCHEMA + DU LIEU GOC XONG ===';
GO
