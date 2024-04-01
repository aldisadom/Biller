--liquibase formatted sql

-- changeset Aldis:2
-- comment: Create sellers table
CREATE TABLE sellers (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,
    company_name character varying(100)  NOT NULL,
    street character varying(255)  NOT NULL,
    city character varying(50)  NOT NULL,
    state character varying(50)  NOT NULL,
    email character varying(100)  NOT NULL,
    phone character varying(50)  NOT NULL,

    PRIMARY KEY(id),
    CONSTRAINT fk_user_id
      FOREIGN KEY(user_id) 
        REFERENCES users(id)
);

CREATE INDEX idx_sellers_user_id ON sellers (user_id);

-- rollback DROP TABLE sellers;
