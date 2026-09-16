-- =============================================================================
-- Migration: 004_add_team_logo_url
-- Service: TournamentService
-- Description: Add logo_url column to teams table idempotently.
-- =============================================================================

SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
      AND TABLE_NAME = 'teams' 
      AND COLUMN_NAME = 'logo_url'
);

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE teams ADD COLUMN logo_url VARCHAR(255) NULL AFTER color_hex;', 
    'SELECT 1;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
