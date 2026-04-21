# Railway Deployment Guide

This project is built for seamless deployment on [Railway.app](https://railway.app/). Railway will host the ASP.NET Core 8 Web API and the PostgreSQL database.

## Prerequisites
1. A Railway account.
2. Railway CLI (optional but recommended).

## Steps to Deploy

1. **Create a New Project on Railway**:
   - Dashboard -> New Project -> "Deploy from GitHub repo" or "Empty Project".

2. **Add a Database**:
   - Right-click dashboard -> "New -> Database -> Add PostgreSQL".
   - This creates a fully managed DB. Note the connection variables.

3. **Deploy the App**:
   - If using CLI: `cd backend && railway up`
   - If using GitHub integration, link the repo. Choose the `backend/` directory as the Root Directory under Settings -> Build.

4. **Set Environment Variables**:
   Go to your API Service -> Variables. Add the following:
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `${{Postgres.DATABASE_URL}}` (Railway resolves this automatically if connected)
   - `Jwt__Issuer` = `speedalert-api`
   - `Jwt__Audience` = `speedalert-mobile`
   - `Jwt__Key` = `Generate_A_Random_Secure_String_Min_32_Chars!`
   - `SpeedProvider__ApiKey` = `YOUR_GOOGLE_ROADS_OR_TOMTOM_KEY`
   - `PORT` = `8080` (Railway automatically assigns $PORT, ASP.NET picks it up via configuration we set)

5. **Migrations & Execution**:
   - The application relies on EF Core Migrations tracking the database schema. **Important:** Before deploying to Railway, make sure you generated the migrations locally and committed them using:
     `dotnet ef migrations add InitialCreate --project SpeedAlert.Infrastructure --startup-project SpeedAlert.Api`
   - During boot, our `Program.cs` automatically invokes `db.Database.Migrate()` on startup to apply these pending migrations safely to your Railway Postgres Database.
   - We also automatically seed a default admin user via the `Admin:Email` and `Admin:Password` environment variables.

6. **Verify Build**:
   - Railway will build using the `Dockerfile` in `/backend`.
   - Once complete, go to Networking -> Generate Domain.
   - Test by visiting `https://<your-railway-domain>/health`.

## Known Limitations
- The internal speed providers (e.g. Google Roads) enforce their own rate limits. This backend caches lookups to prevent Railway egress / API limits.
