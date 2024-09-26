--liquibase formatted sql

-- changeset Aldis:12
-- comment: Create initial customers
INSERT INTO customers
	(id, seller_id, invoice_name, company_name, street, city,state, email, phone)
	VALUES 
    ('c85f22c7-1323-423a-a402-9b711a44c119','f144040e-f926-46af-8704-a5c44cc26803','DragDel','Dragons Delight','Main road 5','Winterfell','The North','dragon_delight@winter.com','+123450679'),
    ('b258f0f9-0b37-40bf-9e7c-276de6a77a0f','d74de3aa-7cdc-4c31-afb9-e3c49b25ba97','SSC','Stark Supply Co.','Northern Road','Winterfell','The North','stark_supply_co@winterfell.com','+123456789'),
    ('f144040e-f926-46af-8704-a5c44cc26803','4907d371-cf06-4f96-943c-2e35df36a701','GHE','Golden Hand Enterprises','Lannister Lane','Casterly Rock','The Westerlands','golden_hand_enterprises@lannister.com','+123456789'),
    ('d74de3aa-7cdc-4c31-afb9-e3c49b25ba97','4c2c9096-bbc1-45e9-9155-39898ee2fb6d','NWS','The Nights Watch Supplies','Castle Black Road','The Wall','Beyond the Wall','nights_watch_supplies@gmail.com','+123456789'),
    ('4907d371-cf06-4f96-943c-2e35df36a701','a59deb55-5799-4394-9cad-66f6d04e4169','SStyle','Stark Style','Winterfell Avenue','Winterfell','The North','stark_style@winterfell.com','+123456789'),
    ('4c2c9096-bbc1-45e9-9155-39898ee2fb6d','9f8788f2-4d08-4812-9953-1ebb02537dbc','LanLib','Lannister Libations','Lannister Lane','Casterly Rock','The Westerlands','lannister_libations@lannister.com','+123456789'),
    ('a59deb55-5799-4394-9cad-66f6d04e4169','09a7ba14-ffd3-4ba2-b879-6604e30f135b','FF','Faceless Freight','No Ones Road','Braavos','The Free Cities','faceless_freight@gmail.com','+123456789'),
    ('9f8788f2-4d08-4812-9953-1ebb02537dbc','938d31b6-a403-4866-b5f5-a63a9e329146','RPI','Red Priestess Imports','Asshai Alley','Asshai','Asshai','red_priestess_imports@lordoflight.com','+123456789'),
    ('09a7ba14-ffd3-4ba2-b879-6604e30f135b','c85f22c7-1323-423a-a402-9b711a44c119','Whisp','Whispering Associates','Spiders Street','Kings Landing','The Crownlands','whispering_associates@gmail.com','+123456789'),
    ('938d31b6-a403-4866-b5f5-a63a9e329146','b258f0f9-0b37-40bf-9e7c-276de6a77a0f','GreenG','Greenseer Grocers','Old Gods Avenue','Winterfell','The North','greenseer_grocers@winterfell.com','+123456789'),
    ('903f9cb1-582f-4ba9-be2d-2a80ea4264d2','b258f0f9-0b37-40bf-9e7c-276de6a77a0f','DragDel','Dragons Delight','Main road 5','Winterfell','The North','dragon_delight@winter.com','+123450679'),
    ('b1f796e8-d23d-4b8b-b464-5e121e4385c7','f144040e-f926-46af-8704-a5c44cc26803','SSC','Stark Supply Co.','Northern Road','Winterfell','The North','stark_supply_co@winterfell.com','+123456789'),
    ('a489f2b1-83a3-4ac1-a367-167fe28c2801','d74de3aa-7cdc-4c31-afb9-e3c49b25ba97','GHE','Golden Hand Enterprises','Lannister Lane','Casterly Rock','The Westerlands','golden_hand_enterprises@lannister.com','+123456789'),
    ('60cfd2f4-b938-47fa-94d3-dbb2ac60135a','4907d371-cf06-4f96-943c-2e35df36a701','NWS','The Nights Watch Supplies','Castle Black Road','The Wall','Beyond the Wall','nights_watch_supplies@gmail.com','+123456789'),
    ('1532b520-f6e9-499c-a807-7b5922c889d6','4c2c9096-bbc1-45e9-9155-39898ee2fb6d','SStyle','Stark Style','Winterfell Avenue','Winterfell','The North','stark_style@winterfell.com','+123456789'),
    ('d88565d4-f032-4ee3-85c2-97cfd4c82f5c','a59deb55-5799-4394-9cad-66f6d04e4169','LanLib','Lannister Libations','Lannister Lane','Casterly Rock','The Westerlands','lannister_libations@lannister.com','+123456789'),
    ('2bd6654e-e0b6-479c-81a0-d1a4eae4e5cb','9f8788f2-4d08-4812-9953-1ebb02537dbc','FF','Faceless Freight','No Ones Road','Braavos','The Free Cities','faceless_freight@gmail.com','+123456789'),
    ('7bd72a2e-aac6-4518-b124-f96e172be855','09a7ba14-ffd3-4ba2-b879-6604e30f135b','RPI','Red Priestess Imports','Asshai Alley','Asshai','Asshai','red_priestess_imports@lordoflight.com','+123456789'),
    ('c41c2c6f-42ae-4420-bb26-304fd38b10c4','938d31b6-a403-4866-b5f5-a63a9e329146','Whisp','Whispering Associates','Spiders Street','Kings Landing','The Crownlands','whispering_associates@gmail.com','+123456789'),
    ('1542880c-b882-4dcf-943b-a206426c1304','c85f22c7-1323-423a-a402-9b711a44c119','GreenG','Greenseer Grocers','Old Gods Avenue','Winterfell','The North','greenseer_grocers@winterfell.com','+123456789');

-- rollback DELETE FROM customers;
