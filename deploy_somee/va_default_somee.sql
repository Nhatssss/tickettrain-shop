-- Vá DEFAULT CONSTRAINT cho TrainTicketDB (Somee) de khop LocalDB.
-- Chay 1 lan sau khi tao schema: sqlcmd -S ... -i va_default_somee.sql
SET QUOTED_IDENTIFIER ON;
IF OBJECT_ID('DF_bookings_trip_type','D') IS NULL ALTER TABLE bookings ADD CONSTRAINT DF_bookings_trip_type DEFAULT 'one_way' FOR trip_type;
IF OBJECT_ID('DF_bookings_total','D') IS NULL ALTER TABLE bookings ADD CONSTRAINT DF_bookings_total DEFAULT 0 FOR total_amount;
IF OBJECT_ID('DF_bookings_status','D') IS NULL ALTER TABLE bookings ADD CONSTRAINT DF_bookings_status DEFAULT 'holding' FOR [status];
IF OBJECT_ID('DF_bookings_created','D') IS NULL ALTER TABLE bookings ADD CONSTRAINT DF_bookings_created DEFAULT SYSDATETIME() FOR created_at;
IF OBJECT_ID('DF_bookings_discount','D') IS NULL ALTER TABLE bookings ADD CONSTRAINT DF_bookings_discount DEFAULT 0 FOR discount_amount;
IF OBJECT_ID('DF_coupons_min_order','D') IS NULL ALTER TABLE coupons ADD CONSTRAINT DF_coupons_min_order DEFAULT 0 FOR min_order;
IF OBJECT_ID('DF_coupons_used_count','D') IS NULL ALTER TABLE coupons ADD CONSTRAINT DF_coupons_used_count DEFAULT 0 FOR used_count;
IF OBJECT_ID('DF_coupons_per_user','D') IS NULL ALTER TABLE coupons ADD CONSTRAINT DF_coupons_per_user DEFAULT 1 FOR per_user_limit;
IF OBJECT_ID('DF_coupons_active','D') IS NULL ALTER TABLE coupons ADD CONSTRAINT DF_coupons_active DEFAULT 1 FOR is_active;
IF OBJECT_ID('DF_coupons_created','D') IS NULL ALTER TABLE coupons ADD CONSTRAINT DF_coupons_created DEFAULT SYSDATETIME() FOR created_at;
IF OBJECT_ID('DF_payments_status','D') IS NULL ALTER TABLE payments ADD CONSTRAINT DF_payments_status DEFAULT 'pending' FOR [status];
IF OBJECT_ID('DF_payments_created','D') IS NULL ALTER TABLE payments ADD CONSTRAINT DF_payments_created DEFAULT SYSDATETIME() FOR created_at;
IF OBJECT_ID('DF_refunds_fee','D') IS NULL ALTER TABLE refunds ADD CONSTRAINT DF_refunds_fee DEFAULT 0 FOR refund_fee;
IF OBJECT_ID('DF_refunds_status','D') IS NULL ALTER TABLE refunds ADD CONSTRAINT DF_refunds_status DEFAULT 'requested' FOR [status];
IF OBJECT_ID('DF_refunds_requested','D') IS NULL ALTER TABLE refunds ADD CONSTRAINT DF_refunds_requested DEFAULT SYSDATETIME() FOR requested_at;
IF OBJECT_ID('DF_tickets_leg','D') IS NULL ALTER TABLE tickets ADD CONSTRAINT DF_tickets_leg DEFAULT 'outbound' FOR leg;
IF OBJECT_ID('DF_tickets_discount','D') IS NULL ALTER TABLE tickets ADD CONSTRAINT DF_tickets_discount DEFAULT 0 FOR discount_amount;
IF OBJECT_ID('DF_tickets_status','D') IS NULL ALTER TABLE tickets ADD CONSTRAINT DF_tickets_status DEFAULT 'holding' FOR [status];
IF OBJECT_ID('DF_tickets_created','D') IS NULL ALTER TABLE tickets ADD CONSTRAINT DF_tickets_created DEFAULT SYSDATETIME() FOR created_at;
IF OBJECT_ID('DF_trips_status','D') IS NULL ALTER TABLE trips ADD CONSTRAINT DF_trips_status DEFAULT 'scheduled' FOR [status];
IF OBJECT_ID('DF_trips_delay','D') IS NULL ALTER TABLE trips ADD CONSTRAINT DF_trips_delay DEFAULT 0 FOR delay_minutes;
IF OBJECT_ID('DF_users_role','D') IS NULL ALTER TABLE users ADD CONSTRAINT DF_users_role DEFAULT 'customer' FOR [role];
IF OBJECT_ID('DF_users_status','D') IS NULL ALTER TABLE users ADD CONSTRAINT DF_users_status DEFAULT 'active' FOR [status];
SELECT CONCAT('Tong default constraint: ', COUNT(*)) AS ket_qua FROM sys.default_constraints;