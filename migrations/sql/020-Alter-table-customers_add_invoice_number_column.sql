--liquibase formatted sql

-- changeset Aldis:20
-- comment: Alter customers table add invoice number column
ALTER TABLE customers ADD COLUMN invoice_number INT DEFAULT 0;

-- rollback ALTER TABLE customers DROP COLUMN invoice_number;
