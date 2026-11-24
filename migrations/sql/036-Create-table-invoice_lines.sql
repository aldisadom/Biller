--liquibase formatted sql

-- changeset Aldis:36
-- comment: Create invoice_lines: a line names its item and keeps its position, quantity and comments; name and price are read from the item. Changeset 37 moves the lines out of invoices.items_data.
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

-- rollback DROP TABLE invoice_lines;
