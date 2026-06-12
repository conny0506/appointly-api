# Appointly API

[![CI](https://github.com/conny0506/appointly-api/actions/workflows/ci.yml/badge.svg)](https://github.com/conny0506/appointly-api/actions/workflows/ci.yml)

A production-grade appointment booking REST API built with **ASP.NET Core 10**, **EF Core** and **PostgreSQL**. It models a real scheduling domain — staff working hours, conflict-free slot allocation, cancellation policies — with the business rules isolated in a pure, unit-tested domain layer.

## Features

- **Conflict-free booking** — overlap detection runs inside a database transaction, so two concurrent requests can never claim the same slot
- **Availability search** — free slots computed from working hours minus existing appointments, on a 15-minute grid
- **Business rules as code** — no past bookings, appointments must fit working hours, cancellations require 2 hours notice; all enforced in `BookingPolicy`, a dependency-free static class
- **JWT authentication** with `Admin` / `Customer` roles
- **Rate limiting** — global fixed-window limiter per IP, with a tighter policy on credential endpoints
- **PBKDF2-SHA256** password hashing with per-password salt and constant-time verification
- **Swagger UI** with bearer-token support, structured logging via Serilog, `/health` endpoint

## Architecture

```
src/
  Appointly.Domain/          Entities + booking rules. Zero dependencies, fully unit-testable.
  Appointly.Infrastructure/  EF Core DbContext, migrations, seeding, password hashing.
  Appointly.Api/             Controllers, JWT auth, rate limiting, Swagger, DI wiring.
tests/
  Appointly.Tests/           Domain unit tests + end-to-end API tests (in-memory SQLite).
```

The domain layer knows nothing about HTTP or the database: `BookingPolicy.CanBook()` takes entities and a clock, returns a verdict. The API layer translates verdicts into RFC 7807 problem responses.

## API Overview

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| POST | `/api/auth/register` | — | Create a customer account, returns JWT |
| POST | `/api/auth/login` | — | Returns JWT |
| GET | `/api/services` | — | List active services |
| POST/PUT | `/api/services` | Admin | Manage services |
| GET | `/api/providers` | — | List providers with services & working hours |
| GET | `/api/providers/{id}/availability?serviceId=&date=` | — | Free slots for a day |
| POST | `/api/providers` | Admin | Create provider |
| PUT | `/api/providers/{id}/working-hours` | Admin | Replace weekly schedule |
| PUT | `/api/providers/{id}/services` | Admin | Assign services |
| POST | `/api/appointments` | Customer | Book a slot |
| GET | `/api/appointments/mine` | Customer | My appointments |
| POST | `/api/appointments/{id}/cancel` | Customer/Admin | Cancel (cutoff enforced for customers) |
| GET | `/api/appointments` | Admin | Filter by provider/date |
| POST | `/api/appointments/{id}/status` | Admin | Confirm / complete / no-show |

## Quick Start

```bash
# 1. Start PostgreSQL + API
docker compose up --build

# 2. Swagger UI
open http://localhost:8080/swagger
```

Or run locally against the dockerized database only:

```bash
docker compose up -d postgres
dotnet run --project src/Appointly.Api
```

A demo provider (Mon–Fri 09:00–17:00) and admin account (`admin@appointly.local` / `ChangeMe!123`) are seeded on first start.

## Tests

```bash
dotnet test
```

- **Unit tests** cover the booking rules: overlap edge cases (back-to-back is allowed, mid-overlap is not), working-hours boundaries, cancellation cutoff, availability grid.
- **Integration tests** boot the real HTTP pipeline via `WebApplicationFactory` against in-memory SQLite and walk the full journey: register → browse providers → check availability → book → double-booking rejected → cancel → slot freed.

## Configuration

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings__Default` | PostgreSQL connection string | localhost |
| `Jwt__Secret` | HMAC signing key (required outside Development) | — |
| `Jwt__AccessTokenMinutes` | Token lifetime | 60 |
| `Bootstrap__AdminEmail` / `Bootstrap__AdminPassword` | Seeded admin credentials | see appsettings |
| `RateLimiting__PermitLimit` | Requests per window per IP | 100 |
| `RateLimiting__AuthPermitLimit` | Auth requests per window per IP | 10 |

## Tech Stack

ASP.NET Core 10 · EF Core 10 + Npgsql · PostgreSQL 17 · xUnit · Serilog · Swagger (Swashbuckle) · Docker
