SET IDENTITY_INSERT dbo.payments ON;
INSERT INTO dbo.payments (payment_id, booking_id, method, amount, transaction_ref, status, created_at, paid_at) VALUES (1, 1, N'vnpay', 554000, N'SIM-0001', N'success', '2026-10-31 07:54:26.7245794', '2026-10-31 07:54:26.7245794');
INSERT INTO dbo.payments (payment_id, booking_id, method, amount, transaction_ref, status, created_at, paid_at) VALUES (2, 3002, N'bank_qr', 0, N'SIM-88EB52E02F', N'success', '2026-10-31 09:20:30.6435478', '2026-10-31 09:20:30.6435478');
INSERT INTO dbo.payments (payment_id, booking_id, method, amount, transaction_ref, status, created_at, paid_at) VALUES (1002, 4002, N'momo', 223000, N'SIM-C73FA5F1E5', N'success', '2026-10-31 09:23:58.1424703', '2026-10-31 09:23:58.1424703');
INSERT INTO dbo.payments (payment_id, booking_id, method, amount, transaction_ref, status, created_at, paid_at) VALUES (1003, 4003, N'momo', 690750, N'SIM-8977398410', N'success', '2026-11-06 14:38:29.4922007', '2026-11-06 14:38:29.4922007');
SET IDENTITY_INSERT dbo.payments OFF;
GO
