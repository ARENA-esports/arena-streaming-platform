-- =============================================================================
-- Migration: 002_create_chat_team_cache.sql
-- Database: arena_chat_db
-- =============================================================================

CREATE TABLE IF NOT EXISTS chat_team_cache (
    team_id    INT PRIMARY KEY,
    team_name  VARCHAR(100) NOT NULL,
    team_color VARCHAR(7)   NOT NULL,
    updated_at TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
