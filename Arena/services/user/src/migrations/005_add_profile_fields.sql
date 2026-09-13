-- Up
ALTER TABLE users 
ADD COLUMN display_name VARCHAR(50) NULL AFTER username,
ADD COLUMN bio VARCHAR(300) NULL AFTER display_name,
ADD COLUMN banner_url VARCHAR(255) NULL AFTER avatar_url;

-- Down
-- ALTER TABLE users DROP COLUMN display_name, DROP COLUMN bio, DROP COLUMN banner_url;
