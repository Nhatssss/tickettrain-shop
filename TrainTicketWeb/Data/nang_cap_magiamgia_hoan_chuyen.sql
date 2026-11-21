/* ============================================================================
   NANG CAP CSDL: ma giam gia + hoan chuyen + khoa tai khoan
   Chay tren TrainTicketDB. An toan de chay lai (idempotent).

   Noi dung:
     1. Bang coupons            -> ma giam gia do admin tao
     2. bookings.coupon_id      -> ma da ap cho don
     3. bookings.discount_amount-> so tien duoc giam
     4. trips.delay_minutes     -> so phut hoan chuyen
     5. trips.cancel_reason     -> ly do huy/hoan
     6. users.locked_reason     -> ly do khoa tai khoan
     7. users.locked_at         -> thoi diem khoa (NULL = dang mo)
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------- 1. Bang coupons ---------- */
IF OBJECT_ID('dbo.coupons', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.coupons
    (
        coupon_id       INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        code            VARCHAR(30)   NOT NULL,
        description     NVARCHAR(200) NULL,
        discount_type   VARCHAR(10)   NOT NULL,      -- 'percent' | 'amount'
        discount_value  DECIMAL(18,2) NOT NULL,
        max_discount    DECIMAL(18,2) NULL,          -- tran giam (chi dung cho 'percent')
        min_order       DECIMAL(18,2) NOT NULL DEFAULT 0,
        max_uses        INT           NULL,          -- NULL = khong gioi han
        used_count      INT           NOT NULL DEFAULT 0,
        per_user_limit  INT           NOT NULL DEFAULT 1,
        valid_from      DATE          NOT NULL,
        valid_to        DATE          NULL,          -- NULL = khong het han
        is_active       BIT           NOT NULL DEFAULT 1,
        created_at      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT CK_coupons_type CHECK (discount_type IN ('percent','amount')),
        CONSTRAINT CK_coupons_value CHECK (discount_value > 0)
    );
    CREATE UNIQUE INDEX UX_coupons_code ON dbo.coupons(code);
    PRINT 'Da tao bang coupons.';
END
ELSE
    PRINT 'Bang coupons da ton tai - bo qua.';
GO

/* ---------- 2. bookings.coupon_id ---------- */
IF COL_LENGTH('dbo.bookings', 'coupon_id') IS NULL
BEGIN
    ALTER TABLE dbo.bookings ADD coupon_id INT NULL;
    ALTER TABLE dbo.bookings ADD CONSTRAINT FK_bookings_coupon
        FOREIGN KEY (coupon_id) REFERENCES dbo.coupons(coupon_id);
    PRINT 'Da them cot bookings.coupon_id.';
END
ELSE
    PRINT 'Cot bookings.coupon_id da co - bo qua.';
GO

/* ---------- 3. bookings.discount_amount ---------- */
IF COL_LENGTH('dbo.bookings', 'discount_amount') IS NULL
BEGIN
    ALTER TABLE dbo.bookings ADD discount_amount DECIMAL(18,2) NOT NULL
        CONSTRAINT DF_bookings_discount DEFAULT 0;
    PRINT 'Da them cot bookings.discount_amount.';
END
ELSE
    PRINT 'Cot bookings.discount_amount da co - bo qua.';
GO

/* ---------- 4. trips.delay_minutes ---------- */
IF COL_LENGTH('dbo.trips', 'delay_minutes') IS NULL
BEGIN
    ALTER TABLE dbo.trips ADD delay_minutes INT NOT NULL
        CONSTRAINT DF_trips_delay DEFAULT 0;
    PRINT 'Da them cot trips.delay_minutes.';
END
ELSE
    PRINT 'Cot trips.delay_minutes da co - bo qua.';
GO

/* ---------- 5. trips.cancel_reason ---------- */
IF COL_LENGTH('dbo.trips', 'cancel_reason') IS NULL
BEGIN
    ALTER TABLE dbo.trips ADD cancel_reason NVARCHAR(300) NULL;
    PRINT 'Da them cot trips.cancel_reason.';
END
ELSE
    PRINT 'Cot trips.cancel_reason da co - bo qua.';
GO

/* ---------- 6. users.locked_reason ---------- */
IF COL_LENGTH('dbo.users', 'locked_reason') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD locked_reason NVARCHAR(300) NULL;
    PRINT 'Da them cot users.locked_reason.';
END
ELSE
    PRINT 'Cot users.locked_reason da co - bo qua.';
GO

/* ---------- 7. users.locked_at ---------- */
IF COL_LENGTH('dbo.users', 'locked_at') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD locked_at DATETIME2 NULL;
    PRINT 'Da them cot users.locked_at.';
END
ELSE
    PRINT 'Cot users.locked_at da co - bo qua.';
GO

/* ---------- 8. Ma GIFF999: giam 5%, khong tran, khong don toi thieu ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.coupons WHERE code = 'GIFF999')
BEGIN
    INSERT INTO dbo.coupons (code, description, discount_type, discount_value, max_discount,
                             min_order, max_uses, per_user_limit, valid_from, valid_to, is_active)
    VALUES ('GIFF999', N'Ma giam gia 5% cho moi don hang',
            'percent', 5, NULL, 0, NULL, 1, CAST(SYSDATETIME() AS DATE), NULL, 1);
    PRINT 'Da tao ma GIFF999 (giam 5%).';
END
ELSE
    PRINT 'Ma GIFF999 da ton tai - bo qua.';
GO

PRINT '=== HOAN TAT NANG CAP ===';
GO
