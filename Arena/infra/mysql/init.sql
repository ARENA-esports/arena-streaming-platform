-- Arena shared MySQL initialization
-- Provisions the logical databases for Stream and Tournament services.
-- Tables are created by DbUp at service startup; this script only ensures the databases exist.
-- The arena_user_db database is provisioned by the mysql-user container via MYSQL_DATABASE env var.
CREATE DATABASE IF NOT EXISTS arena_stream_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS arena_tournament_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
