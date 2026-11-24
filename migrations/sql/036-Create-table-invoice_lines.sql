--liquibase formatted sql

-- changeset Aldis:36
-- comment: Invoice lines move from invoices.items_data (JSON text) to invoice_lines. A line names its item and keeps quantity and comments; name and price are read from the item. items_data that isn't a JSON array moves no lines; changeset 37 stops before dropping it.
CREATE TABLE invoice_lines (
    invoice_id uuid NOT NULL,
    line_no integer NOT NULL,
    item_id uuid NOT NULL,
    quantity numeric NOT NULL,
    comments text,

    PRIMARY KEY (invoice_id, line_no),
    CONSTRAINT fk_invoice_lines_invoice_id
      FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    CONSTRAINT fk_invoice_lines_item_id
      FOREIGN KEY (item_id) REFERENCES items(id),
    CONSTRAINT ck_invoice_lines_line_no CHECK (line_no >= 1),
    CONSTRAINT ck_invoice_lines_quantity CHECK (quantity > 0)
);

CREATE INDEX idx_invoice_lines_item_id ON invoice_lines (item_id);

INSERT INTO invoice_lines (invoice_id, line_no, item_id, quantity, comments)
SELECT i.id, l.ord, (l.value->>'Id')::uuid, (l.value->>'Quantity')::numeric, l.value->>'Comments'
FROM invoices i
CROSS JOIN LATERAL jsonb_array_elements(
    CASE WHEN i.items_data IS JSON ARRAY THEN i.items_data::jsonb ELSE '[]'::jsonb END)
    WITH ORDINALITY AS l(value, ord);

-- rollback DROP TABLE invoice_lines;
