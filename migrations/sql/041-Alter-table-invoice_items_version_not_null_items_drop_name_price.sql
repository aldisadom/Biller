--liquibase formatted sql

-- changeset Aldis:41
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT (SELECT COUNT(*) FROM items i WHERE NOT EXISTS (SELECT 1 FROM item_versions v WHERE v.item_id = i.id)) + (SELECT COUNT(*) FROM invoice_items WHERE version IS NULL)
-- comment: invoice_items.version becomes required, and must be a version of the invoice item's item. Drop items.name and items.price: they live in item_versions, whose highest version is the current one (BIL-85). Stops before the drop while an item or an invoice item has no version. The rollback puts name and price back from each item's latest version only; older versions are lost when changeset 40 is rolled back.
ALTER TABLE invoice_items
    ALTER COLUMN version SET NOT NULL,
    ADD CONSTRAINT fk_invoice_items_item_id_version
      FOREIGN KEY (item_id, version) REFERENCES item_versions (item_id, version);

ALTER TABLE items DROP COLUMN name, DROP COLUMN price;

-- rollback ALTER TABLE items ADD COLUMN name character varying(255), ADD COLUMN price numeric;
-- rollback UPDATE items i SET name = v.name, price = v.price FROM item_versions v WHERE v.item_id = i.id AND v.version = (SELECT MAX(version) FROM item_versions WHERE item_id = i.id);
-- rollback ALTER TABLE items ALTER COLUMN name SET NOT NULL, ALTER COLUMN price SET NOT NULL;
-- rollback ALTER TABLE invoice_items DROP CONSTRAINT fk_invoice_items_item_id_version, ALTER COLUMN version DROP NOT NULL;
