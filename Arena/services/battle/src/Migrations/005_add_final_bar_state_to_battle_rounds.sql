-- =============================================================================
-- Migration: 005_add_final_bar_state_to_battle_rounds.sql
-- Database: arena_battle_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Adds final_bar_state JSON column to battle_rounds table (SCRUM-123).
--              Preserves snapshot of team battle bars when a round concludes.
-- =============================================================================

USE arena_battle_db;

-- -----------------------------------------------------------------------------
-- Add final_bar_state column to store JSON array of { teamId, totalDamage }
-- snapshots captured when the round is won and before bars are reset to zero.
-- -----------------------------------------------------------------------------
ALTER TABLE battle_rounds
    ADD COLUMN final_bar_state JSON NULL AFTER winning_team_id;
