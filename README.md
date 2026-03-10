# TinyNote

A full-stack note-taking application built with a React/TypeScript frontend, an ASP.NET Core 9 REST API backend, and a PostgreSQL database. The entire stack can be run locally with Docker Compose or deployed to AWS ECS Fargate using AWS CDK.

## Architecture

```
┌─────────────────────────────────────────────────────     ┐
│                   AWS (CDK / ECS Fargate)                │
│                                                          │
│  Internet → ALB (port 80)                                │
│               ├── /api*  → API Service (port 8080)       │
│               └── /*     → Frontend Service (nginx)      │
│                                                          │
│  API ──────────────────────→ RDS PostgreSQL 16           │
│  API ──────────────────────→ ADOT Collector → CloudWatch │
└─────────────────────────────────────────────────────     ┘
```

| Layer | Technology |
|---|---|
| Frontend | React 18, TypeScript, Vite, Material UI, Redux Toolkit (RTK Query) |
| Backend API | ASP.NET Core 9, Entity Framework Core 9, AutoMapper, Serilog |
| Database | PostgreSQL 16 |
| Observability | OpenTelemetry (OTLP) → AWS ADOT Collector → CloudWatch EMF |
| Infrastructure | AWS CDK (.NET), ECS Fargate, RDS, ALB, ECR |
| Local Dev | Docker Compose |

## Project Structure

```
Scania/
├── TinyNoteBackEnd/
│   ├── TinyNote.Api/          # ASP.NET Core REST API
│   │   ├── Controllers/       # NotesController
│   │   ├── Services/          # Business logic
│   │   ├── Repository/        # Data access (EF Core)
│   │   ├── Data/              # DbContext & EF migrations
│   │   ├── DTOs/              # Request / Response models
│   │   ├── Metrics/           # OpenTelemetry custom meters
│   │   ├── Middleware/        # Global exception handling
│   │   └── Dockerfile
│   ├── TinyNotes.Tests/       # xUnit unit tests
│   │   ├── Controllers/
│   │   └── Services/
│   └── CDK/                   # AWS CDK stack (.NET)
│       └── src/Cdk/CdkStack.cs
├── TinyNoteFrontEnd/
│   └── TinyNote/              # React + Vite app
│       └── src/
│           ├── components/    # Notes list, AddNote modal
│           ├── services/      # RTK Query API client
│           └── store.ts
├── docker-compose.yml         # Local full-stack runner
└── build-ecs-images.ps1       # Build Docker images for ECS
```

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- (For local frontend dev) [Node.js 20+](https://nodejs.org/)
- (For local backend dev) [.NET 9 SDK](https://dotnet.microsoft.com/download)
- (For AWS deployment) [AWS CLI](https://aws.amazon.com/cli/), [AWS CDK CLI](https://docs.aws.amazon.com/cdk/v2/guide/getting_started.html), Node.js

### Run the full stack locally with Docker Compose

```bash
docker compose up --build
```

| Service | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:8082 |
| Swagger UI | http://localhost:8082/swagger |
| PostgreSQL | localhost:5432 |

The API runs database migrations automatically on startup.

### Run the frontend in dev mode (hot reload)

```bash
cd TinyNoteFrontEnd/TinyNote
npm install
npm run dev
```

The dev server starts at http://localhost:5173 and proxies `/api` requests to the running API container.

### Run the backend in dev mode

```bash
cd TinyNoteBackEnd
dotnet run --project TinyNote.Api
```

Make sure a PostgreSQL instance is reachable. The default connection string in `appsettings.json` points to `Host=postgres` (Docker Compose service name). Override it for local-only dev:

```bash
dotnet run --project TinyNote.Api \
  --ConnectionStrings:DefaultConnection="Host=localhost;Port=5432;Database=tinynote;Username=postgres;Password=postgres"
```

### Run the tests

```bash
cd TinyNoteBackEnd
dotnet test
```

## API Endpoints

All endpoints are prefixed with `/api`.

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/notes` | List notes (supports `userId`, `search`, `sortBy`, `sortOrder`) |
| `GET` | `/api/notes/{id}` | Get a single note by GUID |
| `POST` | `/api/notes` | Create a new note |
| `PUT` | `/api/notes` | Update an existing note |
| `DELETE` | `/api/notes/{id}` | Delete a note |

Full interactive documentation is available at `/swagger` when running in Development mode.

### Health check

There is no dedicated `/health` endpoint. The ALB target group (in the CDK stack) uses `GET /api/notes` as its health check path and treats both `HTTP 200` and `HTTP 400` as healthy responses. A `400` is returned when the required `userId` query parameter is missing, which still confirms the API process and database connection are alive. The interval is 30 seconds with a 5-second timeout.

## AWS Deployment

The CDK stack provisions all required AWS resources in a single `cdk deploy`.

### Infrastructure overview

- **VPC** – 2 availability zones, 1 NAT Gateway
- **RDS** – PostgreSQL 16 on `db.t3.micro` in private subnets
- **ECS Fargate cluster** – `tinynote-cluster` with a private Cloud Map namespace (`tinynote.local`)
- **Frontend service** – Fargate task behind an internet-facing ALB; serves the React app on port 80
- **API service** – Fargate task; ALB routes `/api*` here on port 8080
- **ADOT Collector** – Receives OTLP metrics from the API and exports to CloudWatch
- **CloudWatch Log Groups** – `/ecs/tinynote/api`, `/ecs/tinynote/frontend`, `/ecs/tinynote/collector`, `/aws/otel/tinynote-metrics`

### Deploy to AWS

1. **Bootstrap** your AWS account/region (first time only):

   ```bash
   cd TinyNoteBackEnd/CDK
   cdk bootstrap
   ```

2. **Deploy**:

   ```bash
   cdk deploy
   ```

   CDK will build the Docker images locally, push them to ECR, and create/update the CloudFormation stack. The ALB URL is printed as a stack output when the deploy completes.

3. **Destroy** (tears down all resources):

   ```bash
   cdk destroy
   ```

### Build ECS-compatible images locally (optional)

Use the helper script to build `linux/amd64` images that match the Fargate runtime:

```powershell
# Build both images
./build-ecs-images.ps1

# Build only the API image
./build-ecs-images.ps1 -Api

# Build only the frontend image
./build-ecs-images.ps1 -Frontend

# Use a custom tag
./build-ecs-images.ps1 -Tag latest
```

## Configuration

### API environment variables

| Variable | Default | Description |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | (postgres service) | PostgreSQL connection string |
| `Cors__AllowedOrigins` | `http://localhost:5173;http://localhost:3000` | Semicolon-separated list of allowed CORS origins |
| `OpenTelemetry__OtlpEndpoint` | *(not set)* | OTLP HTTP endpoint for metrics export (e.g. `http://collector.tinynote.local:4318`) |
| `ASPNETCORE_ENVIRONMENT` | `Development` | ASP.NET Core environment name |

### Logging

Serilog is configured via `appsettings.json`. Structured logs are written to the console in Development and to CloudWatch (via stdout captured by the ECS log driver) in Production.

## Observability

The API exports custom OpenTelemetry metrics (defined in `TinyNote.Api/Metrics/TinyNoteMetrics.cs`) alongside standard ASP.NET Core and .NET runtime metrics. In AWS, the ADOT Collector sidecar service receives these over OTLP HTTP (`collector.tinynote.local:4318`) and writes them to the `/aws/otel/tinynote-metrics` CloudWatch log group using the EMF format, making them queryable as CloudWatch metrics under the `TinyNote` namespace.

## Assumptions

The following assumptions were made during the design and implementation of this project:

**Authentication & users**
- There is no authentication layer. The user identity (`userId`) is a hardcoded GUID in the frontend (`d44dc55f-e08c-4db2-a918-3093f1e11848`) and passed as a query parameter to scope note retrieval. A real production system would derive the user identity from a JWT or session token.
- The `UserRepository` exists to support future user management but is not wired to any registration or login flow at this stage.

**Data model**
- Each note belongs to exactly one user and is identified by a server-generated GUID.
- A `summary` field is auto-generated server-side by truncating `content` to the first 50 characters. Clients do not supply a summary.
- Notes are soft-deleted by removal from the database; there is no archive or recycle-bin concept.

**Search**
- Full-text search is implemented using PostgreSQL `ILIKE` (case-insensitive pattern matching) across `title`, `content`, and `summary`. This is sufficient for small-to-medium datasets; a dedicated search index (e.g. pg_trgm, Elasticsearch) would be needed at scale.

**Security**
- Database credentials (`postgres`/`postgres`) are plain text in both the Docker Compose file and the CDK stack. This is intentional for development simplicity. A production deployment should store credentials in AWS Secrets Manager and inject them at runtime.
- **HTTPS is not configured.** The ALB is provisioned with an HTTP-only listener on port 80; no TLS certificate or HTTPS listener is added by the CDK stack. The public access point (`http://<alb-dns>`) therefore uses unencrypted HTTP. To enable HTTPS, an ACM certificate would need to be provisioned and an HTTPS listener (port 443) added to the ALB, with an HTTP → HTTPS redirect rule on port 80. The API container already listens on plain HTTP inside the VPC (which is correct behind a terminating load balancer), but that termination is not currently in place.
- CORS is locked to explicit origin lists; wildcard origins are never permitted.

**Infrastructure**
- The CDK stack deploys to a single AWS region with 2 availability zones and a single NAT Gateway to balance cost and availability for a non-production workload.
- All ECS services run on `linux/amd64` Fargate to match the Docker images built locally on Windows via `--platform linux/amd64`.
- The RDS instance uses `RemovalPolicy.DESTROY`, meaning a `cdk destroy` will permanently delete the database and all data.
- Desired task counts are set to 1 for all services (frontend, API, collector) to minimise cost. Scale these up before any production use.
- Open telemetry implementation is demonstration purpose only. Needs to improve for the production 

**Health checks**
- The API has no dedicated health-check endpoint (e.g. `/health` or `/healthz`). The ALB reuses `GET /api/notes` as a liveness probe, accepting `HTTP 200` (valid request) and `HTTP 400` (missing `userId` parameter) as healthy status codes. This is intentional to avoid adding infrastructure purely for health checking, given the small scope of the project. A proper `/health` endpoint backed by ASP.NET Core's `IHealthCheck` mechanism would be the recommended approach before production use.
