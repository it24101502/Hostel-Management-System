USE Hostel_Management_System;

-- Remove the existing status constraint temporarily.
ALTER TABLE fee_invoices
    DROP CHECK chk_fee_invoices_status;

-- Convert existing unpaid states to the new shared UNPAID state.
UPDATE fee_invoices
SET status = 'UNPAID'
WHERE status IN
(
    'PENDING',
    'PARTIALLY_PAID'
);

-- Newly generated invoices begin as unpaid.
ALTER TABLE fee_invoices
    ALTER COLUMN status
    SET DEFAULT 'UNPAID';

-- Enforce the new invoice status values.
ALTER TABLE fee_invoices
    ADD CONSTRAINT chk_fee_invoices_status
    CHECK
    (
        status IN
        (
            'UNPAID',
            'PAID',
            'OVERDUE',
            'CANCELLED'
        )
    );