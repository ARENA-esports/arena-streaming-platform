-- =============================================================================
-- Migration: 002_add_tournament_query_indexes
-- Service: TournamentService
-- Target Database: arena_tournament_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Adds indexes on start_date and composite (status, start_date)
--              to optimize tournament listing and filtering queries without filesort.
-- =============================================================================

ALTER TABLE tournaments
    ADD INDEX idx_tournaments_start_date (start_date ASC),
    ADD INDEX idx_tournaments_status_start_date (status, start_date ASC);
