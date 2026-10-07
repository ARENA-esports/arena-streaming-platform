-- =============================================================================
-- Migration: 004_create_battle_rounds.sql
-- Database: arena_battle_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Battle round management table with round_active flag (SCRUM-122).
--              Supports atomic check-and-flip to prevent duplicate round-ends.
-- =============================================================================

USE arena_battle_db;

-- -----------------------------------------------------------------------------
-- 1. Table: battle_rounds
-- Tracks rounds per match. Contains round_active boolean flag which is atomically
-- flipped from TRUE to FALSE when a team reaches target_damage (100% bar).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS battle_rounds (
    round_id        INT AUTO_INCREMENT PRIMARY KEY,
    match_id        INT          NOT NULL,
    round_number    INT          NOT NULL DEFAULT 1,
    target_damage   BIGINT       NOT NULL DEFAULT 100,
    winning_team_id INT          NULL,
    round_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    ended_at        TIMESTAMP    NULL,
    UNIQUE KEY uq_match_round (match_id, round_number),
    INDEX idx_match_round_active (match_id, round_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
