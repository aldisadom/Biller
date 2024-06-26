--liquibase formatted sql

-- changeset Aldis:11
-- comment: Create initial sellers
INSERT INTO sellers
	(id, user_id, company_name, street, city,state, email, phone)
	VALUES 
    ('c85f22c7-1323-423a-a402-9b711a44c119','2271c7b3-69ed-4e0c-a820-ecafbd57ac80','Dragons Delight','Main road 5','Winterfell','The North','dragon_delight@winter.com','+123450679'),
    ('b258f0f9-0b37-40bf-9e7c-276de6a77a0f','446e9d66-066b-4400-bd79-025fecbb5296','Stark Supply Co.','Northern Road','Winterfell','The North','stark_supply_co@winterfell.com','+123456789'),
    ('f144040e-f926-46af-8704-a5c44cc26803','45377204-b457-4ed8-b6ff-1feb88d0db08','Golden Hand Enterprises','Lannister Lane','Casterly Rock','The Westerlands','golden_hand_enterprises@lannister.com','+123456789'),
    ('d74de3aa-7cdc-4c31-afb9-e3c49b25ba97','7c6cdefd-b3cb-4db8-afe9-3d079ecbcc20','The Nights Watch Supplies','Castle Black Road','The Wall','Beyond the Wall','nights_watch_supplies@gmail.com','+123456789'),
    ('4907d371-cf06-4f96-943c-2e35df36a701','93abc858-64b0-4c7a-948f-d7d404fb574e','Stark Style','Winterfell Avenue','Winterfell','The North','stark_style@winterfell.com','+123456789'),
    ('4c2c9096-bbc1-45e9-9155-39898ee2fb6d','9405662a-c465-4a21-b1f2-2a90fb9a84b3','Lannister Libations','Lannister Lane','Casterly Rock','The Westerlands','lannister_libations@lannister.com','+123456789'),
    ('a59deb55-5799-4394-9cad-66f6d04e4169','e5c23765-e3a9-4291-9219-b809af8a89e6','Faceless Freight','No Ones Road','Braavos','The Free Cities','faceless_freight@gmail.com','+123456789'),
    ('9f8788f2-4d08-4812-9953-1ebb02537dbc','f34ab121-82b5-4e06-a92e-8757a3b72f87','Red Priestess Imports','Asshai Alley','Asshai','Asshai','red_priestess_imports@lordoflight.com','+123456789'),
    ('09a7ba14-ffd3-4ba2-b879-6604e30f135b','42cbf0b0-e3e1-48d4-aa65-72ab2e8b4f3f','Whispering Associates','Spiders Street','Kings Landing','The Crownlands','whispering_associates@gmail.com','+123456789'),
    ('938d31b6-a403-4866-b5f5-a63a9e329146','fe09200f-2ecb-488b-9390-dc562dec59cb','Greenseer Grocers','Old Gods Avenue','Winterfell','The North','greenseer_grocers@winterfell.com','+123456789')

-- rollback DELETE FROM customers;
