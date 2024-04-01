--liquibase formatted sql

-- changeset Aldis:24
-- comment: Alter sellers table change company number to string
ALTER TABLE sellers DROP COLUMN company_number;
ALTER TABLE sellers ADD COLUMN company_number character varying(50);
