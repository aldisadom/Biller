--liquibase formatted sql

-- changeset Aldis:30
-- comment: Alter customers table add invoice number column
ALTER TABLE users ADD COLUMN salt character varying(255)  NOT NULL;

-- rollback ALTER TABLE users DROP COLUMN salt;