-- =============================================================================
-- Migration: 002_add_tournament_query_indexes
-- Service: TournamentService
-- Target Database: arena_tournament_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Adds indexes on start_date and composite (status, start_date)
--              to optimize tournament listing and filtering queries without filesort.
-- =============================================================================

SET @exist := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'tournaments' AND index_name = 'idx_tournaments_start_date');
SET @sqlstmt := IF(@exist = 0, 'ALTER TABLE tournaments ADD INDEX idx_tournaments_start_date (start_date ASC)', 'SELECT 1');
PREPARE stmt FROM @sqlstmt;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @exist2 := (SELECT COUNT(*) FROM information_schema.statistics WHERE table_schema = DATABASE() AND table_name = 'tournaments' AND index_name = 'idx_tournaments_status_start_date');
SET @sqlstmt2 := IF(@exist2 = 0, 'ALTER TABLE tournaments ADD INDEX idx_tournaments_status_start_date (status, start_date ASC)', 'SELECT 1');
PREPARE stmt2 FROM @sqlstmt2;
EXECUTE stmt2;
DEALLOCATE PREPARE stmt2;

