--liquibase formatted sql

-- changeset Aldis:37
-- comment: Move invoice lines from invoices.items_data (JSON text) to invoice_lines, in their order: item id, quantity and comments. items_data that isn't a JSON array moves no lines; changeset 38 stops before dropping it. A line whose item no longer exists fails on fk_invoice_lines_item_id and nothing is moved.
INSERT INTO invoice_lines (invoice_id, line_no, item_id, quantity, comments)
SELECT i.id, l.ord, (l.value->>'Id')::uuid, (l.value->>'Quantity')::numeric, l.value->>'Comments'
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END)
    WITH ORDINALITY AS l(value, ord);

-- rollback DELETE FROM invoice_lines;
