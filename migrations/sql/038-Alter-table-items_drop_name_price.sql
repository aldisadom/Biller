--liquibase formatted sql

-- changeset Aldis:38
-- comment: Alter items table, drop name and price: they live in item_versions now (the highest version is the current one)
ALTER TABLE items DROP COLUMN name, DROP COLUMN price;

-- rollback ALTER TABLE items ADD COLUMN name character varying(255), ADD COLUMN price numeric;
-- rollback UPDATE items i SET name = v.name, price = v.price FROM item_versions v WHERE v.item_id = i.id AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id);
-- rollback ALTER TABLE items ALTER COLUMN name SET NOT NULL, ALTER COLUMN price SET NOT NULL;
