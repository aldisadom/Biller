--liquibase formatted sql

-- changeset Aldis:39
-- comment: Create item_versions: every name and price an item has had, numbered per item; rows are only inserted, never updated (BIL-85). Add invoice_items.version, the item version an invoice item shows; nullable until changeset 41. Changeset 40 gives every item and invoice item version 1.
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

ALTER TABLE invoice_items ADD COLUMN version integer;

-- rollback ALTER TABLE invoice_items DROP COLUMN version;
-- rollback DROP TABLE item_versions;
