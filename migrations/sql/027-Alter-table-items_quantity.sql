--liquibase formatted sql

-- changeset Aldis:27
-- comment: Alter items table, change quantity from number to decimal
-- comment: Create items table

ALTER TABLE items ALTER COLUMN quantity TYPE decimal;
