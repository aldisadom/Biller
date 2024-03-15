--liquibase formatted sql

-- changeset Aldis:3
-- comment: Create invoice_items table
CREATE TABLE invoice_items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    address_id uuid NOT NULL,
    name character varying(255)  NOT NULL,
    price numeric NOT NULL,
    quantity integer NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_invoice_address_id
      FOREIGN KEY(address_id) 
        REFERENCES invoice_addresses(id)
);

CREATE INDEX idx_invoice_items_user_id ON invoice_items (address_id)

-- rollback DROP TABLE invoice_items;
