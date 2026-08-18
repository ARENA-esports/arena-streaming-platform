**FOR ENTIRE DEVELOPMENT FOLLOW THIS DIRECTORY STRUCTURE**
           **IF MAKE ANY CHANGE NOTE IT IN HERE**

Arena/
├── .editorconfig                 # Shared formatting rules so `dotnet format` is consistent across all devs' IDEs
├── Arena.sln                     # Root solution referencing all 6 service projects for one-click IDE loading
├── README.md                     # System architecture, local setup, and deployment guide
│
├── .github/
│   ├── CODEOWNERS                # Maps each services/X/ folder to the Dev owning it that sprint (auto-requests reviewers)
│   └── workflows/                # GitHub Actions — one CI workflow per service, path-filtered
│       ├── ci-user.yml           # Build, format check, xUnit tests, CodeQL scan — triggers only on services/user/** changes
│       ├── ci-tournament.yml     # Same 4 gates, scoped to services/tournament/**
│       ├── ci-stream.yml         # Same 4 gates, scoped to services/stream/**
│       ├── ci-chat.yml           # Same 4 gates, scoped to services/chat/**
│       ├── ci-battle.yml         # Same 4 gates, scoped to services/battle/**
│       ├── ci-analytics.yml      # Same 4 gates, scoped to services/analytics/**
│       ├── ci-frontend.yml       # ESLint + production build, scoped to client/**
│       ├── cd-backend.yml        # On merge to main: builds Docker image (Git SHA tag), pushes to GHCR, deploys to Azure App Service
│       ├── cd-frontend.yml       # On merge to main: deploys React build to Azure Static Web Apps
│       └── integration-e2e.yml   # Cross-service Selenium/JMeter run, triggered on merge to main (not per-PR — too slow)
│
├── services/                     # All 6 microservices — identical internal shape for predictability across role rotation
│   ├── user/                     # User/Notification Service — auth, JWT issuance, follows, notifications
│   │   ├── src/                  # Controllers, Data (ADO.NET raw SQL), Services, Kafka producers/consumers, Migrations
│   │   ├── tests/                # xUnit unit tests for this service only
│   │   ├── Dockerfile            # Container build definition for this service
│   │   └── UserService.csproj
│   │
│   ├── tournament/                # Tournament Service — tournaments, teams, team colors (CRUD-heavy)
│   │   ├── src/
│   │   ├── tests/
│   │   ├── Dockerfile
│   │   └── TournamentService.csproj
│   │
│   ├── stream/                   # Stream Service — Twitch channel linking, EventSub webhook, HMAC verification, StreamStarted/Ended events
│   │   ├── src/
│   │   ├── tests/
│   │   ├── Dockerfile
│   │   └── StreamService.csproj
│   │
│   ├── chat/                     # Chat Service — WebSocket faction rooms, battleground room, denormalized team color
│   │   ├── src/
│   │   ├── tests/
│   │   ├── Dockerfile
│   │   └── ChatService.csproj
│   │
│   ├── battle/                   # Battle/Economy Service — coin-earning, weapon purchases, atomic battle bar, round-end guard
│   │   ├── src/
│   │   ├── tests/
│   │   ├── Dockerfile
│   │   └── BattleEconomyService.csproj
│   │
│   └── analytics/                # Analytics Service — pure Kafka consumer building a local read-model, dashboards, dynamic reports
│       ├── src/
│       ├── tests/
│       ├── Dockerfile
│       └── AnalyticsService.csproj
│
├── client/
│   └── arena-web/                # React frontend — video embed shell, team selection, chat panels, battle bar UI
│       ├── src/
│       ├── public/
│       ├── tests/
│       └── package.json
│
├── shared/
│   └── EventContracts/           # Plain DTOs only, no logic — the agreed shape of every Kafka event (StreamStarted, AttackSubmitted, etc.)
│       ├── src/
│       └── CHANGELOG.md          # Any field change here is a breaking change — note which services must redeploy together
│
├── tests/                        # Cross-service tests that don't belong to any single service
│   ├── e2e/                      # Selenium — full user journeys spanning multiple services (e.g. login → join stream → attack)
│   └── load/                     # JMeter — battle bar concurrency test, chat fan-out under load
│
├── infra/
│   ├── docker-compose.yml        # Kafka + MySQL only — stateful infra, kept out of the CI/CD app pipeline, updated manually
│   └── services/                 # Per-service compose overlays so a dev can run just what they need locally
│       ├── user.compose.yml
│       ├── tournament.compose.yml
│       ├── stream.compose.yml
│       ├── chat.compose.yml
│       ├── battle.compose.yml
│       ├── analytics.compose.yml
│       └── all.compose.yml       # Full 6-service stack — used for integration testing and demo rehearsal
│
└── docs/
    ├── Arena_SRS.md               # Software Requirements Specification
    ├── Arena_Product_Backlog.md   # 50-story backlog across 9 epics with MoSCoW prioritization
    ├── architecture-decisions.md  # Key decisions and rationale (Twitch embed, JWT pattern, concurrency approach, etc.)
    ├── hotfix-process.md          # Branch convention for fixing bugs in already-shipped services: hotfix/<service>-<desc>
    ├── api/                       # Swagger/OpenAPI exports, versioned per sprint
    │   ├── user-v1.json
    │   ├── tournament-v1.json
    │   └── ...
    └── test-reports/              # Coverage and load-test results, generated at the end of every sprint
        ├── sprint1-coverage.html
        ├── sprint1-jmeter-results.html
        └── ...
    └── commit-logs/               # One auto-generated log file per commit pushed to main — for QA traceability
        ├── 20260824-a1b2c3d.md    # Named by date + short SHA: commit author, timestamp, service(s) touched,
        │                          # commit message, linked story number, CI pass/fail status
        ├── 20260825-e4f5g6h.md
        └── ...