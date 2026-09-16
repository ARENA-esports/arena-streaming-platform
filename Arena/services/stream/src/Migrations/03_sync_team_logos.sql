-- =============================================================================
-- Migration: 03_sync_team_logos.sql
-- Service: StreamService
-- Description: Ensure teams table in arena_stream_db has MEDIUMTEXT logo_url column
-- =============================================================================

ALTER TABLE teams 
MODIFY COLUMN logo_url MEDIUMTEXT NULL;
