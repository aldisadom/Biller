--liquibase formatted sql

-- changeset Aldis:5
-- comment: Create users_permissions table
CREATE TABLE users_permissions (
    user_id uuid NOT NULL,
    permission_id uuid NOT NULL,

    CONSTRAINT fk_user_id
      FOREIGN KEY(user_id) 
        REFERENCES users(id),

    CONSTRAINT fk_permission_id
      FOREIGN KEY(permission_id) 
        REFERENCES permissions(id)
);

-- rollback DROP TABLE users_permissions;
