-- =============================================================================
-- Migration: 003_create_battle_bars.sql
-- Database: arena_battle_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Battle bar damage aggregation table (SCRUM-120).
--              Supports atomic UPDATE for concurrent attack submissions.
-- =============================================================================

USE arena_battle_db;

-- -----------------------------------------------------------------------------
-- 1. Table: battle_bars
-- Aggregated damage per team per match. Updated atomically via
-- INSERT ... ON DUPLICATE KEY UPDATE total_damage = total_damage + @Damage
-- to prevent lost updates under concurrent load.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS battle_bars (
    bar_id        INT AUTO_INCREMENT PRIMARY KEY,
    match_id      INT          NOT NULL,
    team_id       INT          NOT NULL,
    total_damage  BIGINT       NOT NULL DEFAULT 0,
    created_at    TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    updated_at    TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY uq_match_team (match_id, team_id),
    INDEX idx_battle_bars_match (match_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
