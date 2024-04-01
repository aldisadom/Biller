--liquibase formatted sql

-- changeset Aldis:23
-- comment: Alter customers table change company number to string
ALTER TABLE customers DROP COLUMN company_number;
ALTER TABLE customers ADD COLUMN company_number character varying(50);

