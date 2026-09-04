# event-management-system
ST10485700- POE PART 1

# RaceDay — Event Management System (Part 1: System Planning and Database)

## Description
RaceDay is a full-stack, web-based event management system built for the South African road running,
walking, and cycling community. Event Organisers can create and manage events, categories, and
participant results. Participants can browse upcoming events, enter events by selecting a category,
track their personal performance history, and prepare for race day using route information.

This repository currently contains the **Part 1** deliverables: the system plan and database.

## Roles
- **Organiser** — can create, edit, and delete events; manage event categories; capture participant
  results; and view all enrolments for their events.
- **Participant** — can create an account, browse events, enter an event by selecting a category, view
  their own enrolments, and track their own results.

## Part 1 Deliverables (in /docs)
- `RaceDay-ERD.png` — Entity Relationship Diagram, 6 entities: Users, Events, Categories, Routes,
  EventEnrolments, Results.
- `API_Endpoint_Plan.pdf` — full API endpoint plan (30 endpoints across Authentication, User Profile,
  Events, Categories, Event Enrolments, Results, and Routes).
- `RaceDayEvents.sql` — SQL Server script: table creation with PK/FK/CHECK/UNIQUE/DEFAULT constraints,
  plus seed data (2 Organisers, 2 Participants, 3 Events, 7 Categories, 5 Enrolments, 3 Results,
  7 Routes).

## Design Notes
- `Routes` belongs to `Categories`, not `Events` — different categories within the same event (e.g.
  10km vs 21km) can follow different courses.
- Weather is treated as live, external data fetched at request time in later parts — it is not modelled
  as a stored entity.
- `BibNumber` is globally unique across all enrolments (not just per category).

## CI/CD
A GitHub Actions workflow (`.github/workflows/validate-docs.yml`) checks that the `/docs` folder exists
and contains the ERD, endpoint plan, and SQL script on every push.

**Green build:**
![CI Passing](docs/ci-success-screenshot.png)

## Video Walkthrough
**YouTube (unlisted):** <!-- paste your video link here once recorded -->

The video covers: the ERD and the reasoning behind each entity/relationship, the API endpoint plan and
role decisions, and a live run of the SQL script in SSMS.

## AI Tool Disclosure
AI tools (Claude) were used during planning to discuss design trade-offs, review draft ERDs, endpoint
plans, and SQL scripts for consistency and missing constraints, and generate diagram files. All final
design decisions (entity relationships, role permissions, constraint choices) were made and understood
by the author, who can explain and defend each one.
