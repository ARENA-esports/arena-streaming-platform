-- =============================================================================
-- Migration: 002_create_attack_log_and_weapons.sql
-- Database: arena_battle_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Weapon shop catalog and attack audit log (SCRUM-119).
-- =============================================================================

USE arena_battle_db;

-- -----------------------------------------------------------------------------
-- 1. Table: weapons
-- Static catalog of purchasable weapons. Rows are seeded at migration time.
-- icon_key maps to a frontend Lucide icon name for rendering.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS weapons (
    weapon_id   INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(64)  NOT NULL,
    description VARCHAR(255) NOT NULL DEFAULT '',
    cost        INT          NOT NULL,
    damage      INT          NOT NULL DEFAULT 1,
    icon_key    VARCHAR(64)  NOT NULL DEFAULT 'sword',
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMP    DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed starter weapons (costs: 10, 25, 50, 100, 200)
INSERT INTO weapons (name, description, cost, damage, icon_key) VALUES
    ('Throwing Knife', 'A quick, cheap strike.',        10,  1, 'knife'),
    ('Crossbow Bolt',  'Solid ranged damage.',          25,  3, 'crossbow'),
    ('War Hammer',     'Heavy melee blow.',             50,  6, 'hammer'),
    ('Dragon Breath',  'Devastating area attack.',     100, 12, 'dragon'),
    ('Lightning Bolt', 'Legendary electric strike.',   200, 25, 'lightning');

-- -----------------------------------------------------------------------------
-- 2. Table: attack_log
-- Audit trail of every weapon purchase / attack submitted by a viewer.
-- Supports per-match, per-team aggregation for battle outcome calculations.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS attack_log (
    attack_id      BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id        INT    NOT NULL,
    weapon_id      INT    NOT NULL,
    match_id       INT    NOT NULL,
    team_id        INT    NOT NULL,
    coins_spent    INT    NOT NULL,
    damage_dealt   INT    NOT NULL,
    created_at     TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_attack_weapon FOREIGN KEY (weapon_id) REFERENCES weapons(weapon_id),
    INDEX idx_attack_match_team (match_id, team_id),
    INDEX idx_attack_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
