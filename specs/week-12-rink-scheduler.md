# Rink Scheduler — Detailed Specification

## Status: 📋 Planned

## Clarified Decisions

| Topic | Decision |
|-------|----------|
| **Number of rinks** | 2 — seeded as "Rink 1" and "Rink 2"; admin can rename via UI |
| **Operating hours** | 5:00 AM – 12:00 AM (midnight) every day; configurable per rink in the future |
| **Games per matchup** | Admin configurable (1, 2, or 3×); scheduler calculates total games from teams × matchups |
| **Default game length** | 90 minutes; admin can override per schedule generation |
| **Buffer between games** | 10 minutes automatically added after each game on the same rink |
| **Timeline increments** | 1-hour increments (5am, 6am, 7am … 11pm, midnight) |
| **Click-to-create** | Quick form (title, type, end time, notes) — generic enough for any booking type |
| **Blockouts** | Manually added one at a time, no recurring/repeating rules |
| **Month view indicators** | Count badge showing number of bookings on each day |

---

A dedicated **Rink Calendar** page in the admin panel that shows ice time availability per rink, per day. Admins can view existing bookings, schedule league games with automatic conflict detection, and block out rink time for maintenance or holidays.

---

## Goals

- Visual calendar showing all ice time bookings by rink
- Prevent double-booking across sessions and league games
- Auto-generate league game schedules (round robin) while respecting conflicts
- Support multiple rinks
- Admin can block out rink time (maintenance, holidays)

---

## Schema Changes Required

### 1. New: `Rinks` Table

```csharp
public class Rink
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;       // e.g., "Rink 1", "East Rink"
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
```

### 2. New: `RinkBlockouts` Table

For maintenance windows, holidays, or any manual unavailability.

```csharp
public class RinkBlockout
{
    public int Id { get; set; }
    public int RinkId { get; set; }
    public virtual Rink Rink { get; set; } = null!;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public string? Reason { get; set; }             // e.g., "Maintenance", "Christmas"
    public DateTime CreatedAt { get; set; }
}
```

### 3. Update: `Sessions` Table

Add optional `RinkId` foreign key.

```csharp
public int? RinkId { get; set; }
public virtual Rink? Rink { get; set; }
```

### 4. Update: `Games` Table

Add `RinkId` foreign key (replaces freeform `Location` string for structured rink tracking).

```csharp
public int? RinkId { get; set; }
public virtual Rink? Rink { get; set; }
```

> Note: Keep the `Location` string field for backwards compatibility / freeform notes.

### 5. Update: `Teams` Table

Add direct `LeagueId` for easier league-to-team queries.

```csharp
public int? LeagueId { get; set; }
public virtual League? League { get; set; }
```

---

## Backend API Endpoints

### Rink Management

| Method | Route                   | Description   |
| ------ | ----------------------- | ------------- |
| GET    | `/api/admin/rinks`      | Get all rinks |
| POST   | `/api/admin/rinks`      | Create a rink |
| PUT    | `/api/admin/rinks/{id}` | Update a rink |
| DELETE | `/api/admin/rinks/{id}` | Delete a rink |

### Rink Blockouts

| Method | Route                                      | Description              |
| ------ | ------------------------------------------ | ------------------------ |
| GET    | `/api/admin/rinks/{rinkId}/blockouts`      | Get blockouts for a rink |
| POST   | `/api/admin/rinks/{rinkId}/blockouts`      | Add a blockout           |
| DELETE | `/api/admin/rinks/{rinkId}/blockouts/{id}` | Remove a blockout        |

### Rink Calendar

| Method | Route                                                 | Description                                                                |
| ------ | ----------------------------------------------------- | -------------------------------------------------------------------------- |
| GET    | `/api/admin/rink-calendar?rinkId=&date=`              | Get all bookings (sessions + games + blockouts) for a rink on a given date |
| GET    | `/api/admin/rink-calendar/month?rinkId=&year=&month=` | Get days with bookings for a month (for calendar dot indicators)           |

**Calendar Response Shape (per time slot):**

```json
{
  "slots": [
    {
      "type": "session", // "session" | "game" | "blockout" | "available"
      "id": 12,
      "title": "Adult Hockey",
      "startTime": "19:00",
      "endTime": "20:30",
      "rinkId": 1,
      "leagueName": "Winter League",
      "status": "open"
    }
  ]
}
```

### Conflict Detection

| Method | Route                                     | Description                                                    |
| ------ | ----------------------------------------- | -------------------------------------------------------------- |
| POST   | `/api/admin/rink-calendar/check-conflict` | Check if a proposed time slot conflicts with existing bookings |

**Request body:**

```json
{
  "rinkId": 1,
  "startDateTime": "2026-03-15T19:00:00",
  "endDateTime": "2026-03-15T20:30:00",
  "excludeGameId": null
}
```

**Response:**

```json
{
  "hasConflict": true,
  "conflictingItem": {
    "type": "session",
    "title": "Public Skate",
    "startTime": "18:30",
    "endTime": "20:00"
  }
}
```

### League Game Scheduler

| Method | Route                          | Description                                 |
| ------ | ------------------------------ | ------------------------------------------- |
| POST   | `/api/admin/schedule/generate` | Generate proposed league game schedule      |
| POST   | `/api/admin/schedule/confirm`  | Commit proposed schedule (save games to DB) |

**Generate Request:**

```json
{
  "leagueId": 1,
  "sessionId": 5,
  "rinkId": 1,
  "startDate": "2026-04-01",
  "endDate": "2026-06-30",
  "gameLengthMinutes": 90,
  "daysOfWeek": [2, 4], // 0=Sun, 1=Mon, ..., 6=Sat
  "dailyStartTime": "18:00",
  "dailyEndTime": "22:00",
  "gamesPerNight": 2,
  "excludeUSHolidays": true,
  "excludeDates": ["2026-05-25"] // Admin-specified extra exclusions
}
```

**Generate Response:**

```json
{
  "proposedGames": [
    {
      "gameDate": "2026-04-07T19:00:00",
      "homeTeamId": 1,
      "homeTeamName": "Red Team",
      "awayTeamId": 2,
      "awayTeamName": "Blue Team",
      "rinkId": 1,
      "conflict": false
    }
  ],
  "skippedDates": [{ "date": "2026-05-26", "reason": "Memorial Day" }],
  "totalGamesGenerated": 24
}
```

> **Admin reviews the proposed schedule before confirming. No games are saved until confirm is called.**

---

## Conflict Detection Logic (Backend Service)

A proposed slot conflicts if, on the same rink:

1. An existing **Session** overlaps: `session.StartDate + session.StartTime` to `session.EndDate + session.EndTime`
2. An existing **Game** overlaps: `game.GameDate` to `game.GameDate + gameLengthMinutes`
3. A **RinkBlockout** overlaps: `blockout.StartDateTime` to `blockout.EndDateTime`

Two time slots overlap if: `slotStart < existingEnd && slotEnd > existingStart`

---

## Round Robin Schedule Generator

For N teams in a league, generate a full round robin:

- Each team plays every other team at least once
- Home/away alternated where possible
- Games distributed across available nights
- Slots checked for conflicts before being included
- Conflicting slots are skipped and the game is rescheduled to the next available slot
- If no slot can be found for a game, it is flagged in the response for admin review

---

## US Holidays (Fixed, Excluded by Default if `excludeUSHolidays: true`)

- New Year's Day (Jan 1)
- Memorial Day (last Monday in May)
- Independence Day (Jul 4)
- Labor Day (first Monday in September)
- Thanksgiving (4th Thursday in November)
- Christmas Day (Dec 25)

---

## Frontend — Rink Calendar Page

### Route

`/admin/rink-calendar` (protected by `AuthGuard` + `AdminGuard`)

### Layout

```
┌──────────────────────────────────────────────────────────┐
│  [Rink Selector ▼]   Rink Calendar         [+ Blockout]  │
├───────────────┬──────────────────────────────────────────┤
│               │  Tuesday, March 31, 2026                 │
│  Monthly      │  ────────────────────────────────────    │
│  Calendar     │  6:00 AM  [Available]                    │
│               │  7:00 AM  [Available]                    │
│  < Mar 2026 > │  8:00 AM  ████ Public Skate (8–9:30am)   │
│  Su Mo Tu ... │  9:00 AM  ████ Public Skate              │
│  ...          │  10:00 AM [Available]                    │
│  ● = has event│  ...                                     │
│               │  7:00 PM  ████ Adult Hockey – Rink 1     │
│               │  8:00 PM  ████ Adult Hockey              │
│               │  9:00 PM  [Available]                    │
└───────────────┴──────────────────────────────────────────┘
```

### Color Coding

| Color         | Meaning                                    |
| ------------- | ------------------------------------------ |
| 🟦 Blue       | Session (public skate, stick & puck, etc.) |
| 🟩 Green      | League game (scheduled)                    |
| 🟥 Red        | Blockout (maintenance / holiday)           |
| ⬜ Light gray | Available                                  |

### Interactions

- **Click a day** on the calendar → loads that day's hourly schedule on the right
- **Click an available slot** → opens "Quick Create" menu: Create Session or Schedule Game
- **Click a booked slot** → shows detail popover (session/game name, teams, time, edit link)
- **Click + Blockout** → opens blockout form (rink, date range, reason)

### League Game Scheduler Panel

Accessible via a **"Schedule League Games"** button. Opens a slide-in panel:

1. Select League
2. Select Session (filters by league)
3. Set: start date, end date, days of week, time range, game length, games per night
4. Toggle: Exclude US Holidays
5. Add custom exclude dates
6. Click **"Generate Preview"** → shows proposed games in a table
7. Admin reviews — can remove individual games from the list
8. Click **"Confirm & Save"** → saves all games, calendar refreshes

---

## Acceptance Criteria

- [ ] Rink model exists with CRUD endpoints and admin UI
- [ ] Session and Game models have optional `RinkId` FK
- [ ] Rink Calendar page accessible at `/admin/rink-calendar`
- [ ] Rink selector dropdown filters the calendar view
- [ ] Monthly calendar highlights days with bookings
- [ ] Hourly timeline shows sessions, games, and blockouts color-coded
- [ ] Clicking a day loads that day's schedule
- [ ] Conflict detection prevents double-booking same rink + time slot
- [ ] League game scheduler generates round-robin schedule
- [ ] Scheduler respects existing sessions, games, and blockouts
- [ ] US holidays excluded when option is enabled
- [ ] Admin can add custom date exclusions
- [ ] Admin reviews proposed schedule before confirming
- [ ] Confirmed games appear on the calendar immediately
- [ ] Blockouts can be added and removed from the calendar

---

## Implementation Phases

### Phase 1 — Schema & Backend Foundation

- Add Rink, RinkBlockout models and migrations
- Add RinkId to Session and Game
- Add LeagueId to Team
- Rink CRUD endpoints
- Blockout CRUD endpoints

### Phase 2 — Calendar API & Conflict Detection

- Rink calendar endpoint (day view + month indicators)
- Conflict detection service and endpoint
- Unit tests for conflict detection logic

### Phase 3 — Frontend Calendar

- Rink Calendar page with monthly calendar + hourly timeline
- Rink selector
- Color-coded time slot display
- Click interactions (view detail, quick create)
- Blockout creation form

### Phase 4 — League Game Scheduler

- Schedule generator service (round robin + conflict avoidance)
- Generate preview endpoint
- Confirm endpoint
- Frontend scheduler panel with preview table
- Confirm flow → calendar refresh

---

## Dependencies

- Rink model must exist before Sessions/Games can reference it
- Phase 1 must complete before Phase 2
- Phase 2 (conflict detection) must complete before Phase 4 (scheduler)
- Phase 3 (calendar UI) can start in parallel with Phase 4 backend

---

## Risks & Mitigations

| Risk                               | Mitigation                                                                       |
| ---------------------------------- | -------------------------------------------------------------------------------- |
| Round robin scheduling complexity  | Implement standard round robin algorithm (well-known)                            |
| Too many games, no available slots | Flag unschedulable games in preview for admin to handle manually                 |
| Existing sessions have no RinkId   | Allow null RinkId — show on "All Rinks" view, prompt admin to assign             |
| Performance on calendar queries    | Index on (RinkId, StartDate) for Sessions; index on (RinkId, GameDate) for Games |

---

## Feature 2: League Schedule Page

### Overview

A dedicated page showing all scheduled games for a league, filterable and sortable. Available in both an **admin view** (with edit/cancel actions) and a **public view** (read-only for players).

---

### Routes

| Route | Access | Description |
|-------|--------|-------------|
| `/admin/leagues/{id}/schedule` | Admin only | Full schedule with edit/cancel actions |
| `/leagues/{id}/schedule` | Public (read-only) | Players can view their team's games |

---

### Admin View — Columns & Features

**Table Columns:**

| # | Date | Time | Rink | Home Team | Away Team | Status | Score | Actions |
|---|------|------|------|-----------|-----------|--------|-------|---------|
| 1 | Apr 7, 2026 | 7:00 PM | Rink 1 | Red Team | Blue Team | Scheduled | — | Edit / Cancel |
| 2 | Apr 7, 2026 | 9:10 PM | Rink 2 | Gold Team | Green Team | Completed | 4–2 | View |

**Filters (above table):**
- **Team** — dropdown, filter to show only games involving a specific team
- **Date Range** — start date / end date pickers
- **Status** — All / Scheduled / Completed / Cancelled
- **Rink** — All / Rink 1 / Rink 2

**Sorting:**
- Default: ascending by date/time
- Clickable column headers: Date, Rink, Status

**Actions:**
- **Edit** — opens a modal to change date, time, rink, or teams (conflict detection runs on save)
- **Cancel** — marks game as Cancelled (with confirmation), does NOT delete the record
- **"Schedule Games →"** button at top — deep-links to Rink Calendar with this league pre-selected

**Bulk Actions:**
- Select multiple games → Cancel selected

**Export:**
- **Print / PDF** button — printable schedule view (no action buttons, clean layout)

---

### Public View — League Schedule Page

Accessible without login at `/leagues/{id}/schedule`.

**Features:**
- Same table layout but read-only (no Edit/Cancel)
- **Filter by Team** — player can filter to just their team's games
- **Filter by Date Range**
- **Status badges** — Scheduled (blue), Completed (green), Cancelled (red)
- **Score displayed** for completed games
- Clean, mobile-friendly layout

---

### Navigation / Linking

- **Admin League Session page** → **"Schedule Games →"** button → opens Rink Calendar with league + session pre-selected
- **Admin League list page** → each league row has **"View Schedule"** link → `/admin/leagues/{id}/schedule`
- **Public Leagues page** → each league card has **"View Schedule"** link → `/leagues/{id}/schedule`
- **Rink Calendar** → each game block has a link to the league schedule page

---

### Backend Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/admin/leagues/{id}/games` | Get all games for a league (admin, with filter params) |
| GET | `/api/leagues/{id}/games` | Get all games for a league (public, scheduled/completed only) |
| PUT | `/api/admin/games/{id}` | Update a game (date, time, rink, teams) with conflict check |
| PUT | `/api/admin/games/{id}/cancel` | Cancel a game |

**Query Parameters for GET `/api/admin/leagues/{id}/games`:**
```
?teamId=1
&status=Scheduled
&startDate=2026-04-01
&endDate=2026-06-30
&rinkId=1
```

**Response shape per game:**
```json
{
  "id": 12,
  "gameDate": "2026-04-07T19:00:00",
  "rinkId": 1,
  "rinkName": "Rink 1",
  "homeTeamId": 1,
  "homeTeamName": "Red Team",
  "awayTeamId": 2,
  "awayTeamName": "Blue Team",
  "status": "Scheduled",
  "homeScore": null,
  "awayScore": null,
  "sessionId": 5,
  "sessionName": "Winter League 2026"
}
```

---

### Phase Assignment

- **Phase 3** (parallel with frontend calendar): Build League Schedule Page backend endpoints
- **Phase 4**: Build League Schedule Page frontend (admin + public views)

---

## Updated Implementation Phases

### Phase 1 — Schema & Backend Foundation
- Add Rink, RinkBlockout models and migrations
- Add RinkId to Session and Game
- Add LeagueId to Team
- Rink CRUD endpoints
- Blockout CRUD endpoints
- Seed database with Rink 1 and Rink 2

### Phase 2 — Calendar API & Conflict Detection
- Rink calendar endpoint (day view + month count badges)
- Conflict detection service (checks sessions, games, blockouts + 10-min buffer)
- Unit tests for conflict detection logic

### Phase 3 — Frontend Calendar + League Schedule Backend
- Rink Calendar page (monthly calendar + hourly 1-hour timeline, 5am–midnight)
- Rink selector dropdown
- Color-coded time slot display
- Click interactions (view detail, quick-create form)
- Blockout creation form
- League games API endpoints (admin + public)

### Phase 4 — League Game Scheduler + League Schedule Page
- Round robin schedule generator service
- Generate preview endpoint + confirm endpoint
- Frontend scheduler panel (inputs → preview table → confirm)
- League Schedule Page — admin view (filterable, edit/cancel, export)
- League Schedule Page — public view (read-only, filter by team)
- Navigation links between calendar ↔ schedule pages



| Phase                                       | Estimate        |
| ------------------------------------------- | --------------- |
| Phase 1 — Schema & Backend Foundation       | 3–4 hours       |
| Phase 2 — Calendar API & Conflict Detection | 3–4 hours       |
| Phase 3 — Frontend Calendar                 | 4–6 hours       |
| Phase 4 — League Game Scheduler             | 4–6 hours       |
| **Total**                                   | **14–20 hours** |
