--liquibase formatted sql

-- changeset Aldis:29
-- comment: Alter users table, change column number (string) to invoice_number(integer)
-- comment: Create users table

ALTER TABLE users DROP CONSTRAINT email;
ALTER TABLE users ADD CONSTRAINT constrain_email UNIQUE (email);

-- rollback ALTER TABLE users DROP CONSTRAINT constrain_email;
-- rollback ALTER TABLE users add CONSTRAINT email UNIQUE (name);