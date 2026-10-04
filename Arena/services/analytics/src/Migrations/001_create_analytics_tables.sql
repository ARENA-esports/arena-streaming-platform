-- =============================================================================
-- Migration: 001_create_analytics_tables.sql
-- Database: arena_analytics_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Core schema definition for Analytics Service read models (SCRUM-125).
-- =============================================================================

CREATE DATABASE IF NOT EXISTS arena_analytics_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE arena_analytics_db;

-- -----------------------------------------------------------------------------
-- 1. Table: stream_engagement_summary
-- Read-model table aggregating viewer engagement per stream/match.
-- Queried by organizers viewing the engagement dashboard.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stream_engagement_summary (
    stream_id INT PRIMARY KEY,
    total_watch_seconds BIGINT NOT NULL DEFAULT 0,
    total_coins_earned BIGINT NOT NULL DEFAULT 0,
    total_watch_ticks INT NOT NULL DEFAULT 0,
    unique_viewers INT NOT NULL DEFAULT 0,
    last_event_at DATETIME(3) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_summary_coins (total_coins_earned),
    INDEX idx_summary_watch_seconds (total_watch_seconds)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 2. Table: stream_engagement_events
-- Idempotency ledger and audit log of processed watch-tick events.
-- Guarantees at-least-once Kafka deliveries are not double-counted in aggregates.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS stream_engagement_events (
    user_id INT NOT NULL,
    stream_id INT NOT NULL,
    event_timestamp DATETIME(3) NOT NULL,
    amount INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (user_id, stream_id, event_timestamp),
    INDEX idx_events_stream_user (stream_id, user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
