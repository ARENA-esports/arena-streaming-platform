-- =============================================================================
-- Migration: 002_create_battle_stats_tables.sql
-- Database: arena_analytics_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Read-model schema for Battle/Attack stats and round outcomes (SCRUM-126).
-- =============================================================================

USE arena_analytics_db;

-- -----------------------------------------------------------------------------
-- 1. Table: stream_team_battle_summary
-- Aggregates attack volume, damage dealt, coins spent, and round win/loss per stream and team.
-- Queried by organizers viewing the battle/attack stats dashboard.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stream_team_battle_summary (
    stream_id INT NOT NULL,
    team_id INT NOT NULL,
    team_name VARCHAR(100) NOT NULL DEFAULT '',
    total_attacks INT NOT NULL DEFAULT 0,
    total_damage_dealt BIGINT NOT NULL DEFAULT 0,
    total_coins_spent BIGINT NOT NULL DEFAULT 0,
    rounds_won INT NOT NULL DEFAULT 0,
    rounds_lost INT NOT NULL DEFAULT 0,
    last_attack_at DATETIME(3) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (stream_id, team_id),
    INDEX idx_battle_summary_stream (stream_id),
    INDEX idx_battle_summary_team (team_id),
    INDEX idx_battle_summary_attacks (total_attacks),
    INDEX idx_battle_summary_damage (total_damage_dealt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 2. Table: stream_round_outcomes
-- Stores round outcome history per stream and participating teams.
-- Queried by organizers to inspect round progression and victor distributions.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stream_round_outcomes (
    stream_id INT NOT NULL,
    round_number INT NOT NULL,
    winning_team_id INT NOT NULL,
    winning_team_name VARCHAR(100) NOT NULL DEFAULT '',
    team_a_id INT NOT NULL,
    team_b_id INT NOT NULL,
    team_a_attacks INT NOT NULL DEFAULT 0,
    team_b_attacks INT NOT NULL DEFAULT 0,
    team_a_damage INT NOT NULL DEFAULT 0,
    team_b_damage INT NOT NULL DEFAULT 0,
    completed_at DATETIME(3) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (stream_id, round_number),
    INDEX idx_round_outcomes_stream (stream_id),
    INDEX idx_round_outcomes_winner (winning_team_id),
    INDEX idx_round_outcomes_completed (completed_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
