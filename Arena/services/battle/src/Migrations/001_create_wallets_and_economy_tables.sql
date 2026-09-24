-- =============================================================================
-- Migration: 001_create_wallets_and_economy_tables.sql
-- Database: arena_battle_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Core schema definition for Battle/Economy Service (SCRUM-114).
-- =============================================================================

CREATE DATABASE IF NOT EXISTS arena_battle_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE arena_battle_db;

-- -----------------------------------------------------------------------------
-- 1. Table: wallets
-- Stores viewer coin balances and timestamp of the latest successful coin award.
-- INVARIANT: Initial coin balance for any newly created wallet is strictly 0.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS wallets (
    wallet_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL UNIQUE,                          -- Synthetic FK to UserService.users(user_id)
    coins INT NOT NULL DEFAULT 0,                        -- Default balance is strictly 0
    last_tick_at DATETIME(3) NULL,                       -- Timestamp of last successful watch award
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_wallets_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 2. Table: coin_transactions
-- Audit log and ledger for coin awards and purchases.
-- Supports future SCRUM-115 (time-window coin cap calculations per user & stream).
-- Supports future SCRUM-117 (event emission tracing).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS coin_transactions (
    transaction_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,                                -- Synthetic FK to UserService.users(user_id)
    wallet_id INT NOT NULL,
    amount INT NOT NULL,
    transaction_type VARCHAR(32) NOT NULL DEFAULT 'WATCH_TICK',
    stream_id INT NULL,                                  -- Synthetic FK to StreamService.streams(stream_id)
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_coin_transactions_wallet FOREIGN KEY (wallet_id) REFERENCES wallets(wallet_id) ON DELETE CASCADE,
    INDEX idx_transactions_user (user_id),
    INDEX idx_transactions_user_stream_created (user_id, stream_id, created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
