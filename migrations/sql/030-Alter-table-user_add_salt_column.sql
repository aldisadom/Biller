--liquibase formatted sql

-- changeset Aldis:30
-- comment: Alter users table adding password salt
ALTER TABLE users ADD COLUMN salt character varying(255);

UPDATE users
SET salt = '$2a$10$TaqaG43aDC1Ai75moe.ENO';

ALTER TABLE users ALTER COLUMN salt SET NOT NULL;
-- rollback ALTER TABLE users DROP COLUMN salt;