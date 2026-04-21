# Speed Alert API Specification

## Authentication
**Base Path:** `/api/auth`
- `POST /register`: Accepts `email` & `password`. Returns JWT.
- `POST /login`: Accepts `email` & `password`. Returns JWT.
- `POST /refresh`: Accepts refresh token. Returns new JWT. (Requires Auth)
- `POST /logout`: Invalidates current session. (Requires Auth)
- `GET /me`: Returns current user profile. (Requires Auth)

## Users & Settings
**Base Path:** `/api/users` 
*(All endpoints require Auth)*
- `GET /me/settings`: Retrieves user's specific app configuration constraints (tolerance, alert delays).
- `PUT /me/settings`: Updates user configuration.

## Driving Sessions
**Base Path:** `/api/sessions`
*(All endpoints require Auth)*
- `POST /start`: Initializes a new Driving Session.
- `POST /{id}/pause`: Pauses active tracking.
- `POST /{id}/resume`: Resumes paused tracking.
- `POST /{id}/stop`: Ends tracking and computes summary metrics.
- `GET /`: Lists paginated history of user sessions.
- `GET /{id}`: Details for a specific session.
- `GET /{id}/summary`: Aggregated roll-up of session metrics (max speed, alerts count).

## Tracking
**Base Path:** `/api/tracking`
*(All endpoints require Auth)*
- `POST /{sessionId}/points`: Batch upload collected GPS signals.
- `POST /evaluate`: Send recent window of GPS points to evaluate backend confirmation of an overspeed violation.

## Speed Limits
**Base Path:** `/api/speed-limit`
*(All endpoints require Auth)*
- `POST /lookup`: Snap-to-road API translating `lat/lng` into official Speed Limit.
- `GET /providers/status`: Check upstream capacity for Google Roads / External Provider.

## System / Admin
**Base Path:** `/api/monitoring` and `/api/admin`
- `GET /api/monitoring/status`: Public heartbeat.
- `POST /api/monitoring/device-status`: Upload device diagnostics.
- `GET /api/admin/users`: List all users (Role=Admin).
- `GET /api/admin/health-overview`: Aggregate system metrics (Role=Admin).
