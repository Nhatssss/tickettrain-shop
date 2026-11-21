-- ============================================================================
-- Sinh chuyen tau cho cac ngay tuong lai bang cach dich nguyen lich trinh
-- cua chuyen mau (SE1 trip 15, SE2 trip 30) sang ngay moi.
-- Cach lam: khoang cach tu gio khoi hanh -> gio tung ga la KHONG DOI.
-- ============================================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRAN;

-- 1. Xoa cac chuyen sinh loi truoc do (chua ban ve nao)
DELETE ts
FROM trip_stops ts
JOIN trips tr ON tr.trip_id = ts.trip_id
WHERE tr.departure_date >= '2026-11-18'
  AND NOT EXISTS (SELECT 1 FROM tickets tk WHERE tk.trip_id = tr.trip_id);

DELETE tr
FROM trips tr
WHERE tr.departure_date >= '2026-11-18'
  AND NOT EXISTS (SELECT 1 FROM tickets tk WHERE tk.trip_id = tr.trip_id);

-- 2. Tao lai cho tung ngay
DECLARE @d DATE = '2026-11-18';
DECLARE @end DATE = '2026-11-30';
DECLARE @src INT, @newId INT, @trainId INT, @base DATETIME, @shift INT;

WHILE @d <= @end
BEGIN
    -- SE1: chuyen mau 15
    SET @src = 15;
    SET @trainId = (SELECT train_id FROM trips WHERE trip_id = @src);
    SET @base = (SELECT departure_time FROM trip_stops WHERE trip_id = @src AND stop_order = 1);
    SET @shift = DATEDIFF(MINUTE, CAST(@base AS DATE), CAST(@d AS DATE)) * 0
               + DATEDIFF(DAY, CAST(@base AS DATE), CAST(@d AS DATE));
    -- tinh lai bang phut cho chinh xac: gio moi = gio goc + (ngay moi - ngay goc)
    DECLARE @deltaMin INT = DATEDIFF(MINUTE, @base, DATEADD(DAY, DATEDIFF(DAY, CAST(@base AS DATE), CAST(@d AS DATE)), @base));

    INSERT INTO trips (train_id, departure_date, [status]) VALUES (@trainId, @d, 'scheduled');
    SET @newId = SCOPE_IDENTITY();

    INSERT INTO trip_stops (trip_id, station_id, stop_order, arrival_time, departure_time)
    SELECT @newId, station_id, stop_order,
           CASE WHEN arrival_time   IS NULL THEN NULL ELSE DATEADD(MINUTE, @deltaMin, arrival_time)   END,
           CASE WHEN departure_time IS NULL THEN NULL ELSE DATEADD(MINUTE, @deltaMin, departure_time) END
    FROM trip_stops WHERE trip_id = @src;

    -- SE2: chuyen mau 30
    SET @src = 30;
    SET @trainId = (SELECT train_id FROM trips WHERE trip_id = @src);
    SET @base = (SELECT departure_time FROM trip_stops WHERE trip_id = @src AND stop_order = 1);
    SET @deltaMin = DATEDIFF(MINUTE, @base, DATEADD(DAY, DATEDIFF(DAY, CAST(@base AS DATE), CAST(@d AS DATE)), @base));

    INSERT INTO trips (train_id, departure_date, [status]) VALUES (@trainId, @d, 'scheduled');
    SET @newId = SCOPE_IDENTITY();

    INSERT INTO trip_stops (trip_id, station_id, stop_order, arrival_time, departure_time)
    SELECT @newId, station_id, stop_order,
           CASE WHEN arrival_time   IS NULL THEN NULL ELSE DATEADD(MINUTE, @deltaMin, arrival_time)   END,
           CASE WHEN departure_time IS NULL THEN NULL ELSE DATEADD(MINUTE, @deltaMin, departure_time) END
    FROM trip_stops WHERE trip_id = @src;

    SET @d = DATEADD(DAY, 1, @d);
END

COMMIT;

-- 3. Ket qua
SELECT COUNT(*) AS tong_chuyen, MIN(departure_date) AS tu_ngay, MAX(departure_date) AS den_ngay FROM trips;
