-- =============================================================================
-- Migration: 003_create_chat_mutes.sql
-- Database: arena_chat_db
-- Storage Engine: InnoDB | Charset: utf8mb4 | Collation: utf8mb4_unicode_ci
-- =============================================================================

CREATE TABLE IF NOT EXISTS chat_mutes (
    mute_id      BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id      INT          NOT NULL,
    muted_by     INT          NOT NULL,
    reason       VARCHAR(255) DEFAULT NULL,
    muted_at     TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    expires_at   TIMESTAMP    NOT NULL,

    -- Composite index for fast "is this user currently muted?" lookups
    INDEX idx_mute_user_expiry (user_id, expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
