--liquibase formatted sql

-- changeset Aldis:21
-- comment: Alter customers table add company number column
ALTER TABLE customers ADD COLUMN company_number INT;

-- rollback ALTER TABLE customers DROP COLUMN company_number;
