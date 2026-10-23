--liquibase formatted sql

-- changeset Aldis:32
-- comment: Alter invoices table, status NOT NULL with default Created (10)
UPDATE invoices SET status = 10 WHERE status IS NULL;

ALTER TABLE invoices ALTER COLUMN status SET DEFAULT 10;

ALTER TABLE invoices ALTER COLUMN status SET NOT NULL;

-- rollback ALTER TABLE invoices ALTER COLUMN status DROP NOT NULL; ALTER TABLE invoices ALTER COLUMN status DROP DEFAULT;
