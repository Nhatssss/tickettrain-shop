/* ============================================================================
   NANG CAP CSDL: thong bao (notifications) + ma GIFF555
   Chay tren TrainTicketDB. An toan de chay lai (idempotent).

   Noi dung:
     1. Bang notifications  -> thong bao day cho tung khach (SignalR + hop thu)
     2. Ma GIFF555          -> giam 10%
   ============================================================================ */

SET NOCOUNT ON;
GO

/* ---------- 1. Bang notifications ---------- */
IF OBJECT_ID('dbo.notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notifications
    (
        notification_id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        user_id         INT            NOT NULL,
        type            VARCHAR(30)    NOT NULL,   -- trip_delayed | trip_cancelled | refund | coupon | system
        title           NVARCHAR(200)  NOT NULL,
        body            NVARCHAR(600)  NULL,
        link_url        NVARCHAR(300)  NULL,
        ref_trip_id     INT            NULL,        -- de tranh gui trung cho cung 1 chuyen
        ref_coupon_code VARCHAR(30)    NULL,
        is_read         BIT            NOT NULL DEFAULT 0,
        created_at      DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT FK_notifications_user FOREIGN KEY (user_id) REFERENCES dbo.users(user_id)
    );

    CREATE INDEX IX_notifications_user_unread
        ON dbo.notifications(user_id, is_read, created_at DESC);

    -- Mot khach chi nhan 1 thong bao hoan chuyen cho 1 chuyen tau
    CREATE UNIQUE INDEX UX_notifications_trip_type
        ON dbo.notifications(user_id, type, ref_trip_id)
        WHERE ref_trip_id IS NOT NULL;

    PRINT 'Da tao bang notifications.';
END
ELSE
    PRINT 'Bang notifications da ton tai - bo qua.';
GO

/* ---------- 2. Ma GIFF555: giam 10% ---------- */
IF NOT EXISTS (SELECT 1 FROM dbo.coupons WHERE code = 'GIFF555')
BEGIN
    INSERT INTO dbo.coupons (code, description, discount_type, discount_value, max_discount,
                             min_order, max_uses, per_user_limit, valid_from, valid_to, is_active)
    VALUES ('GIFF555', N'Ma giam gia 10% cho moi don hang',
            'percent', 10, NULL, 0, NULL, 1, CAST(SYSDATETIME() AS DATE), NULL, 1);
    PRINT 'Da tao ma GIFF555 (giam 10%).';
END
ELSE
    PRINT 'Ma GIFF555 da ton tai - bo qua.';
GO

PRINT '=== HOAN TAT ===';
GO
