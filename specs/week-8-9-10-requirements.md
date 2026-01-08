# Week 8-9-10 Detailed Requirements

## Week 8 Completion: Draft Publish & User Team Views

### Priority: HIGH - Complete Week 8

---

### 1. Database Changes

#### Sessions Table - New Fields

```sql
ALTER TABLE Sessions ADD DraftEnabled BIT DEFAULT 0;
ALTER TABLE Sessions ADD DraftPublished BIT DEFAULT 0;
```

**Fields**:

- `DraftEnabled` (BIT, default 0): Enables draft feature for session (shows Draft button in UI)
- `DraftPublished` (BIT, default 0): Draft is published and visible to users

---

### 2. Backend API - Draft Publish

#### Endpoint: Publish/Unpublish Draft

- **Route**: `PUT /api/admin/sessions/{sessionId}/publish-draft`
- **Authorization**: `[Authorize(Roles = "Admin")]`
- **Request Body**:
  ```json
  {
    "published": true // true to publish, false to unpublish
  }
  ```
- **Response**: Updated Session object with DraftPublished status
- **Behavior**:
  - When `published = true`: Sets `Session.DraftPublished = 1`
  - When `published = false`: Sets `Session.DraftPublished = 0`
  - Admin can toggle multiple times (for roster adjustments)

#### Endpoint: Get My Team

- **Route**: `GET /api/users/my-team/{sessionId}`
- **Authorization**: `[Authorize]` (authenticated users only)
- **Response**:
  ```json
  {
    "teamId": 5,
    "teamName": "Blue Demons",
    "teamColor": "#0d6efd",
    "captainName": "John Smith",
    "isCaptain": false,
    "teammates": [
      {
        "name": "John Smith",
        "position": "Forward",
        "email": "john@email.com" // Only included if current user is captain
      },
      {
        "name": "Jane Doe",
        "position": "Defense",
        "email": "jane@email.com" // Only included if current user is captain
      }
    ],
    "sessionName": "Winter Platinum Session",
    "sessionDate": "2026-01-15T18:00:00",
    "leagueName": "Adult Rec League",
    "sessionRecord": "5-3-1", // Win-Loss-Tie (null for upcoming sessions)
    "standing": "2nd Place" // (null for upcoming sessions)
  }
  ```
- **Logic**:
  - Find current user's `SessionRegistration` for given `sessionId`
  - Find associated `Player` record via `SessionRegistrationId`
  - Get `Team` via `Player.TeamId`
  - Check if current user is captain: `Team.CaptainUserId == currentUserId`
  - If captain: include teammate emails, else exclude
  - Sort teammates: captain first, then alphabetical
  - Return 404 if draft not published OR user not assigned to team

---

### 3. Frontend - Admin Session Management

#### 3a. Create/Edit Session Modal - Add Draft Toggle

**Location**: `admin-sessions.component.html` (Create/Edit Session Modal)

**New Form Field**:

```html
<div class="mb-3 form-check">
  <input
    type="checkbox"
    class="form-check-input"
    id="draftEnabled"
    formControlName="draftEnabled"
  />
  <label class="form-check-label" for="draftEnabled">
    Enable Draft for this session
  </label>
  <div class="form-text">
    If enabled, admin can draft teams and publish roster to players
  </div>
</div>
```

**Form Model Update**:

```typescript
draftEnabled: [false]; // Add to reactive form
```

**API Update**: Include `draftEnabled` in create/update session requests

---

#### 3b. Sessions Table - Add Draft Status Column

**Location**: `admin-sessions.component.html` (Sessions Table)

**New Column**:

```html
<th>Draft Status</th>
...
<td>
  @if (!session.draftEnabled) {
  <span class="badge bg-secondary">Disabled</span>
  } @else if (!hasTeams(session.id)) {
  <span class="badge bg-secondary">Not Started</span>
  } @else if (!session.draftPublished) {
  <span class="badge bg-warning text-dark">In Progress</span>
  } @else {
  <span class="badge bg-success">Completed</span>
  }
</td>
```

**Logic**:

- **Disabled**: `draftEnabled = false`
- **Not Started**: `draftEnabled = true` AND no teams created
- **In Progress**: `draftEnabled = true` AND teams exist AND `draftPublished = false`
- **Completed**: `draftEnabled = true` AND `draftPublished = true`

**Component Method**:

```typescript
hasTeams(sessionId: number): boolean {
  // Check if session has any teams (via API or cached data)
}
```

---

#### 3c. Sessions Table - Conditional Draft Button

**Current**: Draft button always shows in Actions column

**Updated Logic**:

```html
@if (session.draftEnabled) {
<button
  class="btn btn-sm btn-primary"
  [routerLink]="['/admin/sessions', session.id, 'draft']"
>
  <i class="bi bi-clipboard-check"></i> Draft
</button>
}
```

**Behavior**: Draft button only appears if `session.draftEnabled = true`

---

### 4. Frontend - Draft Page Publish Button

**Location**: `admin-draft.component.html` (Header)

**Add Button Next to "Manage Teams"**:

```html
<div class="d-flex align-items-center gap-2">
  <button class="btn btn-sm btn-outline-primary" (click)="navigateToTeams()">
    <i class="bi bi-people-fill"></i> Manage Teams
  </button>

  <!-- NEW: Publish/Unpublish Button -->
  @if (draftPublished()) {
  <button class="btn btn-sm btn-success" (click)="unpublishDraft()">
    <i class="bi bi-check-circle-fill"></i> Draft Published
  </button>
  } @else {
  <button
    class="btn btn-sm btn-outline-success"
    (click)="publishDraft()"
    [disabled]="teams().length === 0"
  >
    <i class="bi bi-upload"></i> Publish Draft
  </button>
  }
</div>
```

**Component Logic** (`admin-draft.component.ts`):

```typescript
draftPublished = signal<boolean>(false);

publishDraft() {
  this.http.put(`/api/admin/sessions/${this.sessionId()}/publish-draft`, { published: true })
    .subscribe({
      next: () => {
        this.draftPublished.set(true);
        this.toastService.success('Draft published! Players can now see their teams.');
      },
      error: (err) => {
        this.toastService.error('Failed to publish draft');
      }
    });
}

unpublishDraft() {
  if (confirm('Unpublish draft? Players will no longer see team assignments.')) {
    this.http.put(`/api/admin/sessions/${this.sessionId()}/publish-draft`, { published: false })
      .subscribe({
        next: () => {
          this.draftPublished.set(false);
          this.toastService.info('Draft unpublished');
        },
        error: (err) => {
          this.toastService.error('Failed to unpublish draft');
        }
      });
  }
}
```

**Load Draft Status**:

```typescript
loadDraftData() {
  // ... existing code ...

  // Fetch session to get draftPublished status
  this.http.get(`/api/sessions/${this.sessionId()}`).subscribe({
    next: (session: any) => {
      this.draftPublished.set(session.draftPublished);
    }
  });
}
```

---

### 5. Frontend - Player Dashboard

**New Component**: `dashboard.component.ts` (player dashboard, not admin)

**Route**: `/dashboard` (protected by `AuthGuard`)

#### 5a. Current Teams Section

**Display**: Card grid showing upcoming sessions with team assignments

```html
<div class="container mt-4">
  <h2 class="mb-4">My Teams</h2>

  <!-- Current Teams -->
  <h4 class="mb-3">Current Sessions</h4>
  <div class="row">
    @for (session of currentSessions(); track session.sessionId) {
    <div class="col-md-6 col-lg-4 mb-4">
      <div
        class="card h-100 shadow-sm"
        [style.border-top]="'4px solid ' + session.teamColor"
      >
        <div class="card-body">
          <h5 class="card-title">{{ session.sessionName }}</h5>
          <p class="text-muted mb-2">
            <i class="bi bi-calendar-event"></i>
            {{ session.sessionDate | date:'short' }}
          </p>
          <p class="mb-2">
            <i class="bi bi-trophy"></i> {{ session.leagueName }}
          </p>
          <div class="d-flex align-items-center gap-2 mb-3">
            <span class="badge" [style.background-color]="session.teamColor">
              {{ session.teamName }}
            </span>
            @if (session.isCaptain) {
            <span class="badge bg-warning text-dark">
              <i class="bi bi-star-fill"></i> Captain
            </span>
            }
          </div>
          <button
            class="btn btn-sm btn-outline-primary w-100"
            (click)="viewTeam(session.sessionId)"
          >
            <i class="bi bi-people"></i> View Team
          </button>
        </div>
      </div>
    </div>
    }
  </div>

  <!-- Past Teams -->
  <h4 class="mb-3 mt-5">Past Sessions</h4>
  <div class="row">
    @for (session of pastSessions(); track session.sessionId) {
    <div class="col-md-6 col-lg-4 mb-4">
      <div class="card h-100 shadow-sm border-secondary">
        <div class="card-body">
          <h5 class="card-title">{{ session.sessionName }}</h5>
          <p class="text-muted mb-2">
            <i class="bi bi-calendar-event"></i>
            {{ session.sessionDate | date:'short' }}
          </p>
          <div class="d-flex align-items-center gap-2 mb-2">
            <span class="badge bg-secondary"> {{ session.teamName }} </span>
            @if (session.sessionRecord) {
            <span class="badge bg-info">{{ session.sessionRecord }}</span>
            } @if (session.standing) {
            <span class="badge bg-success">{{ session.standing }}</span>
            }
          </div>
          <button
            class="btn btn-sm btn-outline-secondary w-100"
            (click)="viewTeam(session.sessionId)"
          >
            <i class="bi bi-people"></i> View Team
          </button>
        </div>
      </div>
    </div>
    }
  </div>
</div>
```

**Component Logic**:

```typescript
currentSessions = signal<any[]>([]);
pastSessions = signal<any[]>([]);
selectedTeam = signal<any | null>(null);
showTeamModal = signal<boolean>(false);

ngOnInit() {
  this.loadMyTeams();
}

loadMyTeams() {
  this.http.get<any[]>('/api/users/my-teams').subscribe({
    next: (teams) => {
      const now = new Date();
      this.currentSessions.set(teams.filter(t => new Date(t.sessionDate) >= now));
      this.pastSessions.set(teams.filter(t => new Date(t.sessionDate) < now));
    }
  });
}

viewTeam(sessionId: number) {
  this.http.get(`/api/users/my-team/${sessionId}`).subscribe({
    next: (team) => {
      this.selectedTeam.set(team);
      this.showTeamModal.set(true);
    }
  });
}
```

#### 5b. Team Detail Modal

```html
<!-- Team Modal -->
@if (showTeamModal()) {
<div class="modal show d-block" tabindex="-1">
  <div class="modal-dialog modal-lg">
    <div class="modal-content">
      <div
        class="modal-header"
        [style.background-color]="selectedTeam()?.teamColor"
      >
        <h5 class="modal-title text-white">
          <i class="bi bi-shield-fill"></i> {{ selectedTeam()?.teamName }}
        </h5>
        <button
          type="button"
          class="btn-close btn-close-white"
          (click)="showTeamModal.set(false)"
        ></button>
      </div>
      <div class="modal-body">
        <div class="mb-3">
          <strong>Session:</strong> {{ selectedTeam()?.sessionName }}<br />
          <strong>League:</strong> {{ selectedTeam()?.leagueName }}<br />
          <strong>Date:</strong> {{ selectedTeam()?.sessionDate | date:'medium'
          }}<br />
          @if (selectedTeam()?.sessionRecord) {
          <strong>Record:</strong> {{ selectedTeam()?.sessionRecord }} @if
          (selectedTeam()?.standing) { - {{ selectedTeam()?.standing }} } }
        </div>

        <h6 class="mb-3">
          <i class="bi bi-people-fill"></i> Roster ({{
          selectedTeam()?.teammates.length }} players)
        </h6>

        <div class="table-responsive">
          <table class="table table-sm table-hover">
            <thead class="table-light">
              <tr>
                <th>Player</th>
                <th>Position</th>
                @if (selectedTeam()?.isCaptain) {
                <th>Email</th>
                }
              </tr>
            </thead>
            <tbody>
              @for (player of selectedTeam()?.teammates; track player.name) {
              <tr>
                <td>
                  {{ player.name }} @if (player.name ===
                  selectedTeam()?.captainName) {
                  <i class="bi bi-star-fill text-warning ms-2"></i>
                  }
                </td>
                <td>
                  <span class="badge" [ngClass]="'pos-' + player.position">
                    {{ player.position }}
                  </span>
                </td>
                @if (selectedTeam()?.isCaptain) {
                <td>{{ player.email }}</td>
                }
              </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
      <div class="modal-footer">
        <button class="btn btn-secondary" (click)="showTeamModal.set(false)">
          Close
        </button>
      </div>
    </div>
  </div>
</div>
<div class="modal-backdrop show"></div>
}
```

---

### 6. Backend API - Get All User Teams

**Additional Endpoint**: Get all teams for current user (for dashboard)

- **Route**: `GET /api/users/my-teams`
- **Authorization**: `[Authorize]`
- **Response**: Array of team summary objects
  ```json
  [
    {
      "sessionId": 10,
      "sessionName": "Winter Platinum",
      "sessionDate": "2026-01-15T18:00:00",
      "leagueName": "Adult Rec League",
      "teamId": 5,
      "teamName": "Blue Demons",
      "teamColor": "#0d6efd",
      "isCaptain": false,
      "sessionRecord": null, // null for upcoming
      "standing": null
    },
    {
      "sessionId": 8,
      "sessionName": "Fall Gold",
      "sessionDate": "2025-11-20T19:00:00",
      "leagueName": "Adult Rec League",
      "teamId": 3,
      "teamName": "Red Wings",
      "teamColor": "#dc3545",
      "isCaptain": true,
      "sessionRecord": "7-2-1",
      "standing": "1st Place"
    }
  ]
  ```
- **Logic**: Query all sessions where user has `SessionRegistration` → `Player` → `Team`, and `Session.DraftPublished = true`

---

## Week 9: League Standings & Statistics

### Priority: MEDIUM-HIGH

---

### 1. Database Changes - Game Tracking

#### New Table: Games

```sql
CREATE TABLE Games (
  Id INT PRIMARY KEY IDENTITY,
  SessionId INT NOT NULL FOREIGN KEY REFERENCES Sessions(Id),
  GameDate DATETIME2 NOT NULL,
  Rink NVARCHAR(100) NULL,
  HomeTeamId INT NOT NULL FOREIGN KEY REFERENCES Teams(Id),
  AwayTeamId INT NOT NULL FOREIGN KEY REFERENCES Teams(Id),
  HomeScore INT NULL,
  AwayScore INT NULL,
  Status NVARCHAR(20) DEFAULT 'Scheduled',  -- Scheduled, Completed, Cancelled
  CreatedAt DATETIME2 DEFAULT GETDATE(),
  UpdatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### New Table: GameStats (Player Stats)

```sql
CREATE TABLE GameStats (
  Id INT PRIMARY KEY IDENTITY,
  GameId INT NOT NULL FOREIGN KEY REFERENCES Games(Id),
  PlayerId INT NOT NULL FOREIGN KEY REFERENCES Players(Id),
  Goals INT DEFAULT 0,
  Assists INT DEFAULT 0,
  PenaltyMinutes INT DEFAULT 0,
  CreatedAt DATETIME2 DEFAULT GETDATE()
);
```

#### New Table: GoalieStats

```sql
CREATE TABLE GoalieStats (
  Id INT PRIMARY KEY IDENTITY,
  GameId INT NOT NULL FOREIGN KEY REFERENCES Games(Id),
  PlayerId INT NOT NULL FOREIGN KEY REFERENCES Players(Id),  -- Must be goalie
  ShotsAgainst INT DEFAULT 0,
  Saves INT DEFAULT 0,
  GoalsAgainst INT DEFAULT 0,
  Win BIT DEFAULT 0,
  Loss BIT DEFAULT 0,
  Tie BIT DEFAULT 0,
  CreatedAt DATETIME2 DEFAULT GETDATE()
);
```

---

### 2. Backend API - League Standings

#### Endpoint: Get League Standings (for specific session)

- **Route**: `GET /api/leagues/{leagueId}/sessions/{sessionId}/standings`
- **Authorization**: `[Authorize]` (registered users in that league only)
- **Response**:
  ```json
  {
    "sessionName": "Winter Platinum",
    "leagueName": "Adult Rec League",
    "teams": [
      {
        "teamId": 5,
        "teamName": "Blue Demons",
        "teamColor": "#0d6efd",
        "wins": 7,
        "losses": 2,
        "ties": 1,
        "points": 15, // 2pts per win, 1pt per tie
        "goalsFor": 42,
        "goalsAgainst": 28,
        "goalDifferential": 14,
        "standing": 1
      }
    ]
  }
  ```

#### Endpoint: Get Top Players (for specific session)

- **Route**: `GET /api/leagues/{leagueId}/sessions/{sessionId}/top-players`
- **Authorization**: `[Authorize]`
- **Query Params**: `?limit=20` (default 20)
- **Response**:
  ```json
  [
    {
      "playerId": 45,
      "playerName": "John Smith",
      "teamName": "Blue Demons",
      "goals": 12,
      "assists": 15,
      "points": 27,
      "gamesPlayed": 10
    }
  ]
  ```

#### Endpoint: Get Top Goalies (for specific session)

- **Route**: `GET /api/leagues/{leagueId}/sessions/{sessionId}/top-goalies`
- **Authorization**: `[Authorize]`
- **Response**:
  ```json
  [
    {
      "playerId": 23,
      "playerName": "Mike Johnson",
      "teamName": "Red Wings",
      "wins": 8,
      "losses": 2,
      "ties": 0,
      "gaa": 2.45, // Goals Against Average
      "savePercentage": 0.915,
      "gamesPlayed": 10
    }
  ]
  ```

#### Endpoint: Get Upcoming Games (7-day window)

- **Route**: `GET /api/leagues/{leagueId}/sessions/{sessionId}/upcoming-games`
- **Authorization**: `[Authorize]`
- **Response**:
  ```json
  [
    {
      "gameId": 101,
      "gameDate": "2026-01-10T19:00:00",
      "rink": "Ice Arena North - Rink 2",
      "homeTeam": {
        "teamId": 5,
        "teamName": "Blue Demons",
        "teamColor": "#0d6efd"
      },
      "awayTeam": {
        "teamId": 7,
        "teamName": "Green Machine",
        "teamColor": "#198754"
      }
    }
  ]
  ```

---

### 3. Frontend - League Standings Page

**New Component**: `league-standings.component.ts`

**Route**: `/leagues/{leagueId}/sessions/{sessionId}/standings`

**Layout**:

```html
<div class="container mt-4">
  <h2>{{ leagueName() }} - {{ sessionName() }}</h2>

  <!-- Team Standings Card -->
  <div class="card shadow-sm mb-4">
    <div class="card-header bg-primary text-white">
      <h5 class="mb-0"><i class="bi bi-trophy"></i> Team Standings</h5>
    </div>
    <div class="card-body">
      <div class="table-responsive">
        <table class="table table-hover">
          <thead class="table-light">
            <tr>
              <th>#</th>
              <th>Team</th>
              <th>W</th>
              <th>L</th>
              <th>T</th>
              <th>PTS</th>
              <th>GF</th>
              <th>GA</th>
              <th>+/-</th>
            </tr>
          </thead>
          <tbody>
            @for (team of standings(); track team.teamId) {
            <tr>
              <td>{{ team.standing }}</td>
              <td>
                <span class="badge" [style.background-color]="team.teamColor">
                  {{ team.teamName }}
                </span>
              </td>
              <td>{{ team.wins }}</td>
              <td>{{ team.losses }}</td>
              <td>{{ team.ties }}</td>
              <td><strong>{{ team.points }}</strong></td>
              <td>{{ team.goalsFor }}</td>
              <td>{{ team.goalsAgainst }}</td>
              <td
                [class.text-success]="team.goalDifferential > 0"
                [class.text-danger]="team.goalDifferential < 0"
              >
                {{ team.goalDifferential > 0 ? '+' : '' }}{{
                team.goalDifferential }}
              </td>
            </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- Top 20 Players Card -->
  <div class="card shadow-sm mb-4">
    <div class="card-header bg-success text-white">
      <h5 class="mb-0"><i class="bi bi-star-fill"></i> Top Scorers</h5>
    </div>
    <div class="card-body">
      <div class="table-responsive">
        <table class="table table-sm">
          <thead class="table-light">
            <tr>
              <th>Rank</th>
              <th>Player</th>
              <th>Team</th>
              <th>GP</th>
              <th>G</th>
              <th>A</th>
              <th>PTS</th>
            </tr>
          </thead>
          <tbody>
            @for (player of topPlayers(); track player.playerId; let idx =
            $index) {
            <tr>
              <td>{{ idx + 1 }}</td>
              <td>{{ player.playerName }}</td>
              <td>
                <small class="text-muted">{{ player.teamName }}</small>
              </td>
              <td>{{ player.gamesPlayed }}</td>
              <td>{{ player.goals }}</td>
              <td>{{ player.assists }}</td>
              <td><strong>{{ player.points }}</strong></td>
            </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- Goalie Stats Card -->
  <div class="card shadow-sm mb-4">
    <div class="card-header bg-info text-white">
      <h5 class="mb-0">
        <i class="bi bi-shield-fill-check"></i> Goalie Leaders
      </h5>
    </div>
    <div class="card-body">
      <div class="table-responsive">
        <table class="table table-sm">
          <thead class="table-light">
            <tr>
              <th>Rank</th>
              <th>Goalie</th>
              <th>Team</th>
              <th>GP</th>
              <th>W-L-T</th>
              <th>GAA</th>
              <th>SV%</th>
            </tr>
          </thead>
          <tbody>
            @for (goalie of topGoalies(); track goalie.playerId; let idx =
            $index) {
            <tr>
              <td>{{ idx + 1 }}</td>
              <td>{{ goalie.playerName }}</td>
              <td>
                <small class="text-muted">{{ goalie.teamName }}</small>
              </td>
              <td>{{ goalie.gamesPlayed }}</td>
              <td>{{ goalie.wins }}-{{ goalie.losses }}-{{ goalie.ties }}</td>
              <td>{{ goalie.gaa.toFixed(2) }}</td>
              <td>{{ (goalie.savePercentage * 100).toFixed(1) }}%</td>
            </tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <!-- Upcoming Games Card -->
  <div class="card shadow-sm mb-4">
    <div class="card-header bg-warning text-dark">
      <h5 class="mb-0">
        <i class="bi bi-calendar-event"></i> Upcoming Games (Next 7 Days)
      </h5>
    </div>
    <div class="card-body">
      @if (upcomingGames().length === 0) {
      <p class="text-muted mb-0">No games scheduled in the next 7 days.</p>
      } @else {
      <div class="list-group">
        @for (game of upcomingGames(); track game.gameId) {
        <div class="list-group-item">
          <div class="d-flex justify-content-between align-items-center">
            <div>
              <strong>{{ game.gameDate | date:'EEE, MMM d @ h:mm a' }}</strong
              ><br />
              <small class="text-muted">
                <i class="bi bi-geo-alt"></i> {{ game.rink }}
              </small>
            </div>
            <div class="text-end">
              <div>
                <span
                  class="badge"
                  [style.background-color]="game.homeTeam.teamColor"
                >
                  {{ game.homeTeam.teamName }}
                </span>
                vs
                <span
                  class="badge"
                  [style.background-color]="game.awayTeam.teamColor"
                >
                  {{ game.awayTeam.teamName }}
                </span>
              </div>
            </div>
          </div>
        </div>
        }
      </div>
      }
    </div>
  </div>
</div>
```

---

### 4. Admin - Game Management

**New Component**: `admin-games.component.ts`

**Route**: `/admin/sessions/{sessionId}/games`

**Features**:

- Create new games (select date, time, rink, home/away teams)
- Edit game details
- Enter game scores (mark as completed)
- Enter player stats (goals, assists) and goalie stats
- Cancel games
- View game history

---

## Week 10: Additional Enhancements

### 1. User Profile Enhancements

- Phone number field (editable)
- Emergency contact
- Jersey number preference
- Profile photo upload

### 2. Admin User Detail Page

- Click user row in User Management → view detailed profile
- Admin notes field (NVARCHAR(MAX))
- User statistics (registrations, payments, attendance)
- Registration history table
- Payment history table

### 3. Team Color Management

- Admin can customize team colors with color picker
- Predefined color palette
- Custom hex code input

### 4. Footer & Home Page

- Footer with contact info, social links
- Home page redesign with testimonials
- Feature highlights

---

## Implementation Priority

### Week 8 Completion (Immediate):

1. Database: DraftEnabled, DraftPublished fields
2. Backend: Publish draft + Get my team endpoints
3. Frontend: Admin publish button + draft status column
4. Frontend: Player dashboard with team views

### Week 9 (Next Priority):

1. Database: Games, GameStats, GoalieStats tables
2. Backend: Standings, top players, goalie stats endpoints
3. Frontend: League standings page
4. Frontend: Admin game management

### Week 10 (Future):

1. User profile enhancements
2. Admin user detail page
3. Team color management
4. Footer & home page updates
