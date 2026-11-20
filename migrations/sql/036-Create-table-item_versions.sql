--liquibase formatted sql

-- changeset Aldis:36
-- comment: Create item_versions table: every name and price an item has had, numbered per item. Rows are only inserted, never updated
CREATE TABLE item_versions (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    item_id uuid NOT NULL,
    version int NOT NULL,
    name character varying(255) NOT NULL,
    price numeric NOT NULL,
    changed_date timestamptz NOT NULL DEFAULT now(),

    PRIMARY KEY(id),
    CONSTRAINT fk_item_versions_item_id
      FOREIGN KEY(item_id)
        REFERENCES items(id) ON DELETE CASCADE,
    CONSTRAINT uq_item_versions_item_id_version UNIQUE (item_id, version)
);

-- rollback DROP TABLE item_versions;
