--liquibase formatted sql

-- changeset Aldis:37
-- preconditions onFail:HALT onError:HALT
-- precondition-sql-check expectedResult:0 SELECT COUNT(*) FROM invoices WHERE NOT (items_data IS JSON ARRAY)
-- comment: Move invoice items from invoices.items_data (JSON text) to invoice_items, in their order: item id, quantity and comments. Stops before moving anything while an invoice's items_data isn't a JSON array: fix the row and update again. An invoice item whose item no longer exists fails on fk_invoice_items_item_id and nothing is moved.
INSERT INTO invoice_items (invoice_id, line_no, item_id, quantity, comments)
SELECT i.id, ii.ord, (ii.value->>'Id')::uuid, (ii.value->>'Quantity')::numeric, ii.value->>'Comments'
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END)
    WITH ORDINALITY AS ii(value, ord);

-- rollback DELETE FROM invoice_items;
