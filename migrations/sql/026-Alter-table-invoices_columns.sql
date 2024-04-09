--liquibase formatted sql

-- changeset Aldis:26
-- comment: Alter invoices table, add all needed columns
-- comment: Create invoices table

DROP TABLE invoices;

CREATE TABLE invoices (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    customer_id uuid NOT NULL,
    seller_id uuid NOT NULL,
    user_id uuid NOT NULL,
    file_path character varying(255) NOT NULL,
    number character varying(10) NOT NULL,
    user_data TEXT NOT NULL,
    created_date DATE NOT NULL,
    due_date DATE NOT NULL,
    seller_data TEXT NOT NULL,
    customer_data TEXT NOT NULL,
    items_data TEXT NOT NULL,
    comments TEXT,
    total_price decimal NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_invoices_customer_id
      FOREIGN KEY(customer_id) 
        REFERENCES customers(id),

    CONSTRAINT fk_invoices_seller_id
      FOREIGN KEY(seller_id) 
        REFERENCES sellers(id),

    CONSTRAINT fk_invoices_user_id
      FOREIGN KEY(user_id) 
        REFERENCES users(id)
);
