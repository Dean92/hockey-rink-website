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

- User registration and login
- League and session browsing
- Session registration and payment simulation
- Team drafts and admin dashboard
- User profile management
- Notifications and error handling

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

### ⏳ Timeline: Aug 22 – Oct 20, 2025 (~1–2 hrs/day)

---

### ✅ Week 1: Backend Setup

- Initialized `HockeyRinkAPI` and `HockeyRinkWeb`
- Models: `Player`, `League`, `Session`, `Payment`, etc.
- API endpoints: `/api/auth/*`, `/api/users/profile`, `/api/sessions`, `/api/leagues`
- xUnit tests implemented and passing
- Azure SQL (Free Tier) configured and seeded

---

### ✅ Week 2: Frontend Setup

- Angular 20 project with routing and Bootstrap 5.3.3
- Components: `Login`, `Register`, `Leagues`, `Sessions`, `Profile`
- Deployed to Azure Static Web Apps

---

### ✅ Week 3: Enhancements

- Admin dashboard and session filtering
- Hockey-themed branding and UI polish
- Frontend unit tests and error handling

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

---

## 📎 Repository

GitHub: [Dean92/hockey-rink-website](https://github.com/Dean92/hockey-rink-website)
