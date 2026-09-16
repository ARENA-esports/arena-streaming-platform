-- =============================================================================
-- Migration: 02_increase_team_logo_url_length_and_clear_placeholders.sql
-- Service: StreamService
-- Description: Expand logo_url column from VARCHAR(255) to MEDIUMTEXT for base64 images
--              and clear placeholder assets.arena.gg URLs that cause SSL errors.
-- =============================================================================

ALTER TABLE teams 
MODIFY COLUMN logo_url MEDIUMTEXT NULL;

UPDATE teams 
SET logo_url = NULL 
WHERE logo_url LIKE '%assets.arena.gg%';
