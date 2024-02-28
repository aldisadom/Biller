--liquibase formatted sql

-- changeset Aldis:4
-- comment: Create permissions table
CREATE TABLE permissions (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    name character varying(255)  NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT permissions_name UNIQUE (name)
);

-- rollback DROP TABLE permissions;
