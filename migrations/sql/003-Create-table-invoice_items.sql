--liquibase formatted sql

-- changeset Aldis:3
-- comment: Create invoice_items table
CREATE TABLE invoice_items (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    name character varying(255)  NOT NULL,
    price numeric NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_user_id
      FOREIGN KEY(user_id) 
        REFERENCES users(id)
);

CREATE INDEX idx_invoice_items_user_id ON invoice_items (user_id)
-- rollback DROP TABLE invoice_items;
