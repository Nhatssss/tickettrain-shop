-- ============================================================================
-- Don dep du lieu kiem thu (chay 1 lan, co backup truoc do)
--   - Xoa 4 don test: 4004, 4005, 4006, 5004 (va ve/thanh toan/hoan tien cua chung)
--   - Tra chuyen 2002 ve 'scheduled', delay_minutes = 0, cancel_reason = NULL
--   - Xoa thong bao sinh ra boi kiem thu (ref_trip_id = 2002 + thong bao test he thong)
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRAN;

DECLARE @b TABLE (booking_id INT PRIMARY KEY);
INSERT INTO @b VALUES (4004), (4005), (4006), (5004);

DECLARE @t TABLE (ticket_id INT PRIMARY KEY);
INSERT INTO @t SELECT ticket_id FROM tickets WHERE booking_id IN (SELECT booking_id FROM @b);

-- 1. Hoan tien cua cac ve test
DELETE FROM refunds WHERE ticket_id IN (SELECT ticket_id FROM @t);

-- 2. Thanh toan cua cac don test
DELETE FROM payments WHERE booking_id IN (SELECT booking_id FROM @b);

-- 3. Ve test
DELETE FROM tickets WHERE ticket_id IN (SELECT ticket_id FROM @t);

-- 4. Don test
DELETE FROM bookings WHERE booking_id IN (SELECT booking_id FROM @b);

-- 5. Thong bao do kiem thu sinh ra (huy chuyen 2002 + push thu he thong)
DELETE FROM notifications
WHERE ref_trip_id = 2002
   OR (type = 'system' AND title LIKE N'%Kiểm tra thông báo real-time%');

-- 6. Tra chuyen 2002 ve trang thai binh thuong
UPDATE trips
   SET [status] = 'scheduled', delay_minutes = 0, cancel_reason = NULL
 WHERE trip_id = 2002;

COMMIT;

-- ===== Kiem tra sau khi don =====
SELECT 'trips_cancelled' AS k, COUNT(*) AS v FROM trips WHERE [status] = 'cancelled'
UNION ALL SELECT 'trips_delayed', COUNT(*) FROM trips WHERE ISNULL(delay_minutes,0) > 0
UNION ALL SELECT 'tickets', COUNT(*) FROM tickets
UNION ALL SELECT 'bookings', COUNT(*) FROM bookings
UNION ALL SELECT 'payments', COUNT(*) FROM payments
UNION ALL SELECT 'refunds', COUNT(*) FROM refunds
UNION ALL SELECT 'notifications', COUNT(*) FROM notifications
UNION ALL SELECT 'coupons', COUNT(*) FROM coupons;

SELECT trip_id, [status], delay_minutes, ISNULL(cancel_reason, '(null)') AS reason FROM trips WHERE trip_id = 2002;

SELECT b.booking_id, b.booking_code, b.[status], b.total_amount,
       (SELECT COUNT(*) FROM tickets t WHERE t.booking_id = b.booking_id) AS ve
FROM bookings b ORDER BY b.booking_id;
