# Arena Streaming Platform — Chat Service & Frontend Documentation

Comprehensive technical documentation covering the 5 backend stories for the Chat Service and the React frontend integration for the Arena esports streaming platform.

---

## Table of Contents
1. [Architecture Overview](#1-architecture-overview)
2. [Backend Stories (Stories 1 – 5)](#2-backend-stories-stories-1--5)
   - [Story 1: Chat Service Setup & WebSocket Faction Channels (SCRUM-108)](#story-1-chat-service-setup--websocket-faction-channels-scrum-108)
   - [Story 2: History Hydration & Message Persistence (SCRUM-109)](#story-2-history-hydration--message-persistence-scrum-109)
   - [Story 3: Denormalized Team Metadata Cache (SCRUM-110)](#story-3-denormalized-team-metadata-cache-scrum-110)
   - [Story 4: Kafka Event-Driven Team Cache Synchronization (SCRUM-111)](#story-4-kafka-event-driven-team-cache-synchronization-scrum-111)
   - [Story 5: Chat Moderation — User Muting & Profanity Filtering (SCRUM-112)](#story-5-chat-moderation--user-muting--profanity-filtering-scrum-112)
3. [Frontend Implementation & Polish](#3-frontend-implementation--polish)
   - [Dual-Faction Chat Hook (`useFactionChat.ts`)](#dual-faction-chat-hook-usefactionchatts)
   - [Chat Component (`FactionChat.tsx`)](#chat-component-factionchattsx)
   - [Side-by-Side Faction Selector (`TeamSelector.tsx`)](#side-by-side-faction-selector-teamselectortsx)
   - [Stream Player & Responsive Widescreen Layout (`MatchRoomView.tsx` & `StreamContainer.tsx`)](#stream-player--responsive-widescreen-layout-matchroomviewtsx--streamcontainertsx)
4. [WebSocket & REST API Specifications](#4-websocket--rest-api-specifications)
5. [Database Migrations & Schemas](#5-database-migrations--schemas)
6. [Local Environment & Docker Deployment](#6-local-environment--docker-deployment)
7. [Automated Test Suites](#7-automated-test-suites)

---

## 1. Architecture Overview

The Chat Service (`services/chat`) is an asynchronous, high-throughput real-time messaging microservice built using **ASP.NET Core 8**, **Raw WebSockets**, **MySQL**, and **Apache Kafka**. It operates alongside the **Tournament Service** and **User Service** to deliver low-latency match chats to the React frontend.

```
                      ┌───────────────────────────────────────────────┐
                      │              Tournament Service               │
                      │           (Port 8082 / MySQL 3308)            │
                      └──────────────────────┬────────────────────────┘
                                             │ Publishes: arena.teams.changed
                                             ▼
                               ┌───────────────────────────┐
                               │    Apache Kafka Broker    │
                               │        (Port 9092)        │
                               └─────────────┬─────────────┘
                                             │ Consumes: TeamCacheConsumer
                                             ▼
┌──────────────────────┐       ┌───────────────────────────┐       ┌──────────────────────┐
│  React Frontend Web  │◄─────►│       Chat Service        │◄─────►│    MySQL Database    │
│  (Port 5173 / Nginx) │ /ws   │ (Port 5169 / ASP.NET 8)   │       │  arena_chat_db:3310  │
└──────────────────────┘       └───────────────────────────┘       └──────────────────────┘
```

### Key Technical Characteristics
- **Raw WebSockets:** Zero external abstraction overhead (no SignalR or Socket.io); maximum control over byte transfers and frame deserialization.
- **Zero-HTTP Broadcast Latency:** Faction and team metadata (`teamName`, `teamColor`) are denormalized and maintained in the local Chat DB via Kafka event streaming.
- **Unified Dual-Faction Timeline:** Fans from both opposing factions participate in the same match stream chat while retaining distinct team color branding on their usernames.

---

## 2. Backend Stories (Stories 1 – 5)

### Story 1: Chat Service Setup & WebSocket Faction Channels (SCRUM-108)
* **Objective:** Create the standalone Chat Service and establish WebSocket connections separated by `teamId`.
* **Key Components & Files:**
  * `services/chat/src/WebSockets/FactionChannelManager.cs`: In-memory thread-safe connection registry backed by `ConcurrentDictionary<int, ConcurrentDictionary<string, WebSocket>>` managing active sockets per team channel.
  * `services/chat/src/WebSockets/ChatWebSocketMiddleware.cs`: Handles HTTP upgrade requests at `/ws/chat`. Validates mandatory `teamId` query parameter, authenticates JWT bearer tokens from query string (`?token=`) or auth cookie, and executes connection registration.
  * **Payload Validation:** Enforces non-empty messages and a maximum 500-character payload length.
  * **Lifecycle Management:** Graceful socket close handshake with timeout safeguards to prevent thread starvation.
* **Testing:** `FactionChannelManagerTests.cs`, `ChatWebSocketMiddlewareTests.cs`.

---

### Story 2: History Hydration & Message Persistence (SCRUM-109)
* **Objective:** Persist team messages in MySQL and automatically deliver recent message history to clients upon connection.
* **Key Components & Files:**
  * `services/chat/src/migrations/001_create_chat_messages.sql`: Created `chat_messages` table indexed on `(team_id, created_at DESC)`.
  * `services/chat/src/Repositories/ChatMessageRepository.cs`:
    * `InsertAsync(ChatMessage)`: Persists message entity and returns auto-generated `message_id`.
    * `GetRecentByTeamAsync(teamId, limit = 50)`: Retrieves latest 50 messages ordered chronologically.
  * **Automatic Hydration on Join:** Immediately upon connection upgrade, the server sends a typed history payload (`{ "type": "history", "messages": [...] }`) before client messages are accepted.
  * **Error Frames:** Sends `{ "type": "error", "message": "..." }` frames directly to the client socket when validation fails.

---

### Story 3: Denormalized Team Metadata Cache (SCRUM-110)
* **Objective:** Store team metadata inside `arena_chat_db` to enrich chat messages without making cross-service HTTP calls.
* **Key Components & Files:**
  * `services/chat/src/migrations/002_create_chat_team_cache.sql`: Created `chat_team_cache` table (`team_id`, `team_name`, `team_color`, `updated_at`).
  * `services/chat/src/Repositories/ChatTeamCacheRepository.cs`: Implemented `GetByIdAsync(teamId)` and idempotent `UpsertAsync(ChatTeamCache)`.
  * **Middleware Enrichment:** History and live broadcast payloads resolve `teamName` and `teamColor` hex directly from local cache, defaulting to `#FFFFFF` if unknown.

---

### Story 4: Kafka Event-Driven Team Cache Synchronization (SCRUM-111)
* **Objective:** Propagate team changes (creation and updates) from the Tournament Service to the Chat Service asynchronously via Apache Kafka.
* **Key Components & Files:**
  * **Kafka Infrastructure:** Single-node Kafka container running in KRaft mode (`arena-kafka:9092`) defined in `docker-compose.yml`.
  * **Event Contract:** `shared/EventContracts/src/Class1.cs`:
    ```csharp
    public record TeamChangedEvent(
        int TeamId,
        string TeamName,
        string TeamColor,
        string EventType, // "Created" | "Updated"
        DateTime Timestamp
    );
    ```
  * **Tournament Service Producer:**
    * `IKafkaProducerService.cs` & `KafkaProducerService.cs`: Uses `Confluent.Kafka` to publish events to `arena.teams.changed`.
    * Non-blocking design: Kafka publication errors are caught and logged so tournament operations succeed even if Kafka is temporarily offline.
  * **Chat Service Consumer:**
    * `services/chat/src/Consumers/TeamCacheConsumer.cs`: Hosted `BackgroundService` subscribing to `arena.teams.changed` with consumer group `chat-service-team-cache`.
    * Upserts received team metadata into `chat_team_cache` and commits offsets manually.
* **Testing:** `TeamCacheConsumerTests.cs` (5 unit tests covering deserialization, upserts, malformed JSON, and invalid IDs).

---

### Story 5: Chat Moderation — User Muting & Profanity Filtering (SCRUM-112)
* **Objective:** Enable automated keyword profanity scrubbing and role-based user muting for tournament organizers and moderators.
* **Key Components & Files:**
  * `services/chat/src/migrations/003_create_chat_mutes.sql`: Created `chat_mutes` table indexed on `(user_id, muted_until)`.
  * `services/chat/src/Services/ProfanityFilter.cs`: Compiled regex filter with word-boundary detection, replacing flagged profanities with `***`.
  * `services/chat/src/Repositories/ChatMuteRepository.cs`:
    * `IsUserMutedAsync(userId)`: Checks for active mutes where `muted_until > UTC_TIMESTAMP()`.
    * `MuteUserAsync(mute)`: Records temporary user mutes.
  * `services/chat/src/Controllers/ChatModerationController.cs`:
    * `POST /api/chat/moderation/mute`: Protected by `[Authorize(Roles = "Organizer,Moderator")]`.
  * **WebSocket Moderation Pipeline:**
    1. Rejects muted users with `{ "type": "error", "message": "You are currently muted." }`.
    2. Scrubs vulgar words with `profanityFilter.Scrub(content)` before database insertion and broadcasting.
* **Testing:** `ProfanityFilterTests.cs`, `ChatMuteRepositoryTests.cs`, `ChatModerationControllerTests.cs`.

---

## 3. Frontend Implementation & Polish

### Dual-Faction Chat Hook (`useFactionChat.ts`)
* **File:** `client/arena-web/src/hooks/useFactionChat.ts`
* **Simultaneous Connections:** Connects to both `teamAId` and `teamBId` WebSockets, merging incoming messages into one chronological feed.
* **Stream-Isolated Caching:**
  * Chat messages are stored in client `localStorage` keyed by `m{matchId}_{channelName}`.
  * Switching channels/streams instantly refreshes/wipes the chat for the new broadcast.
  * Returning to a previously visited stream restores its chat history.
* **Exponential Backoff:** Auto-reconnects with backoff (1s → 2s → 4s → 8s → 16s).

---

### Chat Component (`FactionChat.tsx`)
* **File:** `client/arena-web/src/components/chat/FactionChat.tsx`
* **Single-Line Inline Layout:**
  * Removed redundant `[TEAM NAME]` badges.
  * Formatted as `[Avatar] Username: Message` on one line.
  * Usernames and avatars are styled in the respective team color (`#EF4444` Crimson / `#00B8FC` Cobalt).
* **Interactive Controls:**
  * 500-character counter with warning color at 450+ characters.
  * Inline system error toasts for moderation mutes and connection drops.
  * Smart auto-scroll that pauses when the user scrolls up.

---

### Side-by-Side Faction Selector (`TeamSelector.tsx`)
* **File:** `client/arena-web/src/components/match/TeamSelector.tsx`
* Redesigned into a compact 2-column grid (`grid grid-cols-2 gap-2.5`).
* Shows team emblems, team names, and active checkmarks side by side, leaving maximum vertical height for the chat history below.

---

### Stream Player & Responsive Widescreen Layout (`MatchRoomView.tsx` & `StreamContainer.tsx`)
* **Files:**
  * `client/arena-web/src/views/MatchRoomView.tsx`
  * `client/arena-web/src/components/player/StreamContainer.tsx`
* **Widescreen Expansion:** Upgraded container width from `max-w-7xl` (1280px) to `max-w-[1920px] w-full px-3 sm:px-6 lg:px-8` to eliminate empty side spaces.
* **Dynamic Sizing:** Stream video column expands up to ~1350px on 1080p displays; chat sidebar scales between 390px and 470px.
* **Channel Switching Callback:** `StreamContainer` fires `onChannelChange` on custom input or preset selection (`Arena_streams`, `riotgames`, `esl_csgo`, `shroud`), triggering chat isolation.

---

## 4. WebSocket & REST API Specifications

### WebSocket: `/ws/chat`
* **URL:** `ws://<host>/ws/chat?teamId={teamId}&token={jwt}`
* **Authentication:** Valid JWT required in query parameter `token` or cookie `access_token`.

#### Inbound Frames (Server → Client)
1. **History Frame:**
   ```json
   {
     "type": "history",
     "messages": [
       {
         "messageId": 101,
         "teamId": 1,
         "teamName": "Team Crimson",
         "teamColor": "#EF4444",
         "userId": 1,
         "username": "IT24610788",
         "content": "Let's go Crimson!",
         "createdAt": "2026-09-27T04:18:17Z"
       }
     ]
   }
   ```
2. **Message Frame:**
   ```json
   {
     "type": "message",
     "messageId": 102,
     "teamId": 1,
     "teamName": "Team Crimson",
     "teamColor": "#EF4444",
     "userId": 1,
     "username": "IT24610788",
     "content": "Defend the payload!",
     "createdAt": "2026-09-27T04:20:00Z"
   }
   ```
3. **Error Frame:**
   ```json
   {
     "type": "error",
     "message": "You are currently muted."
   }
   ```

#### Outbound Frames (Client → Server)
* Plain text string payload (e.g. `"Defend the payload!"`), maximum 500 characters.

---

### REST: Moderation Mute Endpoint
* **Endpoint:** `POST /api/chat/moderation/mute`
* **Headers:** `Authorization: Bearer <JWT>`, `Content-Type: application/json`
* **Authorized Roles:** `Organizer`, `Moderator`
* **Request Body:**
  ```json
  {
    "userId": 42,
    "durationMinutes": 60,
    "reason": "Vulgar language and spamming"
  }
  ```
* **Responses:**
  * `200 OK`: `{"message": "User 42 has been muted for 60 minutes."}`
  * `400 Bad Request`: Validation failure.
  * `401 Unauthorized` / `403 Forbidden`: Authentication / role error.

---

## 5. Database Migrations & Schemas

### `001_create_chat_messages.sql` (`arena_chat_db`)
```sql
CREATE TABLE IF NOT EXISTS chat_messages (
    message_id    BIGINT AUTO_INCREMENT PRIMARY KEY,
    team_id       INT          NOT NULL,
    user_id       INT          NOT NULL,
    username      VARCHAR(50)  NOT NULL,
    content       VARCHAR(500) NOT NULL,
    created_at    TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_chat_team_time (team_id, created_at DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### `002_create_chat_team_cache.sql` (`arena_chat_db`)
```sql
CREATE TABLE IF NOT EXISTS chat_team_cache (
    team_id    INT PRIMARY KEY,
    team_name  VARCHAR(100) NOT NULL,
    team_color VARCHAR(7)   NOT NULL,
    updated_at TIMESTAMP    DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

### `003_create_chat_mutes.sql` (`arena_chat_db`)
```sql
CREATE TABLE IF NOT EXISTS chat_mutes (
    mute_id             BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id             INT          NOT NULL,
    muted_until         DATETIME     NOT NULL,
    reason              VARCHAR(255) NULL,
    created_at          TIMESTAMP    DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id  INT          NOT NULL,
    INDEX idx_user_muted_until (user_id, muted_until)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

---

## 6. Local Environment & Docker Deployment

All microservices run in Docker Compose:

```bash
cd Arena
docker compose up --build
```

### Port Mappings
| Service | Host Port | Internal Port | Description |
|---|---|---|---|
| **Web Frontend (Nginx)** | `5173` | `80` | React web application & reverse proxy |
| **Stream Service** | `5167` | `8080` | Match stream management |
| **User Service** | `5168` | `8080` | Auth & profiles |
| **Chat Service** | `5169` | `8080` | WebSocket & moderation REST hub |
| **Tournament Service** | `8082` | `8080` | Tournaments, teams, rosters |
| **Kafka Broker** | `9092` | `9092` | Team event sync bus |
| **MySQL (User)** | `3307` | `3306` | `arena_user_db` |
| **MySQL (Tournament)** | `3308` | `3306` | `arena_tournament_db` |
| **MySQL (Stream)** | `3309` | `3306` | `arena_stream_db` |
| **MySQL (Chat)** | `3310` | `3306` | `arena_chat_db` |

---

## 7. Automated Test Suites

### Running Chat Service Unit Tests
```bash
cd Arena/services/chat/tests
dotnet test
```
* **Coverage:** 64 unit tests covering WebSocket handshake, history hydration, persistence, Kafka consumer event handling, mute verification, and profanity scrubbing.

### Running Frontend Production Build
```bash
cd Arena/client/arena-web
npm run build
```
* **Status:** Verified with zero TypeScript or bundling errors.
