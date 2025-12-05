# 🏒 Hockey Rink Website

A full-stack web application for managing local hockey leagues, player sessions, and team registrations.

---

## 🚀 Tech Stack

| Layer        | Technology                                        |
| ------------ | ------------------------------------------------- |
| **Backend**  | .NET Core 9 API, Entity Framework Core, Azure SQL |
| **Frontend** | Angular 20, Bootstrap 5.3.3 (CSS variables)       |
| **DevOps**   | GitHub Actions (CI/CD), Azure Static Web Apps     |

---

## 🎯 Core Features

### User Features

- User registration and authentication with cookie-based sessions
- League browsing with dynamic pricing (early bird/regular rates)
- Session browsing with filtering by league and date
- Session registration with comprehensive form (name, address, phone, DOB, position)
- User profile management
- Real-time session capacity tracking

### Admin Features

- Admin dashboard with comprehensive analytics
- League management (CRUD operations, pricing tiers, registration windows)
- Session management (CRUD operations, capacity settings, registration periods)
- Session registration management (view registrants, manual registration)
- User management and role administration

### Technical Features

- Responsive hockey-themed UI with ice blue color palette
- Toast notifications for user feedback
- Form validation with reactive forms
- Environment-based database configuration (InMemory for tests, SQL Server for production)
- Comprehensive test suite with xUnit (12 passing integration tests)

---

## 🧰 Getting Started

### 🔧 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for Angular)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- SQL Server LocalDB or Azure SQL Database

---

### ▶️ Running the Backend (`HockeyRinkAPI`)

#### Step 1: Trust HTTPS Certificate (First Time Only)

```powershell
dotnet dev-certs https --trust
```

#### Step 2: Start the API

**Option A** – Visual Studio Open:

```powershell
cd HockeyRinkAPI
dotnet build
dotnet run --no-build --launch-profile https
```

**Option B** – Visual Studio Closed:

```powershell
cd HockeyRinkAPI
dotnet run --launch-profile https
```

**Option C** – Visual Studio GUI:
Open `HockeyRinkWebsite.sln`, press F5 or click "https" in the toolbar.

#### Step 3: Verify API

- Swagger UI (HTTPS): `https://localhost:7134`
- Swagger UI (HTTP): `http://localhost:5253`
- Health Check: `http://localhost:5253/health`

---

### ▶️ Running the Frontend (`HockeyRinkWeb`)

```powershell
cd HockeyRinkWeb
npm install
ng serve -o
```

App launches at: `http://localhost:4200`

---

## 🧪 Troubleshooting

### Port Conflict (API)

```powershell
netstat -ano | findstr :7134
taskkill /PID <PID> /F
```

### `dotnet run` Hangs

- Run `dotnet build` then `dotnet run --no-build`
- Close Visual Studio before running
- Use Visual Studio’s built-in run instead

---

## 📌 MVP Development Plan

### ⏳ Timeline: Aug 22 – Dec 20, 2025 (~1–2 hrs/day)

---

### ✅ Week 1: Backend Setup (Completed)

- Initialized `HockeyRinkAPI` and `HockeyRinkWeb` projects
- Models: `Player`, `League`, `Session`, `Payment`, `SessionRegistration`, `ApplicationUser`
- API endpoints: `/api/auth/*`, `/api/users/profile`, `/api/sessions`, `/api/leagues`
- xUnit tests implemented and passing
- Azure SQL (Free Tier) configured with migrations and seeding

---

### ✅ Week 2: Frontend Setup (Completed)

- Angular 20 project with routing and Bootstrap 5.3.3
- Components: `Login`, `Register`, `Leagues`, `Sessions`, `Profile`
- Authentication service with cookie-based sessions
- Deployed to Azure Static Web Apps

---

### ✅ Week 3: Admin Features & Enhancements (Completed - November 11, 2025)

- Admin dashboard with statistics and user management
- Admin sessions management (full CRUD with reactive forms)
- Session filtering by league and date
- AuthGuard and AdminGuard implementation
- TypeScript interfaces for type safety
- Angular 20 control flow (`@if`, `@for`)
- Database migrations with foreign key constraints
- Accessibility improvements (ARIA labels)
- Conditional navigation based on authentication

---

### 🚀 Week 4: Final Integration & UI Polish

#### 🔐 Navigation Bar

- Angular 20 `@if` blocks for conditional rendering
- Logged-in users: show `Leagues`, `Sessions`, `Profile`, `Logout`
- Guests: show only `Login`
- Use `AuthService` with `signal<boolean>` or `BehaviorSubject<boolean>`

#### 🏒 Hero Section (Landing Page)

- Full-width layout with hockey-themed image
- Headline: “Hit the Ice. Join a League. Track Your Sessions.”
- Subtext: “Manage your hockey schedule, register for sessions, and connect with your team—all in one place.”
- CTA buttons: `/register`, `/leagues`
- Responsive Bootstrap grid

#### 🎨 UI Polish

- Custom Bootstrap variables: ice blue, puck black, goal red
- Hover effects, transitions, SVG icons
- Responsive layout for mobile/desktop
- Optional dark mode toggle via CSS variables

#### 🧪 Optional Add-ons

- Toast notifications for login/logout/session registration
- 404 page with hockey-themed illustration
- Final deployment verification

**Status**: ✅ Completed - November 2025

---

### ✅ Week 5: Session Registration Enhancements (Completed - November 19, 2025)

#### Admin League Management

- Full CRUD interface for league settings
- Registration windows (open/close dates)
- Pricing tiers (early bird + regular rates)
- League start dates and capacity management

#### Session Enhancements

- Max players setting (1-250, default 20)
- Registration windows with auto-activation
- Pricing tiers (early bird/regular)
- Auto-deactivation for closed/past sessions
- Enhanced session cards with gradient styling

#### Bug Fixes

- Fixed datetime handling across forms
- Fixed league name display in admin tables
- Fixed registration status logic
- Improved success messages

---

### ✅ Week 6: Testing Infrastructure (Completed - December 5, 2025)

#### Comprehensive Test Suite

- Fixed database provider conflict (InMemory for tests, SQL Server for production)
- Environment-based configuration in `Program.cs`
- 12 passing integration tests covering admin, session, and auth workflows
- Custom test fixtures with `CustomWebApplicationFactory`
- Test isolation with in-memory database per test

#### Testing Strategy

- Conditional database provider: `builder.Environment.IsEnvironment("Testing")`
- Simplified test infrastructure
- Fixed cookie authentication redirect handling
- Active session creation in tests

---

### 🚀 Week 7: Payment & Profile Features (Planned - December 2025)

#### Mock Payment Integration

- Credit card form with validation
- Mock payment service (test cards for success/failure)
- Integration with session registration

#### Manual Registration Enhancement

- Password setup workflow for manually registered users
- Email-based password tokens
- Setup password page at `/setup-password/:token`

#### User Profile Enhancements

- Editable personal information (address, phone, DOB, position)
- Preferred league association
- Change password functionality

#### User Dashboard with Session History

- Upcoming sessions view with countdown
- Past sessions history table
- Session cancellation feature

---

## 📎 Repository

GitHub: [Dean92/hockey-rink-website](https://github.com/Dean92/hockey-rink-website)
