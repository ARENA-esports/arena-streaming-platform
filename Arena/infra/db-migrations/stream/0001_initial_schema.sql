-- =============================================================================
-- Migration: 0001_initial_schema
-- Service: StreamService
-- Target Database: arena_stream_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Initial schema definition and seed data for StreamService.
-- Note: Database creation and user/credential provisioning are handled
-- separately (infra setup), not by this migration. DbUp runs this script
-- against an already-selected arena_stream_db connection.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Table: teams (Sprint 1 Mock Store for Fixtures & Chat Factions)
-- TODO (Sprint 3): once Tournament Service is live, drop the FK constraints on
-- matches.team_a_id / team_b_id below and convert them to synthetic references
-- (same pattern as streams.streamer_id), OR keep a denormalized local copy
-- synced via Kafka. Decide one, don't leave both half-done.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS teams (
    team_id INT AUTO_INCREMENT PRIMARY KEY,
    team_name VARCHAR(100) NOT NULL UNIQUE,
    color_hex VARCHAR(7) NOT NULL DEFAULT '#FF0055',
    logo_url VARCHAR(255) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_teams_name (team_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 2. Table: matches (Tournament Fixtures Linking Teams)
-- Status vocabulary standardized to TitleCase across matches AND streams:
-- 'Scheduled' | 'Live' | 'Ended' | 'Cancelled'
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS matches (
    match_id INT AUTO_INCREMENT PRIMARY KEY,
    tournament_id INT NOT NULL DEFAULT 1,
    team_a_id INT NOT NULL,
    team_b_id INT NOT NULL,
    scheduled_time DATETIME NOT NULL,
    status ENUM('Scheduled', 'Live', 'Ended', 'Cancelled') NOT NULL DEFAULT 'Scheduled',
    winner_team_id INT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_matches_team_a FOREIGN KEY (team_a_id) REFERENCES teams(team_id) ON DELETE RESTRICT,
    CONSTRAINT fk_matches_team_b FOREIGN KEY (team_b_id) REFERENCES teams(team_id) ON DELETE RESTRICT,
    CONSTRAINT fk_matches_winner FOREIGN KEY (winner_team_id) REFERENCES teams(team_id) ON DELETE SET NULL,
    INDEX idx_matches_status (status),
    INDEX idx_matches_schedule (scheduled_time),
    INDEX idx_matches_tournament (tournament_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 3. Table: streams (Twitch Channel Ingestion & Embed Metadata)
-- streams.status is the column Story 21 actually updates — it's driven
-- directly by the Twitch webhook (StreamStarted/StreamEnded). Whether
-- matches.status cascades from this is a separate, explicit decision —
-- see the trigger note below.
-- Added: eventsub_subscription_id (Story 18, manage/renew/revoke the
-- subscription) and embed_parent_domain (Story 17 AC: Twitch-ToS-compliant
-- embed params must be stored).
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS streams (
    stream_id INT AUTO_INCREMENT PRIMARY KEY,
    streamer_id INT NOT NULL,                             -- Synthetic/Logical FK to UserService.users(user_id)
    tournament_id INT NULL DEFAULT 1,
    match_id INT NULL,
    channel_name VARCHAR(100) NOT NULL,                  -- Twitch channel login name (e.g., 'esl_csgo')
    platform ENUM('Twitch', 'YouTube') NOT NULL DEFAULT 'Twitch',
    stream_title VARCHAR(255) NOT NULL DEFAULT 'Arena Live Match Broadcast',
    twitch_broadcast_id VARCHAR(100) NULL,               -- Unique live broadcast ID from EventSub
    eventsub_subscription_id VARCHAR(100) NULL,           -- Twitch EventSub subscription ID (manage/renew/revoke)
    embed_parent_domain VARCHAR(255) NULL,                -- Required 'parent' param for Twitch embed ToS compliance
    status ENUM('Scheduled', 'Live', 'Ended', 'Cancelled') NOT NULL DEFAULT 'Scheduled',
    viewer_count INT NOT NULL DEFAULT 0,
    started_at DATETIME NULL,
    ended_at DATETIME NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_streams_match FOREIGN KEY (match_id) REFERENCES matches(match_id) ON DELETE SET NULL,
    INDEX idx_streams_channel (channel_name),
    INDEX idx_streams_status (status),
    INDEX idx_streams_streamer (streamer_id),
    INDEX idx_streams_match (match_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -----------------------------------------------------------------------------
-- 4. Table: webhook_message_logs (EventSub Message-ID Deduplication)
-- Added optional stream_id link so QA can trace which stream a given
-- webhook delivery belonged to during debugging.
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS webhook_message_logs (
    message_id VARCHAR(128) PRIMARY KEY,                 -- Value from Twitch-Eventsub-Message-Id header
    stream_id INT NULL,                                   -- Optional trace link back to streams.stream_id
    message_type VARCHAR(64) NOT NULL,                   -- e.g., 'stream.online', 'stream.offline'
    subscription_type VARCHAR(64) NULL,
    payload_hash VARCHAR(64) NULL,
    received_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_webhook_logs_stream FOREIGN KEY (stream_id) REFERENCES streams(stream_id) ON DELETE SET NULL,
    INDEX idx_dedup_received (received_at),
    INDEX idx_dedup_stream (stream_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================================
-- SEED DATA POPULATION
-- =============================================================================

-- 1. Populate Teams
INSERT INTO teams (team_id, team_name, color_hex, logo_url) VALUES
(1, 'Team Crimson', '#FF0055', 'https://assets.arena.gg/teams/crimson.png'),
(2, 'Team Cobalt', '#0077FF', 'https://assets.arena.gg/teams/cobalt.png'),
(3, 'Team Emerald', '#00FF66', 'https://assets.arena.gg/teams/emerald.png'),
(4, 'Team Shadow', '#8A2BE2', 'https://assets.arena.gg/teams/shadow.png')
ON DUPLICATE KEY UPDATE
    team_name = VALUES(team_name),
    color_hex = VALUES(color_hex),
    logo_url = VALUES(logo_url);

-- 2. Populate Scheduled Matches
INSERT INTO matches (match_id, tournament_id, team_a_id, team_b_id, scheduled_time, status) VALUES
(1, 1, 1, 2, DATE_ADD(NOW(), INTERVAL 2 HOUR), 'Scheduled'),
(2, 1, 3, 4, DATE_ADD(NOW(), INTERVAL 5 HOUR), 'Scheduled'),
(3, 1, 1, 3, DATE_ADD(NOW(), INTERVAL 1 DAY), 'Scheduled')
ON DUPLICATE KEY UPDATE
    scheduled_time = VALUES(scheduled_time),
    status = VALUES(status);

-- 3. Populate Associated Stream Broadcasts
-- streamer_id 999 and 998 represent mock/test user IDs
INSERT INTO streams (
    stream_id,
    streamer_id,
    tournament_id,
    match_id,
    channel_name,
    platform,
    stream_title,
    embed_parent_domain,
    status,
    viewer_count
) VALUES
(101, 999, 1, 1, 'mock_twitch_channel_1', 'Twitch', 'Grand Finals - Team Crimson vs Team Cobalt', 'localhost', 'Scheduled', 0),
(102, 998, 1, 2, 'mock_twitch_channel_2', 'Twitch', 'Semi Finals - Team Emerald vs Team Shadow', 'localhost', 'Scheduled', 0)
ON DUPLICATE KEY UPDATE
    channel_name = VALUES(channel_name),
    stream_title = VALUES(stream_title),
    embed_parent_domain = VALUES(embed_parent_domain),
    status = VALUES(status);