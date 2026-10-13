<div align="center">

# ⚔️ ARENA — Esports Streaming & Gamified Tournament Platform

[![CI Pipeline](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/ci.yml/badge.svg)](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/ci.yml)
[![CD Staging](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/cd-staging.yml/badge.svg)](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/cd-staging.yml)
[![Frontend Staging](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/cd-frontend-staging.yml/badge.svg)](https://github.com/ARENA-esports/arena-streaming-platform/actions/workflows/cd-frontend-staging.yml)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![React](https://img.shields.io/badge/React-18.x-61DAFB?logo=react&logoColor=black)](https://react.dev/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.x-3178C6?logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Kafka](https://img.shields.io/badge/Apache_Kafka-Event--Driven-231F20?logo=apachekafka&logoColor=white)](https://kafka.apache.org/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED?logo=docker&logoColor=white)](https://www.docker.com/)
[![Azure](https://img.shields.io/badge/Azure-Cloud_Deployed-0078D4?logo=microsoftazure&logoColor=white)](https://azure.microsoft.com/)

**Next-generation, cloud-native interactive esports broadcasting platform.**  
Viewers don't just watch tournament streams — they align with factions, earn passive watch rewards, engage in real-time faction chat, spend earned coins on weapon strikes, and sway live tournament rounds via a shared dynamic battle bar.

[Key Features](#-key-features) • [Architecture](#-system-architecture) • [Microservices](#-microservices-ecosystem) • [Quickstart](#-getting-started) • [Deployment](#-cloud--cicd-deployment) • [Testing](#-testing--quality)

</div>

---

## 🎮 Key Features

### 📺 High-Fidelity Broadcast Playback
- **Multi-Source Streaming**: Adaptive HLS (HTTP Live Streaming) glass-to-glass playback and integrated Twitch live stream embedding with automatic fallback.
- **RTMP Ingestion**: OBS Studio / hardware encoder RTMP broadcast ingestion with webhook-driven stream key cryptographic authentication.
- **Live Match Scheduling**: Tournament organizers can schedule matches, link live broadcasts, configure team rosters, and trigger match states.

### ⚔️ Gamified Viewer Economy & Faction Battles
- **Watch-to-Earn Engine**: Viewers passively earn platform coins through cryptographically signed heartbeat ticks verified against active stream liveness.
- **Faction Alignment**: Viewers choose sides (e.g. *Team Crimson* vs. *Team Cobalt*), gaining access to dedicated team tactical channels and cross-team battleground chat.
- **Weapon Shop & Atomic Strikes**: Viewers spend accumulated coins on dynamic attack weapons (e.g. EMP, Nuke, Railgun) to push a synchronized tug-of-war Battle Bar toward enemy territory.
- **Round State Machine**: High-concurrency atomic round resolution and automatic reset upon reaching threshold conditions, persisting round outcomes and victory history.

### 💬 Real-Time Faction Chat (SignalR)
- **Sub-Second Room Messaging**: Real-time communication across faction team rooms and the public battleground with sub-second message delivery.
- **Moderation Controls**: Role-based chat moderation enabling tournament organizers to mute disruptive participants and filter toxic content.

### 📊 Comprehensive Organizer Analytics
- **Viewer Engagement Dashboard**: Live aggregates of total watch hours, coin velocity, active participant counts, and viewer distribution.
- **Battle & Attack Stats Dashboard**: Real-time inspection of team strike volumes, damage distribution per weapon class, and historical round win/loss rates.

### 🔐 Enterprise-Grade Identity & RBAC
- **Dual Authentication**: Native email/password authentication with BCrypt hashing alongside Twitch OAuth2 authorization.
- **Stateless Role Synchronization**: Instant, zero-downtime RBAC role switching between `Viewer` and `Organizer` with automatic JWT claim alignment and session preservation.

---

## 🏗️ System Architecture

ARENA is built on an **Event-Driven Microservices Architecture** orchestrated asynchronously through **Apache Kafka** and containerized with **Docker**:

```
                              ┌───────────────────────────────────┐
                              │     ARENA Web Client (React 18)   │
                              │  Vite · TypeScript · Tailwind CSS │
                              └───────────────┬───────────────────┘
                                              │ HTTPS / WSS
                                              ▼
 ┌────────────────────────────────────────────────────────────────────────────────────────┐
 │                                   API Gateway Layer                                    │
 └───────┬──────────────┬──────────────┬──────────────┬──────────────┬─────────────┬──────┘
         │              │              │              │              │             │
         ▼              ▼              ▼              ▼              ▼             ▼
   ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐ ┌───────────┐
   │   User    │  │  Stream   │  │Tournament │  │   Chat    │  │  Battle / │ │ Analytics │
   │  Service  │  │  Service  │  │  Service  │  │  Service  │  │  Economy  │ │  Service  │
   │ (.NET 8)  │  │ (.NET 8)  │  │ (.NET 8)  │  │ (.NET 8)  │  │ (.NET 8)  │ │ (.NET 8)  │
   └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘ └─────┬─────┘
         │              │              │              │              │             │
         │ ADO.NET      │ ADO.NET      │ ADO.NET      │ SignalR      │ ADO.NET     │ ADO.NET
         ▼              ▼              ▼              ▼              ▼             ▼
   ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐  ┌───────────┐ ┌───────────┐
   │   MySQL   │  │   MySQL   │  │   MySQL   │  │ In-Memory │  │   MySQL   │ │   MySQL   │
   │ (User DB) │  │(Stream DB)│  │(Tourn DB) │  │  Hub & DB │  │(Battle DB)│ │(Analy DB) │
   └───────────┘  └───────────┘  └───────────┘  └───────────┘  └───────────┘ └───────────┘
         │              │              │              │              │             │
         └──────────────┴──────────────┼──────────────┴──────────────┴─────────────┘
                                       │ Event Streaming
                                       ▼
                       ┌───────────────────────────────┐
                       │     Apache Kafka Event Bus    │
                       │  StreamLifecycle · CoinEarned │
                       │  WeaponAttack    · RoundEnd   │
                       └───────────────────────────────┘
```

---

## 📦 Microservices Ecosystem

| Service | Technology | Port | Core Responsibilities |
| :--- | :--- | :--- | :--- |
| **`User Service`** | .NET 8 / ADO.NET | `5168` | User registration, authentication, BCrypt, JWT minting & refresh, Twitch OAuth exchange, profile & avatar management, RBAC role updates. |
| **`Stream Service`** | .NET 8 / ADO.NET / NGINX | `5167` | Match scheduling, RTMP ingest key generation, webhook validation, stream status state machine, HLS transcoding. |
| **`Tournament Service`** | .NET 8 / ADO.NET | `5000` | Tournament bracket creation, tournament lifecycle, team registrations, team faction colors and rosters. |
| **`Chat Service`** | .NET 8 / SignalR | `5271` | Real-time WebSocket faction chat hubs, room routing (Team Crimson, Team Cobalt, Battleground), chat moderation. |
| **`Battle / Economy`** | .NET 8 / ADO.NET | `5050` | Watch-time heartbeat tracking, passive coin calculation & caps, weapon inventory, atomic battle-bar manipulation, round end triggers. |
| **`Analytics Service`** | .NET 8 / Kafka Consumer | `5169` | Real-time event consumption, stream engagement aggregation (watch seconds, coin velocity), battle summary read models. |
| **`Frontend Client`** | React 18 / Vite / TS | `5173` | Responsive cyber-esports user interface, HLS/Twitch stream players, live battle bar HUD, real-time chat, organizer analytics. |

---

## 🛠️ Technology Stack

- **Frontend**: React 18, TypeScript, Vite, Tailwind CSS, Lucide Icons, Axios, Formik & Yup, HLS.js, `@microsoft/signalr`.
- **Backend**: ASP.NET Core 8 Web API, ADO.NET (Raw SQL & Stored Procedures for maximum throughput), SignalR.
- **Messaging**: Apache Kafka & Confluent Schema Registry (asynchronous event-driven inter-service messaging).
- **Database**: MySQL 8.0 (Domain-driven database per microservice design).
- **Media Ingestion**: RTMP Media Server via NGINX with RTMP module.
- **DevOps & Cloud**: Docker, Docker Compose, GitHub Actions CI/CD, Azure App Service (Linux Containers), Azure Container Apps, Vercel.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+ & npm](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) with Docker Compose support
- Git

### 1. Clone the Repository
```bash
git clone https://github.com/ARENA-esports/arena-streaming-platform.git
cd arena-streaming-platform
```

### 2. Environment Configuration
Create an environment file by copying the template:
```bash
cp Arena/.env.example Arena/.env
```
Ensure required secrets (JWT secret keys, database credentials, Twitch client IDs) are defined.

### 3. Launch via Docker Compose (Recommended)
Spin up the complete microservices topology, databases, and Kafka broker in a single command:
```bash
cd Arena
docker compose up --build -d
```
Verify container statuses:
```bash
docker compose ps
```

### 4. Running the Frontend Locally (Dev Mode)
For client-side development with hot-module replacement (HMR):
```bash
cd Arena/client/arena-web
npm install
npm run dev
```
Open **[http://localhost:5173](http://localhost:5173)** in your browser.

---

## 🧪 Testing & Quality

ARENA maintains high test coverage through automated unit, integration, and UI test suites.

### Backend Unit & Integration Tests (.NET)
Execute tests across all microservices:
```bash
dotnet test Arena/Arena.slnx
```

### Frontend Unit & Component Tests (Jest)
Run client unit tests and component suites:
```bash
cd Arena/client/arena-web
npm test
```

### End-to-End & Load Testing
- **Selenium**: Automated browser flows under `Arena/tests/` verifying match room playback, faction chat, and heartbeat execution.
- **Apache JMeter**: Performance test plans located in `Arena/client/arena-web/` simulating concurrent coin-earning viewers and battle-bar assault spikes.

---

## ☁️ Cloud & CI/CD Deployment

The platform utilizes automated GitHub Actions pipelines targeting staging and production environments:

- **`ci.yml`**: Triggers on pull requests and pushes across all service paths; runs .NET test suites, static analysis, and Docker build validations.
- **`cd-staging.yml`**: Path-filtered continuous deployment pipeline automatically rebuilding and updating affected Azure App Services upon merging into `dev`.
- **`cd-frontend-staging.yml`**: Builds and deploys the containerized React single-page application to GHCR and hosting environments.

---

## 🔒 Security Practices

- **Zero-Trust Identity**: Strictly typed JWTs signed with HMAC-SHA256 containing expiration and JTI claims blacklisted upon logout.
- **Rate Limiting**: IP-partitioned and User-partitioned fixed-window rate limiters protecting authentication, profile mutation, and watch ticks against DDoS and credential abuse.
- **CORS & CSP**: Explicit whitelisting of trusted origins with strict cookie policies (`SameSite=Strict`, `HttpOnly`, `Secure`).
- **SQL Injection Prevention**: All database access executes through parameterized queries via ADO.NET `MySqlCommand.Parameters`.

---

## 👥 Branching Strategy & Contribution

- **`main`**: Production-ready, stable releases.
- **`dev`**: Staging integration branch where feature branches converge.
- **`feat/*`**: Topic feature branches branched from `dev` and merged via Pull Requests.

---

<div align="center">
  <sub>Built with passion for esports by the ARENA Engineering Team.</sub>
</div>