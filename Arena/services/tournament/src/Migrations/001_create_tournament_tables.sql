-- =============================================================================
-- Migration: 001_create_tournament_tables
-- Service: TournamentService
-- Target Database: arena_tournament_db
-- Target Engine: MySQL 8.0+ (InnoDB, UTF8mb4)
-- Description: Creates tournaments schema and initial season seed data.
-- =============================================================================

CREATE TABLE IF NOT EXISTS tournaments (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    season_identifier VARCHAR(100) NOT NULL,
    start_date DATETIME NOT NULL,
    end_date DATETIME NOT NULL,
    status ENUM('Scheduled', 'Active', 'Completed', 'Cancelled') NOT NULL DEFAULT 'Scheduled',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tournaments_status (status),
    INDEX idx_tournaments_season (season_identifier)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed initial tournament season
INSERT INTO tournaments (id, name, season_identifier, start_date, end_date, status)
VALUES (1, 'Arena Championship 2026', 'SEASON-2026-Q1', NOW(), DATE_ADD(NOW(), INTERVAL 30 DAY), 'Scheduled')
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    season_identifier = VALUES(season_identifier),
    start_date = VALUES(start_date),
    end_date = VALUES(end_date),
    status = VALUES(status);
