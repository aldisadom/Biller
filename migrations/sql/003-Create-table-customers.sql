--liquibase formatted sql

-- changeset Aldis:3
-- comment: Create customers table
CREATE TABLE customers (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    seller_id uuid NOT NULL,
    invoice_name character varying(10)  NOT NULL,
    company_name character varying(100)  NOT NULL,
    street character varying(255)  NOT NULL,
    city character varying(50)  NOT NULL,
    state character varying(50)  NOT NULL,
    email character varying(100)  NOT NULL,
    phone character varying(50)  NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_seller_id
      FOREIGN KEY(seller_id) 
        REFERENCES sellers(id)
);

CREATE INDEX idx_customers_seller_id ON customers (seller_id);
-- rollback DROP TABLE customers;
