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
* **Local Development (`appsettings.Development.json`):**
  ```json
  "JwtSettings": {
    "Secret": "Arena_Secret_Key_For_Jwt_Token_Signing_2026_SE3022_Production_Grade!",
    "Issuer": "Arena.UserService",
    "Audience": "Arena.Platform",
    "ExpiryMinutes": 120
  }