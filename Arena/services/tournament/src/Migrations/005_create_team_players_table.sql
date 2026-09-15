-- =============================================================================
-- Migration: 005_create_team_players_table
-- Service: TournamentService
-- Description: Creates team_players table linking competing roster players to teams
--              with indexed foreign keys for performant non-locking joined reads.
-- =============================================================================

CREATE TABLE IF NOT EXISTS team_players (
    player_id INT AUTO_INCREMENT PRIMARY KEY,
    team_id INT NOT NULL,
    username VARCHAR(100) NOT NULL,
    role VARCHAR(50) NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    CONSTRAINT fk_team_players_team FOREIGN KEY (team_id) REFERENCES teams(team_id) ON DELETE CASCADE,
    INDEX idx_team_players_team_id (team_id),
    INDEX idx_team_players_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed default teams if not present (ensures seed rosters can link)
INSERT INTO teams (team_id, team_name, color_hex, logo_url) VALUES
(1, 'Team Crimson', '#FF0055', 'https://assets.arena.gg/teams/crimson.png'),
(2, 'Team Cobalt', '#0077FF', 'https://assets.arena.gg/teams/cobalt.png'),
(3, 'Team Emerald', '#00FF66', 'https://assets.arena.gg/teams/emerald.png'),
(4, 'Team Shadow', '#8A2BE2', 'https://assets.arena.gg/teams/shadow.png')
ON DUPLICATE KEY UPDATE
    team_name = VALUES(team_name),
    color_hex = VALUES(color_hex),
    logo_url = VALUES(logo_url);

-- Seed initial roster players for competing teams (including active and inactive players)
INSERT INTO team_players (player_id, team_id, username, role, is_active) VALUES
(1, 1, 'ViperX', 'Captain', TRUE),
(2, 1, 'Blaze', 'Duelist', TRUE),
(3, 1, 'Phantom', 'Support', TRUE),
(4, 1, 'BenchWarmer_1', 'Substitute', FALSE),
(5, 2, 'Frostbite', 'Captain', TRUE),
(6, 2, 'Glacier', 'Initiator', TRUE),
(7, 2, 'Tidal', 'Flex', TRUE),
(8, 3, 'Verdant', 'Captain', TRUE),
(9, 3, 'Moss', 'Sentinel', TRUE)
ON DUPLICATE KEY UPDATE
    team_id = VALUES(team_id),
    username = VALUES(username),
    role = VALUES(role),
    is_active = VALUES(is_active);
