# RoadGuard Speed Alert System

Welcome to RoadGuard – a fully functional, production-ready hands-free speed monitoring and warning system.

## System Architecture

- **Mobile App**: Flutter (Android/iOS) with background location telemetry and deterministic overspeed evaluation.
- **Backend API**: .NET 8 Clean Architecture.
- **Database**: PostgreSQL (Entity Framework Core with automatic DB Migrations via `dotnet ef`).
- **Web App / Dashboard**: Next.js (React) Admin Dashboard fetching live system health and session metrics.
- **Location & Boundaries**: C#-based Provider pattern for resolving speed limits, heavily utilizing geo-bounding box caching for cost reduction and scale.

## End to End Capabilities
- **Passive Readiness & Auto-Start**: Mobile application uses `flutter_activity_recognition` and native background services. When motion > 15 kph is detected, it automatically begins a `DrivingSession` via the Backend API.
- **Real-Time Point Ingestion**: Location points and alert evaluations are pushed iteratively.
- **Overspeed Tolerance Verification**: A mobile engine filters noise on a rolling 10-second average, ensuring warnings only trigger after 3+ seconds of prolonged tolerance violation (Defaults to +5 km/h).
- **Graceful Degradation**: Offline or connection-refused limits correctly reflect as "Offline Fallback" states across the Mobile App and Dashboard UI. 

## Quick Start (Local Development)

```bash
docker-compose up --build
```
Once the containers are running:
- The **Backend API** will be accessible at `http://localhost:8080/swagger`
- The **PostgreSQL Database** operates on `localhost:5432`

To run the Next.js admin dashboard:
```bash
npm install
npm run dev
```

To run the Flutter app locally:
```bash
cd mobile
flutter pub get
flutter run
```

### Built-in Admin
An admin user is seeded directly on startup *only if explicitly configured*. Do NOT use default credentials in production. Configure your environment variables to set the admin seeding credentials:
- Email configuration: `Admin:Email`
- Password configuration: `Admin:Password`

## Deployment (Railway)

The environment is configured specifically for platforms like Railway.
1. Connect this GitHub repository.
2. Under "Add Service", choose "Database" -> provision a **PostgreSQL** node.
3. Under "Add Service", deploy the backend API by selecting the repository and linking the `./backend` directory. 
   - Define strict Env Variables to ensure the container starts:
     - `ConnectionStrings__DefaultConnection` (value = your PostgreSQL connection string).
     - `Jwt__Key` (e.g. your high-entropy secure key, >32 chars).
     - `Jwt__Issuer` and `Jwt__Audience` (optional, default strings used otherwise).
     - `Admin__Email` and `Admin__Password` (required to boot and securely seed).
     - `SpeedProvider__ApiKey` (Required to use Google Roads, otherwise defaults to offline -1 limit).
     - `AllowedOrigins__0` (e.g. `https://your-nextjs-app.railway.app` required for secure CORS & SignalR).
     - `RUN_MIGRATIONS=true` (Required to run EF setup and seed admin during init).
4. Under "Add Service", deploy the `./` root directory for the Next.js frontend, ensuring the environment variable `NEXT_PUBLIC_API_URL` points to your backend Railway domain.

## Mobile Application Setup
- Ensure the Mobile application base API URL is properly set via Environment flags or updated in `api_client.dart` if compiling for Production.

## Platform Testing & Verification

1. **Passive Readiness**: Grant location and activity permissions in the Flutter App. It will exhibit a "Waiting for vehicle motion" status.
2. **Auto-Detect**: Drive or simulate GPS speeds above 15 kph. The mobile app automatically shifts to "Active Monitoring" and communicates to the Cloud.
3. **Verify Dashboard Reflection**: Login to your web admin panel (`admin@speedalert.com`). The "Live Sessions" roster and the Active Session metrics will actively pulse green to signify data ingestion from the mobile node. 
4. **Alarms**: Purposefully exceed the local limit (Simulate > (Limit + 5 kph)) for 3+ seconds.
5. **Violation Log**: Observe the dashboard "Total Violations" value increment locally as the edge alert hits the Server. 
