--liquibase formatted sql

-- changeset Aldis:1  
-- comment: Create lentele table
CREATE TABLE items (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    name VARCHAR,
    price DECIMAL,
    shop_id UUID
);


CREATE INDEX idx_lentele_date ON items (shop_id);
 
-- rollback DROP TABLE lentele;
