--liquibase formatted sql

-- changeset Aldis:38
-- comment: Drop invoices.total_price: the total is computed from the lines and the items' current prices (BIL-113). The rollback rebuilds it the same way.
ALTER TABLE invoices DROP COLUMN total_price;

-- rollback ALTER TABLE invoices ADD COLUMN total_price decimal;
-- rollback UPDATE invoices i SET total_price = COALESCE((SELECT SUM(l.quantity * it.price) FROM invoice_lines l JOIN items it ON it.id = l.item_id WHERE l.invoice_id = i.id), 0);
-- rollback ALTER TABLE invoices ALTER COLUMN total_price SET NOT NULL;
