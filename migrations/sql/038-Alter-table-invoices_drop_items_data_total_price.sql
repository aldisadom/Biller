--liquibase formatted sql

-- changeset Aldis:38
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT COUNT(*) FROM invoices i WHERE CASE WHEN i.items_data IS JSON ARRAY THEN jsonb_array_length(i.items_data::jsonb) <> (SELECT COUNT(*) FROM invoice_items ii WHERE ii.invoice_id = i.id) ELSE true END
-- comment: Drop invoices.items_data and invoices.total_price: the invoice items are in invoice_items, and the total is computed from them and the items' current prices (BIL-113). Stops when an invoice's JSON items weren't all moved. The rollback rebuilds both from invoice_items with the items' current name and price.
ALTER TABLE invoices DROP COLUMN items_data, DROP COLUMN total_price;

-- rollback ALTER TABLE invoices ADD COLUMN items_data TEXT, ADD COLUMN total_price decimal;
-- rollback UPDATE invoices i SET items_data = COALESCE((SELECT jsonb_agg(jsonb_build_object('Id', ii.item_id, 'Name', it.name, 'Price', it.price, 'Quantity', ii.quantity, 'Comments', ii.comments) ORDER BY ii.line_no) FROM invoice_items ii JOIN items it ON it.id = ii.item_id WHERE ii.invoice_id = i.id), '[]'::jsonb)::text, total_price = COALESCE((SELECT SUM(ii.quantity * it.price) FROM invoice_items ii JOIN items it ON it.id = ii.item_id WHERE ii.invoice_id = i.id), 0);
-- rollback ALTER TABLE invoices ALTER COLUMN items_data SET NOT NULL, ALTER COLUMN total_price SET NOT NULL;
