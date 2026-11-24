--liquibase formatted sql

-- changeset Aldis:37
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT COUNT(*) FROM invoices i WHERE CASE WHEN i.items_data IS JSON ARRAY THEN jsonb_array_length(i.items_data::jsonb) <> (SELECT COUNT(*) FROM invoice_lines l WHERE l.invoice_id = i.id) ELSE true END
-- comment: Drop invoices.items_data: the lines are in invoice_lines. Stops when an invoice's JSON lines weren't all moved. The rollback rebuilds the JSON from invoice_lines with the items' current name and price.
ALTER TABLE invoices DROP COLUMN items_data;

-- rollback ALTER TABLE invoices ADD COLUMN items_data TEXT;
-- rollback UPDATE invoices i SET items_data = COALESCE((SELECT jsonb_agg(jsonb_build_object('Id', l.item_id, 'Name', it.name, 'Price', it.price, 'Quantity', l.quantity, 'Comments', l.comments) ORDER BY l.line_no) FROM invoice_lines l JOIN items it ON it.id = l.item_id WHERE l.invoice_id = i.id), '[]'::jsonb)::text;
-- rollback ALTER TABLE invoices ALTER COLUMN items_data SET NOT NULL;
