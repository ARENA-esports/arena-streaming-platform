-- =============================================================================
-- Migration: 006_increase_avatar_and_banner_url_length.sql
-- Service: UserService
-- Description: Expand avatar_url and banner_url columns from VARCHAR(255) to MEDIUMTEXT
--              to support base64 image data URLs and high-resolution profile URLs.
-- =============================================================================

ALTER TABLE users 
MODIFY COLUMN avatar_url MEDIUMTEXT NULL,
MODIFY COLUMN banner_url MEDIUMTEXT NULL;
