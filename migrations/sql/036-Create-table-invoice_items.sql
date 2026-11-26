--liquibase formatted sql

-- changeset Aldis:36
-- comment: Create invoice_items: an invoice item names its item and keeps its line number, quantity and comments; name and price are read from the item. Changeset 37 moves the invoice items out of invoices.items_data.
CREATE TABLE invoice_items (
    invoice_id uuid NOT NULL,
    line_no integer NOT NULL,
    item_id uuid NOT NULL,
    quantity numeric NOT NULL,
    comments text,

    PRIMARY KEY (invoice_id, line_no),
    CONSTRAINT fk_invoice_items_invoice_id
      FOREIGN KEY (invoice_id) REFERENCES invoices(id) ON DELETE CASCADE,
    CONSTRAINT fk_invoice_items_item_id
      FOREIGN KEY (item_id) REFERENCES items(id),
    CONSTRAINT ck_invoice_items_line_no CHECK (line_no >= 1),
    CONSTRAINT ck_invoice_items_quantity CHECK (quantity > 0)
);

CREATE INDEX idx_invoice_items_item_id ON invoice_items (item_id);

-- rollback DROP TABLE invoice_items;
