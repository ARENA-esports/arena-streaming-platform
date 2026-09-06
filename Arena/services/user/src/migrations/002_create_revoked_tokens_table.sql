-- =============================================================================
-- Migration: 002_create_revoked_tokens_table.sql
-- Database: arena_user_db
-- Storage Engine: InnoDB | Charset: utf8mb4 | Collation: utf8mb4_unicode_ci
-- =============================================================================

USE arena_user_db;

-- Revoked Tokens Table (JWT Blacklist Store)
CREATE TABLE IF NOT EXISTS revoked_tokens (
    jti VARCHAR(64) NOT NULL PRIMARY KEY,
    user_id INT NULL,
    revoked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at DATETIME NOT NULL,
    INDEX idx_revoked_tokens_expires (expires_at),
    INDEX idx_revoked_tokens_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
