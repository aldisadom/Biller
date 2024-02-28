--liquibase formatted sql

-- changeset Aldis:3
-- comment: Create invoice_items table
CREATE TABLE invoice_items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    client_id uuid NOT NULL,
    name character varying(255)  NOT NULL,
    price numeric NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_invoice_client_id
      FOREIGN KEY(client_id) 
        REFERENCES invoice_clients(id)
);

CREATE INDEX idx_invoice_items_user_id ON invoice_items (client_id)
-- rollback DROP TABLE invoice_items;
