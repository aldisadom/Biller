--liquibase formatted sql

-- changeset Aldis:1  
-- comment: Create users table
CREATE TABLE users (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    email character varying(255) NOT NULL,
    password character varying  NOT NULL,
    user_type character varying(50) NOT NULL,
    name character varying(50) NOT NULL,
    last_name character varying(50) NOT NULL,

    PRIMARY KEY(id)
);

-- rollback DROP TABLE users;
