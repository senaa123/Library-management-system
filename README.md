# Book Ledger - Library Management System

Book Ledger is a full-stack library management system built with ASP.NET Core Web API and React. It supports book catalog management, member accounts, reservations, loans, returns, renewals, fines, QR-based member verification, Stripe fine payments, and role-based staff workflows.

## Project Description

**Book Ledger - Library Management System (ASP.NET Core, C#, Entity Framework Core, SQLite, React, TypeScript, Vite, Tailwind CSS, JWT, Stripe, QR Scanner) - GitHub**

Built a full-stack library management system for managing books, members, reservations, loans, renewals, returns, and fines. Implemented role-based authentication, QR-code member verification, automated reservation expiry, fine calculation, Stripe payments, and transaction tracking with a clean layered backend architecture.

## Tech Stack

| Layer | Technologies |
| --- | --- |
| Frontend | React, TypeScript, Vite, Tailwind CSS, Axios, React Router |
| Backend | ASP.NET Core Web API, C#, .NET 10 |
| Data | Entity Framework Core, SQLite |
| Auth | JWT authentication, BCrypt password hashing, role-based authorization |
| Payments | Stripe checkout for fine payments |
| QR | `html5-qrcode`, `qrcode.react` |

## Features

- User registration and login with JWT authentication.
- Role-based access for Admin, Librarian, and Member users.
- Book catalog browsing, searching, filtering, creation, editing, and removal.
- Staff book section for managing inventory and available copies.
- Member reservation workflow with waiting queue and pickup windows.
- Automated reservation expiry through a hosted background service.
- Staff loan issuing, returns, and renewal workflows.
- QR-code member scanning for faster book issuing.
- Member dashboards for borrowed books, reservations, and fines.
- Fine calculation for overdue, damaged, lost, missing-page, and reservation no-show cases.
- Stripe checkout integration for online fine payments.
- Desk payment recording for staff.
- Transaction history for circulation, reservation, and fine events.
- Member restriction and circulation limits based on outstanding fines.

## Architecture

The backend follows a layered structure:

```text
Domain/          Core entities and enums
Application/     Services, DTOs, requests, business rules, abstractions
Infrastructure/  EF Core persistence, repositories, auth, security, payments
API/             Controllers, dependency injection, configuration, hosted services
client/          React + TypeScript frontend
```

## Requirements

- .NET 10 SDK
- Node.js and npm
- SQLite
- Stripe account keys, only if testing online fine checkout

## Backend Setup

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project API
```

The API runs on:

```text
http://localhost:5156
https://localhost:7225
```

OpenAPI is available in development through the ASP.NET Core OpenAPI endpoint.

## Frontend Setup

From the repository root:

```bash
cd client
npm install
npm run dev
```

The frontend runs on:

```text
http://localhost:5173
```

## Configuration

Main settings live in `API/appsettings.json`.

Important sections:

- `ConnectionStrings:DefaultConnection` - SQLite database path.
- `Jwt` - token key, issuer, and audience.
- `Library` - loan periods, renewal limits, fine amounts, reservation hold days, and circulation limits.
- `Stripe` - publishable key, secret key, client URL, and currency.
- `DefaultAdmin` - first admin account seeded during startup.

For local development, place secrets in `API/.env` instead of committing them.

Example:

```env
Jwt__Key=your-local-jwt-secret
DefaultAdmin__Password=your-local-admin-password
Stripe__PublishableKey=pk_test_xxx
Stripe__SecretKey=sk_test_xxx
```

## Main API Areas

| Controller | Purpose |
| --- | --- |
| `AuthController` | Login and registration |
| `BooksController` | Catalog and inventory management |
| `UsersController` | Members, staff users, profiles, restrictions |
| `ReservationsController` | Member and staff reservation workflows |
| `LoansController` | Issue, return, renew, and QR-based issuing |
| `FinesController` | Fine summaries, payments, and Stripe checkout |
| `TransactionsController` | Circulation and payment activity history |

## Default Local Workflow

1. Start the backend with `dotnet run --project API`.
2. Start the frontend with `npm run dev` inside `client`.
3. Register or use the configured default admin account.
4. Add books and members.
5. Use the staff views to issue books, manage reservations, record returns, and handle fines.

