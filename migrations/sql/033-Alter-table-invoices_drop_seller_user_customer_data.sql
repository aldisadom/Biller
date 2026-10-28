--liquibase formatted sql

-- changeset Aldis:33
-- comment: Alter invoices table, drop the user, seller and customer copies. Invoices read the current rows by id, so the copies are not kept anywhere. The rollback rebuilds them from the current rows, with an empty password.

ALTER TABLE invoices DROP COLUMN user_data;
ALTER TABLE invoices DROP COLUMN seller_data;
ALTER TABLE invoices DROP COLUMN customer_data;

-- rollback ALTER TABLE invoices ADD COLUMN user_data TEXT, ADD COLUMN seller_data TEXT, ADD COLUMN customer_data TEXT;
-- rollback UPDATE invoices i SET
-- rollback     user_data = jsonb_build_object('Id', u.id, 'Name', u.name, 'LastName', u.last_name)::text,
-- rollback     seller_data = jsonb_build_object('Id', s.id, 'UserId', s.user_id, 'CompanyName', s.company_name,
-- rollback         'CompanyNumber', s.company_number, 'Street', s.street, 'City', s.city, 'State', s.state,
-- rollback         'Email', s.email, 'Phone', s.phone, 'BankName', s.bank_name, 'BankNumber', s.bank_number)::text,
-- rollback     customer_data = jsonb_build_object('Id', c.id, 'SellerId', c.seller_id, 'InvoiceName', c.invoice_name,
-- rollback         'InvoiceNumber', i.invoice_number, 'CompanyName', c.company_name, 'CompanyNumber', c.company_number,
-- rollback         'Street', c.street, 'City', c.city, 'State', c.state, 'Email', c.email, 'Phone', c.phone)::text
-- rollback FROM users u, sellers s, customers c
-- rollback WHERE u.id = i.user_id AND s.id = i.seller_id AND c.id = i.customer_id;
-- rollback ALTER TABLE invoices ALTER COLUMN user_data SET NOT NULL, ALTER COLUMN seller_data SET NOT NULL, ALTER COLUMN customer_data SET NOT NULL;
