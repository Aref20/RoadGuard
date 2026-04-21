# Speed Alert Architecture

## Overview
Speed Alert is a hands-free driving safety assistant consisting of a Flutter mobile application and a .NET 8 Web API backend using PostgreSQL. The mobile application is responsible for active sensor polling and offline buffering, while the backend serves as the source of truth for user auth, session metrics, complex analytics, and speed limit provider caching.

## Component Diagram

```text
[ Mobile App (Flutter) ]
   |-- Driving Detection Engine (Activity Recognition)
   |-- Speed Monitoring Engine (GPS, Background Service)
   |-- Local Cache (Hive/SQLite)
   |-- Alert Manager (Audio/Vibrate/Notification)
   |
   +-- (REST / HTTPS)
   |
[ Backend API (.NET 8) ]
   |-- Auth & User management (JWT)
   |-- Session Ingestion
   |-- Speed Limit Resolution (Cache + API)
   |
   +-- [ PostgreSQL Database ]
   +-- [ Google Roads API / External Speed Limit Provider ]
```

## Non-Negotiable Core Concepts
1. **Hands-Free Priority**: App transitions between `Passive Readiness`, `Driving Confirming`, `Active Monitoring`, and `Idle` via OS background activity transitions.
2. **Deterministic Alerts**: A single speeding sample NEVER triggers an alert. We use a buffer of recent location data, compute a smoothed speed (e.g., Simple Moving Average), and alert only after `Tolerance` is exceeded continuously for `AlertDelaySeconds`.
3. **Graceful Degradation**: If no signal, GPS, or speed limit provider fails, the app explicitly enters `Degraded Mode` rather than showing false confidence.

## Data Flow
- **Location Polling**: 1Hz when active. Buffered locally.
- **Limit Lookups**: Mobile app queries backend for limit via `snap-to-road`. Backend serves from PostgreSQL `RoadLookupCache` or falls back to Provider, reducing API costs.
- **Sync**: Telemetry and sessions sync when network is active. Offline points are queued.
