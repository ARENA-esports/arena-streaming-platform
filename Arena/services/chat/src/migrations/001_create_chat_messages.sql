-- =============================================================================
-- Migration: 001_create_chat_messages.sql
-- Database: arena_chat_db
-- Storage Engine: InnoDB | Charset: utf8mb4 | Collation: utf8mb4_unicode_ci
-- =============================================================================

CREATE DATABASE IF NOT EXISTS arena_chat_db;
USE arena_chat_db;

-- 1. Core Chat Messages Table
CREATE TABLE IF NOT EXISTS chat_messages (
    message_id    BIGINT AUTO_INCREMENT PRIMARY KEY,
    team_id       INT          NOT NULL,
    user_id       INT          NOT NULL,
    username      VARCHAR(50)  NOT NULL,
    content       VARCHAR(500) NOT NULL,
    created_at    TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,

    -- Composite index for efficient history hydration: SELECT ... WHERE team_id = ? ORDER BY created_at DESC LIMIT 50
    INDEX idx_chat_team_time (team_id, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
