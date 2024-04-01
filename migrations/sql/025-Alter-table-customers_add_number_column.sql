--liquibase formatted sql

-- changeset Aldis:25
-- comment: Alter items table change customers_id to customer_id
ALTER TABLE items RENAME COLUMN customers_id to customer_id;

-- rollback ALTER TABLE items RENAME COLUMN customer_id to customers_id;