--liquibase formatted sql

-- changeset Aldis:7
-- comment: Create invoices table
CREATE TABLE invoices (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    customers_id uuid NOT NULL,
    file_path character varying(255) NOT NULL,

    CONSTRAINT fk_invoices_customers_id
      FOREIGN KEY(customers_id) 
        REFERENCES customers(id)
);

CREATE INDEX idx_invoices_customer_id ON invoices (customers_id);
-- rollback DROP TABLE users_permissions;
