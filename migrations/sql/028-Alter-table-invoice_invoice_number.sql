--liquibase formatted sql

-- changeset Aldis:28
-- comment: Alter invoices table, change column number (string) to invoice_number(integer)
-- comment: Create invoices table

ALTER TABLE invoices RENAME COLUMN number to invoice_number;
ALTER TABLE invoices ALTER COLUMN invoice_number TYPE integer USING invoice_number::integer;

-- rollback ALTER TABLE invoices RENAME COLUMN invoice_number to number TYPE string;
