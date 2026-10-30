--liquibase formatted sql

-- changeset Aldis:35
-- comment: Move invoice series from customers to series table, link customers and invoices to series

-- 1. One series per seller and trimmed customer name. Invoices no longer keep the name they were issued
--    with (changeset 33), so every old invoice goes into its customer's current series.
--    next_number = highest "next number" of the customers with that name.
INSERT INTO series (seller_id, name, next_number)
SELECT seller_id, TRIM(invoice_name), MAX(GREATEST(COALESCE(invoice_number, 1), 1))
FROM customers
GROUP BY seller_id, TRIM(invoice_name);

-- 2. Link customers to the series with their current name.
ALTER TABLE customers ADD COLUMN series_id uuid;

UPDATE customers c
SET series_id = s.id
FROM series s
WHERE s.seller_id = c.seller_id AND s.name = TRIM(c.invoice_name);

ALTER TABLE customers ALTER COLUMN series_id SET NOT NULL;
ALTER TABLE customers ADD CONSTRAINT fk_customers_series_id
    FOREIGN KEY (series_id) REFERENCES series(id);
CREATE INDEX idx_customers_series_id ON customers (series_id);

-- 3. Link invoices to their customer's series.
ALTER TABLE invoices ADD COLUMN series_id uuid;

UPDATE invoices i
SET series_id = c.series_id
FROM customers c
WHERE c.id = i.customer_id;

ALTER TABLE invoices ALTER COLUMN series_id SET NOT NULL;
ALTER TABLE invoices ADD CONSTRAINT fk_invoices_series_id
    FOREIGN KEY (series_id) REFERENCES series(id);
CREATE UNIQUE INDEX uq_invoices_series_id_invoice_number ON invoices (series_id, invoice_number);

-- 4. next_number must be above every existing invoice number in the series.
UPDATE series s
SET next_number = GREATEST(s.next_number, m.max_number + 1)
FROM (SELECT series_id, MAX(invoice_number) AS max_number FROM invoices GROUP BY series_id) m
WHERE m.series_id = s.id;

-- 5. Remove the old columns.
ALTER TABLE customers DROP COLUMN invoice_name;
ALTER TABLE customers DROP COLUMN invoice_number;
ALTER TABLE invoices DROP COLUMN file_path;

-- rollback ALTER TABLE invoices ADD COLUMN file_path character varying(255) NOT NULL DEFAULT '';
-- rollback ALTER TABLE customers ADD COLUMN invoice_name character varying(10);
-- rollback ALTER TABLE customers ADD COLUMN invoice_number INT DEFAULT 0;
-- rollback UPDATE customers c SET invoice_name = s.name, invoice_number = s.next_number FROM series s WHERE s.id = c.series_id;
-- rollback ALTER TABLE customers ALTER COLUMN invoice_name SET NOT NULL;
-- rollback DROP INDEX uq_invoices_series_id_invoice_number;
-- rollback ALTER TABLE invoices DROP COLUMN series_id;
-- rollback ALTER TABLE customers DROP COLUMN series_id;
-- rollback DELETE FROM series;
