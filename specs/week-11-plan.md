# Week 11 Implementation Plan - Late January 2026

## Status: 📋 Planned

## Primary Focus: League Management, Rink Scheduler

---

## 1. League Management (Admin CRUD + Coming Soon)

**Priority: HIGH**  
**Estimated Hours: 6-8 hours**

### Goals

- Admins can **create, edit, and delete** leagues
- Newly created leagues display as **"Coming Soon"** on the public leagues page

### Functional Requirements

- Admin UI for league CRUD (name, description, start date, prices, registration windows)
- Delete confirmation with warning if leagues have sessions/registrations
- League list reflects updates immediately
- Public leagues page shows **Coming Soon** badge/status for new leagues

### Technical Notes

- Use `registrationOpenDate` to determine "Coming Soon" status
- If `registrationOpenDate` is in the future → show "Coming Soon"
- If not present, fall back to `startDate` in future
- Add admin endpoints for league CRUD:
  - `POST /api/admin/leagues`
  - `PUT /api/admin/leagues/{id}`
  - `DELETE /api/admin/leagues/{id}`

### Acceptance Criteria

- [x] Admin can create a league from the admin UI
- [x] Admin can edit existing leagues
- [x] Admin can delete leagues (with confirmation)
- [x] Public leagues page displays "Coming Soon" for newly created leagues

---

## 2. Rink Scheduler (Admin)

**Priority: HIGH**  
**Estimated Hours: 10-14 hours**

### Overview

Add a rink-wide scheduling system for ice time availability and league scheduling.

### Core Features

- **Availability Calendar** showing available/unavailable ice time
- **League Schedule Generator** with constraints and overrides

### Inputs / Constraints

- **Daily time frame** (start time and end time, 12:00am–11:59pm)
- **Game length** (minutes)
- **Exclude days** (specific dates + fixed US holidays)
- **Rink count** (how many rinks are available)
- **Admin override** (manual adjustments to generated schedule)

### Scheduler Output

- Time slots across all rinks
- Conflict detection with existing sessions
- Ability to reserve blocks for leagues

### Acceptance Criteria

- [ ] Admin can view a rink availability calendar
- [ ] Admin can generate a league schedule from inputs
- [ ] Admin can exclude dates/holidays (fixed US holidays + admin-managed exclusions)
- [ ] Admin can override schedule entries manually
- [ ] Supports multiple rinks

---

## 3. Sessions Page Polish (UI/UX)

**Priority: MEDIUM**  
**Estimated Hours: 2-4 hours**

### Goals

- Visual refinement for sessions list cards
- Improve hierarchy and readability
- Stronger CTA for registration

### Potential Enhancements

- Session card header layout improvements
- Badges for status (Open, Opening Soon, Full)
- Better spacing and alignment for date/location/price
- Responsive improvements for mobile

---

## 4. Deferred Week 10 Features (Optional)

**Priority: LOW - Polish Features**  
**Estimated Hours: 4-6 hours**

These features were deferred from Week 10 and can be completed if time permits:

### 4.1 Admin User Detail Page (2-3 hours)

- Click user row to view detailed profile
- User statistics dashboard
- Registration and payment history
- Admin notes section
- Quick actions

### 4.2 Team Color Management (1-2 hours)

- Color picker for team colors
- Predefined palette + custom hex input
- Live preview on team cards

### 4.3 Footer Enhancement (0.5-1 hour)

- Professional footer with contact info
- Social media links
- Quick links and copyright

### 4.4 Home Page Polish (1-2 hours)

- Testimonials section
- Feature highlights
- Statistics section
- Newsletter signup

---

## 5. League Standings & Statistics (May Defer to Week 12)

**Priority: MEDIUM**  
**Estimated Hours: 8-10 hours**

Begin work on league standings if other items are completed:

### Features

- League standings table (W/L/T records)
- Player statistics (Goals, Assists, Points)
- Team statistics page
- Sortable/filterable tables
- Season leaders boards
- Export to PDF/Excel

### Technical

- Database: Create Stats table
- Backend: Statistics aggregation endpoints
- Frontend: Statistics dashboard components
- Charts: Chart.js or ngx-charts

**Note:** This may be deferred to Week 12.

---

## 6. Completed This Week (January 26, 2026)

### ✅ Password Confirmation on Registration

- Added confirmPassword field
- Custom validator for password matching
- Real-time validation feedback
- Error message: "Passwords do not match"

### ✅ Login UI Improvements

- Reduced login box size (col-lg-4)
- Reduced padding for compact design
- Smaller icon (3rem instead of 4rem)
- Better screen fit

---

## Priority Order

1. **COMPLETED:** League Management CRUD + Coming Soon ✅
2. **HIGH:** Rink Scheduler (10-14 hours)
3. **COMPLETED:** Sessions Page Polish
4. **COMPLETED:** Password confirmation on registration
5. **COMPLETED:** Login UI improvements
6. **LOW:** Deferred Week 10 polish features (4-6 hours)
7. **MEDIUM:** League standings and statistics (8-10 hours - may defer to Week 12)

---

## Timeline

### Week 11 Schedule (Late January 2026)

**Days 1-2:** League CRUD + Coming Soon (6-8 hours)

- Admin create/edit/delete leagues
- Coming Soon badge logic on leagues page
- Backend CRUD endpoints

**Days 3-5:** Rink Scheduler (10-14 hours)

- Availability calendar
- Schedule generator inputs
- Overrides + multi-rink support

**Day 6:** Sessions Page Polish + QA (completed)

**Remaining Time:** Optional polish features or start on statistics

---

## Dependencies

- Week 10 completion ✅
- Icon library selected (Bootstrap Icons ✅)

---

## Risks & Mitigation

### Risks

1. Rink scheduler complexity may exceed estimates
2. League delete may need cascade handling if teams/registrations exist

### Mitigation

1. Scope rink scheduler to MVP first, add overrides in iteration
2. Add warnings on delete if related records exist

---

## Success Metrics

- [ ] League CRUD is complete and stable
- [ ] Newly created leagues show Coming Soon on leagues page
- [ ] Rink scheduler can generate schedules with constraints
- [ ] Page load time remains under 2 seconds
- [ ] Accessibility score remains 95+ on Lighthouse
- [ ] All existing functionality continues to work

---

## Notes

- League CRUD and Coming Soon status are required for admin workflows
- Rink scheduler is a major scheduling enhancement
- Sessions Page Polish is complete ✅

---

## Next Steps After Week 11

### Week 12 Likely Focus:

1. League Standings & Statistics (if deferred from Week 11)
2. JWT Authentication Migration (high priority security update)
3. Framework Upgrades (.NET 10, Angular 21 if released)
4. Additional polish features based on user feedback

---

## Total Estimated Hours: 18-26 hours

- **League CRUD + Coming Soon:** 6-8 hours
- **Rink scheduler:** 10-14 hours
- **Optional polish features:** 4-6 hours (if time permits)
- **League statistics:** 8-10 hours (may defer to Week 12)

**Status:** Ready to begin implementation  
**Start Date:** Late January 2026  
**Target Completion:** Early February 2026
