--liquibase formatted sql

-- changeset Aldis:40
-- comment: Version 1 of every item from its current name and price, and version 1 for every invoice item, in one statement (BIL-85). There is no older history: BIL-113 dropped the invoices' copies of names and prices. changed_date is this deploy, not when the item was last edited. No invoice, total or PDF changes.
WITH added AS (
    INSERT INTO item_versions (item_id, version, name, price)
    SELECT id, 1, name, price FROM items
    RETURNING item_id
)
UPDATE invoice_items ii SET version = 1
FROM added a
WHERE a.item_id = ii.item_id;

-- rollback WITH cleared AS (UPDATE invoice_items SET version = NULL) DELETE FROM item_versions;
