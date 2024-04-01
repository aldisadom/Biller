--liquibase formatted sql

-- changeset Aldis:10
-- comment: Create initial users
INSERT INTO users
    (id, email, password, name, last_name)
VALUES 
    ('2271c7b3-69ed-4e0c-a820-ecafbd57ac80', 'MotherOfDragons@hotmail.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OVw.WT6BTj5dwVvuDoJ8dG5lqdTtbPR2', 'Daenerys', 'Targaryen'), --FireAndBlood
    ('446e9d66-066b-4400-bd79-025fecbb5296', 'LadyCatelyn@winterfell.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OX3NkGElKHfebHXS.swYEAnaMt2qS5wC', 'Catelyn', 'Stark'), --MotherOfWolves
    ('45377204-b457-4ed8-b6ff-1feb88d0db08', 'Kingslayer@lannister.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OPLb9TtOO32AjY8PV.Ll3vsKIYDTs6wC', 'Jaime', 'Lannister'), -- GoldenHand
    ('7c6cdefd-b3cb-4db8-afe9-3d079ecbcc20', 'WannabeStark@gmail.com', '$2a$13$.nWShw.mR6cpwUxp16iE.O4KoWNEqH3t1BjFus1nCc4ar7woP7j5a', 'Jon', 'Snow'), -- WinterIsComing
    ('93abc858-64b0-4c7a-948f-d7d404fb574e', 'LadySansa@winterfell.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OJZbuG9DMtT8aF89ulmjamXsJNeESnG2', 'Sansa', 'Stark'), -- LadyOfWinterfell
    ('9405662a-c465-4a21-b1f2-2a90fb9a84b3', 'TheImp@lannister.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OcU81A0HN1p1B3oi6lO7IXg2T/Rzqe0G', 'Tyrion', 'Lannister'), -- ImpInTrouble
    ('e5c23765-e3a9-4291-9219-b809af8a89e6', 'NoOne@gmail.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OfsFuOeKJ7aSTLJ0GV/9ZqWDp2kevzMu', 'Arya', 'Stark'), -- ValarMorghulis
    ('f34ab121-82b5-4e06-a92e-8757a3b72f87', 'RedPriestess@lordoflight.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OYKQCwwU3aJYO/JJuEsZnDJ6CDW.jrIS', 'Melisandre', 'of Asshai'), -- FieryRedLady
    ('42cbf0b0-e3e1-48d4-aa65-72ab2e8b4f3f', 'TheSpider@spymaster.com', '$2a$13$.nWShw.mR6cpwUxp16iE.OQzNA36iWpivS23EAE4fG8urVochGLBW', 'Varys', '.'), -- SecretWhisperer
    ('fe09200f-2ecb-488b-9390-dc562dec59cb', 'ThreeEyedRaven@winterfell.com', '$2a$13$.nWShw.mR6cpwUxp16iE.O8Z7xYLG1/CodqqDeU.mMtkg6DdDbRuW', 'Bran', 'Stark'); --Greenseer

-- rollback DELETE FROM users;
