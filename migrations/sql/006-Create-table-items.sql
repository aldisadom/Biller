--liquibase formatted sql

-- changeset Aldis:6
-- comment: Create items table
CREATE TABLE items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    customers_id uuid NOT NULL,
    name character varying(255)  NOT NULL,
    price numeric NOT NULL,
    quantity integer NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_invoice_customers_id
      FOREIGN KEY(customers_id) 
        REFERENCES customers(id)
);

CREATE INDEX idx_items_user_id ON items (customers_id)

-- rollback DROP TABLE items;
