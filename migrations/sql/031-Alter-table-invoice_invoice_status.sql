--liquibase formatted sql

-- changeset Aldis:31
-- comment: Alter invoices table, add status column
-- comment: Create invoices table

ALTER TABLE invoices ADD COLUMN status INT;

UPDATE invoices SET status = 0;

-- rollback ALTER TABLE invoices DROP COLUMN status;
