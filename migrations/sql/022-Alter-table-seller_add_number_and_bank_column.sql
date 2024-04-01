--liquibase formatted sql

-- changeset Aldis:22
-- comment: Alter sellers table add company number column
ALTER TABLE sellers ADD COLUMN company_number INT;
ALTER TABLE sellers ADD COLUMN bank_name character varying(50);
ALTER TABLE sellers ADD COLUMN bank_number character varying(50);

-- rollback ALTER TABLE sellers DROP COLUMN company_number,bank_name,bank_number;
