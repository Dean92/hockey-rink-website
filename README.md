# Hockey Rink Website

A web application for managing a local hockey league.

- **Backend**: .NET Core 9 API with Entity Framework Core and Azure SQL.
- **Frontend**: Angular 20 with Bootstrap 5 (CSS-based styling).
- **Features**: User registration, session management, team drafts, admin dashboard.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (for Angular)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)
- SQL Server LocalDB or Azure SQL Database

### Running the API (HockeyRinkAPI)

#### Trust the HTTPS Development Certificate (First Time Only)

```powershell
dotnet dev-certs https --trust
```

#### Option 1: Using Command Line (Recommended when Visual Studio is open)

```powershell
cd HockeyRinkAPI
dotnet build
dotnet run --no-build --launch-profile https
```

#### Option 2: Using Command Line (When Visual Studio is closed)

```powershell
cd HockeyRinkAPI
dotnet run --launch-profile https
```

#### Option 3: Using Visual Studio 2022

Open `HockeyRinkWebsite.sln` and press F5 or click the "https" button in the toolbar.

#### Verify the API is Running

- **Swagger UI (HTTPS)**: https://localhost:7134
- **Swagger UI (HTTP)**: http://localhost:5253
- **Health Check**: http://localhost:5253/health

**Note**: If `dotnet run` hangs at "Building...", this is due to MSBuild lock conflicts when Visual Studio is open. Use Option 1 above (build and run separately) or close Visual Studio.

### Running the Frontend (HockeyRinkWeb)

```powershell
cd HockeyRinkWeb
npm install
ng serve -o
```

The Angular app will open at `http://localhost:4200`

### Troubleshooting

#### API Won't Start - Port Already in Use

If you see "Failed to bind to address https://127.0.0.1:7134: address already in use":

1. Find the process using the port:

   ```powershell
   netstat -ano | findstr :7134
   ```

2. Kill the process (replace PID with actual process ID):
   ```powershell
   taskkill /PID <PID> /F
   ```

#### dotnet run Hangs at "Building..."

This happens when Visual Studio 2022 has the solution open. Use one of these solutions:

- Run `dotnet build` followed by `dotnet run --no-build`
- Close Visual Studio before using `dotnet run`
- Use Visual Studio's built-in run functionality instead

[Add any unique remote content here]
