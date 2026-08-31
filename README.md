# FixMyCity

**Smart Civic Problem Reporting & Resolution Platform**

FixMyCity is a role-based web platform that lets citizens report civic issues — potholes, garbage pileups, broken streetlights, water leaks, damaged public property, and faulty traffic signals — and tracks each issue through a transparent life cycle: **Report → Analyze → Group → Prioritize → Assign → Repair → Verify → Resolve**.

Built for **CSE 3200 – Software Development V**.

---

## The Problem

Civic issue reporting today is fragmented across phone calls, social media, and paper complaints. Citizens rarely get meaningful progress updates, authorities receive duplicate reports with no way to gauge urgency, and a case can be marked "resolved" with no public evidence it actually was. FixMyCity replaces this with a single, location-aware, evidence-driven workflow connecting citizens, departments, and administrators.

**Aligned SDGs:** SDG 11 (Sustainable Cities and Communities), SDG 16 (Peace, Justice and Strong Institutions).

---

## Core Features

| Area | What it does |
|---|---|
| **Citizen Reporting** | Submit a report with photo, category, and pinned location; track status; verify or reject a claimed resolution |
| **Smart Problem Map** | City-wide map with severity-differentiated markers, filterable by category, area, and department |
| **Duplicate Detection** | Same category + within ~50m + similar description → suggests an existing issue instead of creating a new one |
| **Explainable Priority Scoring** | Report count + severity + location importance + duration + safety risk → Low / Medium / High / Critical |
| **Department Routing** | Category-to-department auto-assignment with before/after evidence required before closure |
| **Citizen Verification** | Resolutions require citizen sign-off; rejection reopens the issue with a recorded reason |
| **Admin Dashboard** | Manage users, departments, categories; review flagged reports; hotspot and trend analytics |

---

## Tech Stack

- **Backend:** ASP.NET Core MVC (.NET 9/10)
- **ORM:** Entity Framework Core
- **Database:** MySQL (via Pomelo.EntityFrameworkCore.MySql)
- **Auth:** ASP.NET Core Identity (role-based: Citizen, Department Staff, Department Manager, Administrator)
- **Frontend:** Razor Views, Bootstrap 5, Leaflet.js (map)
- **Architecture:** Layered — `Web` (UI/controllers) → `Services` (business logic) → `Data` (EF Core models & DbContext)

---

## Project Structure
