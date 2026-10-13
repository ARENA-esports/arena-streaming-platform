-- =============================================================================
-- Migration: 007_create_external_login_codes_table.sql
-- Database: arena_user_db
-- Description: Single-use short-lived tokens to exchange OAuth callbacks for Arena JWTs
-- =============================================================================

USE arena_user_db;

CREATE TABLE IF NOT EXISTS external_login_codes (
    code_hash VARCHAR(64) NOT NULL PRIMARY KEY,
    user_id INT NOT NULL,
    expires_at DATETIME NOT NULL,
    is_used BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_login_codes_user 
        FOREIGN KEY (user_id) REFERENCES users(user_id) 
        ON DELETE CASCADE,
    INDEX idx_login_codes_lookup (code_hash, is_used, expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;