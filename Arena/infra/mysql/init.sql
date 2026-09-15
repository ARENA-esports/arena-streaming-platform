-- Arena shared MySQL initialization
-- Creates both arena_stream_db and arena_tournament_db.
-- Tables are created by DbUp at service startup; this script only provisions the databases.
CREATE DATABASE IF NOT EXISTS arena_stream_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
CREATE DATABASE IF NOT EXISTS arena_tournament_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
