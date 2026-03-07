-- Add a random user to the users table
-- PostgreSQL script for TinyNote database

INSERT INTO users ("Id", "UserName", "Email", "PasswordHash", "CreatedAt")
VALUES (
    'd44dc55f-e08c-4db2-a918-3093f1e11848',
    'user_' || substr(md5(random()::text), 1, 8),
    'user_' || substr(md5(random()::text), 1, 8) || '@example.com',
    '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',  -- BCrypt hash of "password"
    NOW()
);
