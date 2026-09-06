# JWT Specification (Story 4)

## 1. Cryptographic Configuration
* **Algorithm:** HMAC-SHA256 (`HS256`)
* **Issuer (`iss`):** `Arena.UserService`
* **Audience (`aud`):** `Arena.Platform`
* **Token Expiration:** 120 minutes from issuance

## 2. Token Payload (Claims Shape)
| Claim Key | Standard / Type | Example Value | Description |
| :--- | :--- | :--- | :--- |
| `sub` | `JwtRegisteredClaimNames.Sub` | `"999"` | Internal `user_id` (stringified integer). |
| `email` | `JwtRegisteredClaimNames.Email` | `"player@arena.gg"` | User email address. |
| `unique_name`| `ClaimTypes.Name` | `"ProGamer99"` | Unique display username. |
| `role` | `ClaimTypes.Role` | `"Streamer"` | Access tier: `Viewer`, `Streamer`, `Organizer`, `Admin`. |
| `jti` | `JwtRegisteredClaimNames.Jti` | `"c9bf9e57-..."` | Unique token identifier (GUID). |
| `exp` | Standard Unix Timestamp | `1724284800` | Expiration time. |

## 3. Secret & Configuration Convention

**Never commit a real secret value to this file or to any `appsettings.json` in the repo.** The value below is a placeholder only — every developer generates their own local secret.

* **Local Development (`appsettings.Development.json`, not committed):**
  ```json
  "JwtSettings": {
    "Secret": "<GENERATE_YOUR_OWN_LOCAL_SECRET_MIN_32_CHARS>",
    "Issuer": "Arena.UserService",
    "Audience": "Arena.Platform",
    "ExpiryMinutes": 120
  }
  ```
  Generate a local value yourself (e.g. a random 32+ character string) and store it only in your own untracked `appsettings.Development.json` or via `dotnet user-secrets` — never paste a real value into this spec, into `appsettings.json`, or into any committed file.

* **Staging / Production:** the signing secret is set via environment variable / Azure App Service application configuration / GitHub Actions repository secret at deploy time — it is never committed to source control in any form. Whoever configures the CD pipeline is responsible for setting this value directly in Azure/GitHub, not in a file.

## 4. Cross-Service Requirement

All six Arena microservices (User, Tournament, Stream, Chat, Battle/Economy, Analytics) must validate incoming JWTs against this **identical** Issuer, Audience, and Secret. Each service configures its own JWT bearer middleware independently (no central gateway — see architecture decision: per-service JWT validation), but all six must reference this exact spec so a token issued by User Service is accepted consistently everywhere. Implementation of this validation across all six services is covered by Story 5.

## 5. Team Sign-Off

This spec must be reviewed and agreed on by all 4 team members before Story 5 (per-service JWT middleware implementation) begins. Record the sign-off date and confirmation in the daily standup log once reviewed — this spec document alone does not satisfy that acceptance criterion.

*Sign-off status: pending — update this line once confirmed in the standup log.*
