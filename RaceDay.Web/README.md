# RaceDay — Part 3: MVC Front End

ASP.NET Core 8 MVC web application for the RaceDay platform. It consumes the Part 2
REST API for **all** data operations (no direct database access, no business logic
duplication) and communicates with Azure **only** through the API.

## Architecture
- `RaceDay.Api` — Part 2 REST API, extended in Part 3 with Azure Blob Storage
  (event banners, profile pictures) and supporting endpoints.
- `RaceDay.Web` — ASP.NET Core MVC front end with session-based authentication,
  role-aware UI (Organiser / Participant), and a sporting RaceDay theme (Bootstrap 5).
- `RaceDay.Tests` — Unit tests carried over from Part 2.

## Prerequisites
- .NET 8 SDK
- SQL Server / SQL Express with `RaceDayDB` created from the Part 1 script
- Azure Storage account **or** Azurite / no storage (falls back to local disk in dev)

## Setup
1. Create the database with the Part 1 script, then run the Part 3 column script:
   `RaceDay.Api/Data/Part3_AddColumns.sql` (in SSMS, against `RaceDayDB`).
2. Configure `RaceDay.Api/appsettings.json`:
   - `ConnectionStrings:RaceDay` — your SQL Server.
   - `Storage:ConnectionString` — your Azure Storage connection string. Leave empty
     for local development; files are then stored on disk under `App_Data/blobs`
     with identical behaviour.
3. Configure `RaceDay.Web/appsettings.json`:
   - `Api:BaseUrl` — the URL the API runs on (default `http://localhost:5080`).

## Run locally
Terminal 1 (API):
```bash
dotnet run --project RaceDay.Api/RaceDay.Api.csproj
