# Speed Alert

A hands-free, production-ready driving safety assistant that monitors speed limits automatically.

## Setup Instructions

### Backend (Local)
1. Ensure Docker, Docker Compose, and .NET 8 SDK are installed.
2. Run `docker-compose up -d db` to start PostgreSQL.
3. CD into `backend/SpeedAlert.Api` and run `dotnet run`.
4. API running at `http://localhost:5000/swagger`.

### Mobile (Local)
1. Ensure Flutter stable is installed.
2. Run `cd mobile` and `flutter pub get`.
3. Set your backend URL in a local environment configuration or directly in the API client.
4. Run `flutter run`.

## Deployment
See `railway-deploy.md` for exact steps to deploy the API to a production environment.

## Documentation
- [Architecture](architecture.md)
- [Hands-Free Guide](hands-free-mode.md)
- [Testing Guide](testing-guide.md)
- [Known Limitations](known-limitations.md)
