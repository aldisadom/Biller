--liquibase formatted sql

-- changeset Aldis:9
-- comment: Create invoices table
CREATE TABLE invoices (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    address_id uuid NOT NULL,
    file_path character varying(255) NOT NULL,

    CONSTRAINT fk_address_id_id
      FOREIGN KEY(address_id) 
        REFERENCES invoice_addresses(id),
);

-- rollback DROP TABLE users_permissions;
