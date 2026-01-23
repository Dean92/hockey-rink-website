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

#### Step 1: Navigate to API Directory

```powershell
cd HockeyRinkAPI
```

#### Step 2: Start the API

**Option A** – HTTP (Recommended for Development):

```powershell
dotnet build
dotnet run --no-build --launch-profile http
```

**Option B** – HTTPS (Requires Certificate Trust):

First-time HTTPS setup:

```powershell
dotnet dev-certs https --trust
```

Then run:

```powershell
dotnet run --launch-profile https
```

**Option C** – Visual Studio GUI:
Open `HockeyRinkWebsite.sln`, press F5 or select "http" or "https" in the toolbar.

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

### ✅ Week 7-8: Draft System & Team Management (Completed - January 15, 2026)

#### Draft System

- Drag-and-drop player assignment with Angular CDK
- Team cards with position badges and captain display
- Draft publish/unpublish functionality
- Player dashboard with team assignments
- Expandable team roster views

#### Testing Infrastructure

- 12 passing integration tests with Bearer token authentication
- CustomWebApplicationFactory for test isolation
- Fixed authentication bug in test helper

---

### ✅ Week 9: Player Rating & Admin User Management (Completed - January 22, 2026)

#### Player Rating System

- Admin can rate players 1-5 with decimal increments
- Player notes field for admin observations
- Draft integration showing player ratings
- Team average rating calculation with visual balance indicators

#### Admin User Management Enhancements

- Full profile editing (name, email, league assignment)
- Email duplicate checking with automatic username updates
- Last login tracking and display
- Clickable user rows navigate to profile page
- Phone number formatting and mailto links

#### User Management UX Improvements

- **Search functionality**: Filter users by first or last name (case-insensitive)
- **Pagination**: Display 25 users per page with Previous/Next navigation
- **Smart search integration**: Automatically resets to page 1 when search term changes
- **Page indicator**: Shows "Showing X to Y of Z users"
- **Streamlined UI**: Removed redundant navigation buttons

#### Registrations Modal Enhancements

- **Pagination**: Display 25 registrations per page
- **Search by name**: Filter registrations by first or last name
- **Search integration**: Auto-resets to page 1 on search term change
- **Consistent UX**: Matches user management pagination patterns

#### Draft Page Improvements

- **Player notes display**: Toggle button (plus/minus icon) inline with player name
- **Notes visibility**: Click to expand/collapse notes below player name
- **Consistent across views**: Works in both available pool and team rosters
- **Draft API enhancement**: GetDraftPlayers includes PlayerNotes field
- **Position sorting**: Goalies appear first in draft pool
- **Team average rating**: Toggle display with color-coded balance indicators
- **Position badges**: Special handling for "Forward/Defense" as "B" (Both)

#### Database Enhancements

- AddRatingAndNotesToApplicationUser migration
- AddLastLoginAt migration
- Games table foundation (for future statistics in Week 11)

---

### 🚀 Week 10: Emergency Contact & Jersey Number Management (Planned)

#### Emergency Contact

- Emergency contact name and phone required during registration
- Editable by user on profile page
- Stored in ApplicationUser table

#### Hockey Registration Number

- Optional USA Hockey or AAU Hockey registration number field
- User can specify registration type and number on profile page
- Numbers updated annually
- Future: Admin view to track all registration numbers and expirations

#### Jersey Number Management

- Admin assigns jersey numbers per session (0-99, unique per team)
- New admin dashboard quick link: "Manage Jersey Numbers"
- Jersey Management page:
  - View all sessions (filterable by active/inactive)
  - Clickable session cards
  - Jersey assignment table for all players in session
  - Individual or batch save options
- User Views:
  - Jersey number displayed on user dashboard
  - Jersey numbers shown in "My Teams" roster view
  - New "Jersey #" column in team roster tables

#### Admin User Detail Page

- Click user row to view detailed profile
- User statistics (registrations, payments, attendance)
- Registration and payment history tables

#### Additional Enhancements

- Team color management with color picker
- Footer with contact info and social links
- Home page testimonials and feature highlights

---

### 🚀 Week 11: League Standings & Statistics (Planned)

#### Database Tables

- GameStats table (player goals, assists, penalty minutes per game)
- GoalieStats table (saves, goals against, W-L-T per game)

#### Backend API

- League standings calculation endpoints
- Top players/goalies statistics
- Game score entry and validation

#### Frontend Features

- League standings page with team rankings
- Admin game management interface
- Player/goalie statistics displays

---

### 🚀 Week 7 (Original Plan): Payment & Profile Features

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

### ✅ Week 8: Team Draft System & Admin UI Redesign (Completed - December 2024)

#### Team Draft System

- **Player Rating System**: Session-specific ratings (0.0-10.0 scale) on SessionRegistrations table
- **Team Management**: Full CRUD with captain assignment, max players (1-50), player count tracking
- **Draft API Endpoints**: GetDraftPlayers, AssignPlayerToTeam, RemovePlayerFromTeam, GetTeamPlayers, SetPlayerRating
- **Draft Page UI**: Professional dashboard with Angular CDK drag-and-drop
  - Sidebar draft pool with goalie-first sorting (position priority)
  - Grid layout for team cards (responsive, 800px max height)
  - Bidirectional drag-drop: pool↔teams, team↔team
  - Position badges with color coding (F=blue, D=orange, G=green)
  - Remove buttons on hover as alternative to drag-to-pool
  - Inline captain display with star icon in team headers

#### Team Captain Feature

- Captain dropdown populated from session registrations
- Captain auto-clears when removed from team
- Inline badge display: "⭐ [Captain Name]"
- Fixed data loading bug in getSessionRegistrations

#### Admin UI Redesign

- **Admin Sessions Page**: Modern card-based layout with toolbar, filter pills (gap-2 spacing), table cards
- **Modal Polish**: Icon headers, summary cards, table cards for registrations
- **Professional Styling**: Bootstrap 5 cards with shadows, rounded corners, responsive design

#### Session Visibility Logic Update

- Sessions remain visible until 7 days after session start (not when registration closes)
- "Starting Soon!" notification when registration closed but session hasn't started
- Backend auto-deactivation updated to 7-day window
- Frontend filtering with isSessionStartingSoon() method

#### Database Changes

- Rating field: DECIMAL(3,1) on SessionRegistrations
- Player-SessionRegistration link: SessionRegistrationId FK with unique index
- MaxPlayers field: INT on Teams with validation (1-50 range)
- Simplified model: Players link directly to Teams (no junction table)

#### Technical Implementation

- Angular CDK @angular/cdk@^20.0.0 for drag-and-drop
- Signal-based reactivity for state management
- Optimistic UI updates with API error rollback
- Goalie-first sorting: Position 'G' or 'GOALIE' (case-insensitive)
- Toast notifications for user feedback

**Status**: ✅ Completed with expanded scope (professional UI/UX, session visibility enhancements)

**Not Implemented**: Auto-draft algorithm, user-facing team views (deferred to future sprint)

---

### ✅ Week 9: Player Rating System & Admin Profile Management (In Progress - January 2026)

#### Player Rating & Notes System ✅ COMPLETED

- **Database Schema**: Added Rating (decimal 3,1) and PlayerNotes (nvarchar) to ApplicationUser
- **Admin Endpoints**: PUT /api/admin/users/{userId}/profile for complete profile management
- **Rating Scale**: 1.0 to 5.0 with 0.5 increments
- **Draft Integration**: Team average rating calculation uses ApplicationUser.Rating
- **Visual Balance Indicators**: Green/Yellow/Red borders on draft teams based on rating distribution

#### Admin User Management Enhancements ✅ COMPLETED

- **Full Profile Editing**: Admins can edit firstName, lastName, email with duplicate checking
- **League Assignment**: Dropdown to assign users to leagues
- **Email Change Validation**: Prevents duplicate emails, updates username automatically
- **Last Login Tracking**: LastLoginAt field displays when users last logged in
- **Clickable User Rows**: Navigate to profile page with userId query parameter
- **User Management Table**: Added "Last Login" column showing formatted timestamp or "Never"

#### Profile Page Enhancements ✅ COMPLETED

- **Admin-Only Fields**: Rating, notes, email, name editing visible only when admin views another user
- **View Mode**: Displays rating with star icon, notes, clickable mailto email link
- **Edit Mode**: Editable fields for name, email, phone, position, rating, notes, league
- **Phone Formatting**: Auto-formats to (XXX) XXX-XXXX on save and in view mode
- **Navigation**: "Back to User Management" button when admin views user profile
- **Validation**: Email format, duplicate checking, rating range (1-5)

#### Draft Page Improvements ✅ COMPLETED

- **Position Badge Logic**: "Forward/Defense" displays as 'B' (for "Both")
- **Team Average Rating Toggle**: Show/hide team averages on draft page
- **Rating Calculation**: Excludes goalies, displays 1 decimal place
- **Balance Indicators**: Visual borders (green/yellow/red) based on session average
- **Player Notes Display**: Toggle button inline with player name to show/hide notes (January 21, 2026)
- **Draft API Enhancement**: GetDraftPlayers includes PlayerNotes field (January 21, 2026)

#### User Management UX Improvements ✅ COMPLETED (January 21, 2026)

- **Search Functionality**: Filter users by first or last name (case-insensitive)
- **Pagination**: Display 25 users per page with Previous/Next navigation
- **Smart Search Integration**: Automatically resets to page 1 when search term changes
- **Page Indicator**: Shows "Showing X to Y of Z users" above pagination controls
- **Streamlined UI**: Removed redundant "Back to Admin Dashboard" button

#### Technical Implementation

- **Migrations Applied**:
  - AddRatingAndNotesToApplicationUser (January 20, 2026)
  - AddLastLoginAt (January 20, 2026)
- **Authentication**: Login endpoint updates LastLoginAt timestamp
- **Security**: Email changes use ASP.NET Identity's ChangeEmailAsync with token validation
- **UX**: Toast notifications, inline validation, conditional field visibility

**Status**: ✅ Major features completed - Player rating system, admin profile management, last login tracking

**In Progress**: GameStats and GoalieStats database migrations

---

### 📋 Week 10: Statistics & League Standings (Planned)

#### League Standings Page

- Win/Loss/Tie records by team
- Points calculation and ranking
- Season statistics display

#### Game Statistics Entry

- Admin game entry form
- Player statistics tracking (goals, assists, +/-)
- Goalie statistics (saves, goals against)

---

## 📎 Repository

GitHub: [Dean92/hockey-rink-website](https://github.com/Dean92/hockey-rink-website)
