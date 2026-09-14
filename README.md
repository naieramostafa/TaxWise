# Streamline Tax and Compliance - Backend

Backend API for the Streamline Tax and Compliance application. A .NET 10 Clean Architecture solution with CQRS (MediatR), PostgreSQL, Redis, S3 storage, OCR receipt processing, and JWT authentication.

---

## Prerequisites

- .NET SDK 10.0.400+
- PostgreSQL 14+
- Redis 6+
- MinIO (S3-compatible) or AWS S3
- Seq (structured logging, optional)

---

## Project Structure

```
StreamlineTax.sln
src/
  StreamlineTax.Domain/           Entities, enums, domain rules
  StreamlineTax.Application/      CQRS commands/queries, FluentValidation
  StreamlineTax.Infrastructure/   Services, DB context, external integrations
  StreamlineTax.Api/              REST API controllers, middleware, startup
  streamline-tax-ui/              Angular 19 frontend (separate)
```

---

## Quick Start

### 1. Start Infrastructure (Docker)

```bash
docker run -d --name streamline-postgres -e POSTGRES_PASSWORD=<your_password> -e POSTGRES_DB=streamline_tax -p 5432:5432 postgres:16
docker run -d --name streamline-redis -p 6379:6379 redis:7-alpine
docker run -d --name streamline-minio -p 9000:9000 -p 9001:9001 -e MINIO_ROOT_USER=<your_user> -e MINIO_ROOT_PASSWORD=<your_password> minio/minio server /data --console-address ":9001"
docker run -d --name streamline-seq -p 5341:80 datalust/seq:latest
```

Or use `start-dev.cmd` which handles everything.

### 2. Set Environment Variables

The `start-dev.cmd` script sets these automatically:

```
JWT_KEY              # JWT signing key (min 32 characters)
ASPNETCORE_URLS      # API listen address
Smtp__Host           # SMTP server host
Smtp__Port           # SMTP server port
Smtp__Username       # SMTP username
Smtp__Password       # SMTP password / API key
Smtp__From           # Sender email address
OCRSPACE_API_KEY     # OCR.space API key
```

### 3. Run the API

```bash
# From solution root
dotnet run --project src/StreamlineTax.Api

# Or use the dev script
start src\StreamlineTax.Api\start-dev.cmd
```

API starts at **http://localhost:5238**
Swagger UI at **http://localhost:5238/swagger**

---

## Architecture

### Clean Architecture Layers

- **Domain** (`StreamlineTax.Domain`) - Entities, enums, domain rules. No dependencies.
- **Application** (`StreamlineTax.Application`) - CQRS handlers, FluentValidation, business logic.
- **Infrastructure** (`StreamlineTax.Infrastructure`) - EF Core, external services, JWT, background jobs.
- **API** (`StreamlineTax.Api`) - Controllers, middleware, DI composition root.

### CQRS with MediatR

Commands and queries organized by feature:

```
Application/
  Tax/            -> GetTaxSummary
  Transactions/   -> GetTransactions, CategorizeTransaction, UpdateTransaction, DeleteTransaction
  Receipts/       -> UploadReceipt, GetReceipts
  Demo/           -> SeedDemoData
```

---

## API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user (email, password, name)
- `POST /api/auth/login` - Login (returns JWT + refresh token)

### Transactions

- `GET /api/transactions` - List user transactions (optional query: year, month, category)
- `POST /api/transactions` - Create transaction
- `PUT /api/transactions/{id}` - Update transaction (ownership required)
- `DELETE /api/transactions/{id}` - Delete transaction (ownership required)
- `PUT /api/transactions/{id}/category` - Categorize transaction (ownership required)

### Tax Periods

- `GET /api/taxperiods` - List periods with comparison data
- `GET /api/taxperiods/{id}` - Get period details (ownership required)
- `POST /api/taxperiods/{id}/close` - Close period (ownership required)
- `POST /api/taxperiods/{id}/lock` - Lock period (ownership required)

### Tax Summary

- `GET /api/tax/summary` - Get tax summary with income, withholdings, deductions

### Receipts

- `POST /api/receipts` - Upload receipt (multipart/form-data)
- `GET /api/receipts` - List user receipts
- `GET /api/receipts/{id}` - Get receipt with OCR data

### Notifications

- `GET /api/notifications` - List notifications
- `PUT /api/notifications/{id}/read` - Mark as read

### Demo

- `POST /api/demo/seed` - Seed demo data (6 sample transactions)

---

## Key Features

### Authentication & Security

- JWT Bearer authentication with refresh tokens
- ASP.NET Core Identity for password hashing
- Per-user data isolation (all queries filter by UserId)
- Rate limiting on authentication endpoints
- Owner-only access to transactions, periods, and receipts

### OCR Receipt Processing

- **OCR.space** cloud API (primary) - free tier, 1000 req/month
- **Tesseract** (fallback) - local OCR with image preprocessing
- Extracts: merchant name, amount, date from receipt images
- Background processing via Hangfire

### Email Notifications

- SendGrid SMTP integration (port 587)
- Non-blocking fire-and-forget email sending
- HTML email templates for notifications

### File Storage

- AWS S3 / MinIO for receipt file storage
- Configurable via S3 endpoint for local development

### Database

- PostgreSQL via EF Core (Npgsql)
- Redis caching for frequently accessed data
- Hangfire with PostgreSQL for background job storage

---

## Domain Model

### Entities

- `AppUser` - User account (extends IdentityUser)
- `Transaction` - Income transaction with amount, category, tax withholding
- `TaxPeriod` - Quarterly tax period (Open/Closed/Locked)
- `TaxAccount` - Tax account grouping periods
- `Receipt` - Uploaded receipt with OCR data
- `Notification` - User notification
- `RefreshToken` - JWT refresh token

### Enums

- `TransactionCategory` - Uncategorized, Salary, Freelance, BusinessIncome, Investment, OtherIncome
- `TaxPeriodStatus` - Open, Closed, Locked
- `ReceiptStatus` - Pending, Processed, Failed
- `NotificationType` - Various notification types

---

## Tech Stack

- **Runtime**: .NET 10.0
- **Database**: PostgreSQL 16 (Npgsql + EF Core)
- **Cache**: Redis 7 (StackExchange)
- **Storage**: AWS S3 / MinIO
- **Auth**: JWT Bearer + ASP.NET Core Identity
- **CQRS**: MediatR 14
- **Validation**: FluentValidation 12
- **Background Jobs**: Hangfire
- **OCR**: OCR.space API + Tesseract 5.2
- **Email**: MailKit + SendGrid
- **Logging**: Serilog + Seq
- **API Docs**: Swashbuckle (Swagger)

---

## Running Tests

```bash
dotnet test
```

---

## Project References

- **Api** references Application and Infrastructure
- **Application** references Domain
- **Infrastructure** references Domain and Application
- **Domain** has no project references (outermost layer)
