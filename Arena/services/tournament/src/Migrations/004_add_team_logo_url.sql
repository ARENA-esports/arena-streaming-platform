-- =============================================================================
-- Migration: 004_add_team_logo_url
-- Service: TournamentService
-- Description: Add logo_url column to teams table for team visual branding.
-- =============================================================================

ALTER TABLE teams ADD COLUMN logo_url VARCHAR(255) NULL AFTER color_hex;
