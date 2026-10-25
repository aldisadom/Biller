--liquibase formatted sql

-- changeset Aldis:33
-- comment: Create series table (invoice series per seller)
CREATE TABLE series (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    seller_id uuid NOT NULL,
    name character varying(10) NOT NULL,
    next_number integer NOT NULL DEFAULT 1,

    PRIMARY KEY(id),
    CONSTRAINT fk_series_seller_id
      FOREIGN KEY(seller_id)
        REFERENCES sellers(id),
    CONSTRAINT uq_series_seller_id_name UNIQUE (seller_id, name),
    CONSTRAINT ck_series_next_number CHECK (next_number >= 1)
);

CREATE INDEX idx_series_seller_id ON series (seller_id);

-- rollback DROP TABLE series;
