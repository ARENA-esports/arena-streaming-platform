Software Requirements Specification (SRS)

Project: Arena

Version: 1.0

Date: August 10, 2026

Author:

# Introduction

## Purpose

This document defines the software requirements for the Tournament Live Streaming Platform, a web-based system that allows viewers to watch live tournament streams, align with a team/side, and participate in team-specific and cross-team chat. The platform is built as a microservices architecture using event-driven messaging for real-time features.

## Scope

The system is a web application allowing viewers to:

* View a homepage showing currently live streams (a random selection, plus streams from followed streamers)
* Select a stream and choose a team to support, identified by a team color
* Passively earn coins by watching a stream
* Spend coins on weapons to attack the opposing team, contributing to a shared "battle bar"
* Experience round-based outcomes: when the battle bar fills for one side, the round ends and resets

Organizers/Admins continue to manage tournaments and teams; Streamers continue to broadcast via RTMP.

## Definitions, Acronyms, and Abbreviations

SRS – Software Requirements Specification

RTMP – Real-Time Messaging Protocol (used for stream ingestion)

HLS – HTTP Live Streaming (used for browser playback)

Kafka – distributed event streaming platform used for async messaging between services

NFR – Non-Functional Requirement

FR – Functional Requirement

# Overall Description

## Product Perspective

The system is a new, standalone platform composed of six independently deployable .NET microservices : Tournament, Stream, Chat, Analytics, and User/Notification ,Battle/Economy communicating asynchronously via Kafka. It is not a modification of an existing system, though it draws conceptually on established live-streaming platforms (e.g., Twitch) for its core interaction model

## User Classes and Characteristics

 Viewer**:** browses homepage, watches streams, selects a team per stream, chats, earns and spends coins, attacks opposing team

 Streamer**:** generates stream keys, broadcasts

 Tournament **Organizer/Admin:** creates tournaments, registers teams (now also responsible for defining each team's color)

 System **(Kafka consumers):** handles coin-earning ticks and bar-state broadcasts

## Operating Environment

Web applications are accessed via modern desktop and mobile browsers supporting HLS.js playback and WebSocket connections. Backend services run as Docker containers deployed to Azure App Service. Streamers require OBS Studio (or compatible RTMP-capable software) on their local machine.

## Design and Implementation Constraints

* Must use ASP.NET with ADO.NET and raw SQL (no ORM), per course requirements
* Minimum of four distinct .NET microservices
* Must use MySQL as the relational data store
* Must integrate Apache Kafka for event-driven communication between services
* Must be hosted on Azure
* Must include a CI/CD pipeline (GitHub Actions, containerized deployment)

# Specific Requirements

## Functional Requirements

*Tournament/Stream Service*

* FR1: The system shall allow an Organizer to create a tournament and register participating teams.
* FR2: The system shall allow a Streamer to generate a unique stream key for a scheduled stream.
* FR3: The system shall validate stream keys via webhook before accepting an RTMP ingest connection.
* FR4: The system shall transcode incoming RTMP streams to HLS for browser playback.
* FR5: The system shall publish a StreamStarted event to Kafka when a broadcast begins.
* FR6: The system shall publish a StreamEnded event to Kafka when a broadcast ends.

*Chat Service*

* FR7: The system shall allow a Viewer to select a team/side upon joining a stream.
* FR8: The system shall route a Viewer to the chat room corresponding to their selected team.
* FR9: The system shall provide an optional "battleground" chat room accessible to viewers from all teams.
* FR10: The system shall deliver chat messages to all members of a room in real time.

*Analytics Service*

* FR11: The system shall consume stream lifecycle and chat events from Kafka to compute viewer counts and team distribution.
* FR12: The system shall provide an Organizer/Admin-facing dashboard displaying tournament and stream analytics.

*User/Notification Service*

* FR13: The system shall allow users to register and authenticate.
* FR14: The system shall allow a Viewer to follow a Streamer.
* FR15: The system shall notify followers when a followed Streamer starts a broadcast, triggered by a Kafka event.

## Non-Functional Requirements

 NFR1: The system shall deliver chat messages to room members within 1 second under normal load.

 NFR2: The system shall maintain HLS playback latency within an acceptable range for a live-viewing experience (target: single-digit seconds glass-to-glass).

 NFR3: The system shall be available during scheduled tournament broadcasts, with graceful degradation of non-critical services (e.g., analytics) preferred over disruption of live stream/chat delivery.

 NFR4: The system shall isolate services such that failure or high load in the Analytics service does not degrade Stream or Chat service availability.

 NFR5: The system shall support automated build, test, and deployment via a CI/CD pipeline for all four microservices independently.

 NFR6: The system shall run locally via docker-compose, replicating the production service topology for development and testing.

 NFR7: The system shall enforce authentication on chat participation and stream-key generation endpoints.

 NFR8: All client-server communication shall use HTTPS/WSS.

## Assumptions and Dependencies

 Streamers have access to OBS Studio or equivalent RTMP-capable broadcasting software.

 Viewers have a stable internet connection sufficient for HLS playback.

 Azure student credits remain available and sufficient for the project duration; usage will be monitored to avoid exhaustion before final evaluation.

 Kafka and MySQL are provisioned as part of the deployment environment (via Docker containers in dev, managed/containerized services in Azure).

 Team capacity constrains scope features such as prediction markets or reaction heatmaps are treated as stretch goals, not baseline requirements, unless explicitly prioritized.