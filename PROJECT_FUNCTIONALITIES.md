# FixMyCity – Comprehensive Project Functionalities & Architecture Guide

**Platform:** FixMyCity – Smart Civic Problem Reporting & Resolution Platform  
**Target Framework:** ASP.NET Core MVC (.NET 10.0)  
**Database:** MySQL / MariaDB via Pomelo Entity Framework Core  
**Course & Project:** CSE 3200 – Software Development V  
**Sustainable Development Goals:** SDG 11 (Sustainable Cities and Communities) & SDG 16 (Peace, Justice and Strong Institutions)  

---

## 1. Executive Summary & System Overview

**FixMyCity** is an enterprise-grade, role-based civic incident management and tracking platform designed to bridge the communication gap between citizens, municipal service departments, field work crews, and city administration. 

In traditional municipal structures, civic complaints (such as potholes, hazardous open drains, overflowing garbage bins, faulty traffic signals, and broken streetlights) suffer from lack of transparency, repeated duplicate entries, absence of objective priority metrics, and unverified closure claims. FixMyCity addresses these challenges by implementing an evidence-driven, transparent 8-stage life cycle:

$$\text{Report} \longrightarrow \text{Analyze} \longrightarrow \text{Group} \longrightarrow \text{Prioritize} \longrightarrow \text{Assign} \longrightarrow \text{Repair} \longrightarrow \text{Verify} \longrightarrow \text{Resolve}$$

```
+-----------------------------------------------------------------------------------+
|                                  FIXMYCITY ECOSYSTEM                              |
+--------------------+---------------------+--------------------+-------------------+
|      CITIZEN       |  DEPARTMENT STAFF   | DEPARTMENT MANAGER |   ADMINISTRATOR   |
|  - GPS Incident    |  - Priority Radar   |  - Dispatch Radar  |  - Full System    |
|    Reporting       |  - Field Operations |  - Staff Workload  |    Metrics & KPI  |
|  - Duplicate Check |  - Status Updates   |    Balancing       |  - Role Management|
|  - Impact Score    |  - Before/After     |  - Dept Assignment |  - Dept/Category  |
|  - Interactive Map |    Evidence Upload  |  - Resolution Sign |  - Hotspot        |
|  - CSV Export      |  - Repair Notes     |    off             |    Analytics      |
+--------------------+---------------------+--------------------+-------------------+
```

---

## 2. Technology Stack & Technical Architecture

The project follows a clean **3-tier Layered Architecture** with distinct separation of concerns:

```
[ Presentation Layer: FixMyCity.Web ]
      │ (Controllers, Views, ViewModels, Identity UI, Static Assets)
      ▼
[ Business / Service Layer: FixMyCity.Services ]
      │ (Domain workflows & potential micro-services)
      ▼
[ Data Access & Persistence Layer: FixMyCity.Data ]
      │ (DbContext, EF Core Models, MySQL Migrations)
      ▼
[ Database: MySQL Server / MariaDB / XAMPP ]
```

### Detailed Component Stack

| Component | Technology | Version / Specification |
|---|---|---|
| **Runtime & Framework** | ASP.NET Core MVC | .NET 10.0 (`net10.0`) |
| **Object-Relational Mapper (ORM)** | Entity Framework Core | 9.0.0 |
| **Database Provider** | `Pomelo.EntityFrameworkCore.MySql` | 9.0.0 (Supports MySQL 8.x, 9.x, MariaDB) |
| **Authentication & Authorization** | ASP.NET Core Identity | Cookie-based auth with `IdentityDbContext` |
| **Frontend Framework** | Bootstrap | 5.3 + Bootstrap Icons 1.11.3 |
| **Geographic Mapping** | Leaflet.js | 1.9.4 with OpenStreetMap Tiles |
| **Design Language** | Custom Lavender/Purple Design System | Google Fonts (`DM Sans`, `Plus Jakarta Sans`, `Oswald`, `JetBrains Mono`) |
| **Client-Side Progressive Enhancement** | JavaScript (ES6+) | `IntersectionObserver` scroll animations, AJAX file previews, HTML5 Geolocation |
| **Deployment & Local Setup** | Multi-environment connection handler | Automatic migration + password fallback for local XAMPP |

---

## 3. Database Architecture & Entity Relationships

The data layer is configured inside `FixMyCity.Data.FixMyCityDbContext`. Below is the complete relational breakdown of all core entities:

```
                      +-------------------+
                      |  ApplicationUser  |
                      +-------------------+
                       ▲        ▲       ▲
                       │        │       │
       AssignedStaffId │        │       │ UserId
                       │        │       │
          +------------+        │       +-------------+
          |                     │                     |
+------------------+   +-----------------+   +------------------+
|    Assignment    |   |     Report      |──▶|      Issue       |
+------------------+   +-----------------+   +------------------+
| - Id             |   | - Id            |   | - Id             |
| - IssueId        |   | - Description   |   | - PriorityScore  |
| - DepartmentId   |   | - PhotoPath     |   | - Status         |
| - AssignedStaff  |   | - Latitude      |   | - CategoryId     |
| - Status         |   | - Longitude     |   +------------------+
| - AssignedAt     |   | - CreatedAt     |            │
+------------------+   | - CategoryId    |            ▼
        │              | - UserId        |   +------------------+
        ▼              | - IssueId       |   |  StatusHistory   |
+------------------+   +-----------------+   +------------------+
|     Evidence     |            │            | - Id             |
+------------------+            ▼            | - IssueId        |
| - Id             |   +-----------------+   | - PreviousStatus |
| - AssignmentId   |   |    Category     |   | - NewStatus      |
| - PhotoPath      |   +-----------------+   | - Note           |
| - Note           |   | - Id            |   | - ChangedAt      |
| - UploadedAt     |   | - Name          |   +------------------+
+------------------+   | - DepartmentId  |
                       +-----------------+
                                │
                                ▼
                       +-----------------+
                       |   Department    |
                       +-----------------+
                       | - Id            |
                       | - Name          |
                       | - Description   |
                       +-----------------+
```

### Entity Specifications

1. **`ApplicationUser`** (inherits from `IdentityUser`):
   - `FullName`: Resident or official's full legal name.
   - `Role`: System access role (`Citizen`, `DepartmentStaff`, `DepartmentManager`, `Administrator`).
   - `Address`: Optional residential street address.
   - `ProfileImageFileName`: Avatar filename stored in `wwwroot/uploads/profile-pictures/`.

2. **`Department`**:
   - `Id`: Auto-incrementing primary key.
   - `Name`: Department title (e.g., "Roads & Transport", "Waste Management", "Water & Utilities", "Public Lighting").
   - `Description`: Functional overview of the municipal agency.
   - `Categories`: Navigation collection of all issue categories routed to this department.

3. **`Category`**:
   - `Id`: Unique category identifier.
   - `Name`: Problem class (e.g., "Pothole / Road Damage", "Water Leak / Drainage").
   - `ResponsibleDepartment`: Plain-text department reference.
   - `DepartmentId`: Foreign key to `Department`.

4. **`Issue`**:
   - Represents the canonical problem instance. Multiple duplicate community reports aggregate into a single `Issue`.
   - `PriorityScore`: Numerical triage weight dynamically increased by repeated reports and severity.
   - `Status`: Current state: `Reported` $\rightarrow$ `InProgress` $\rightarrow$ `Resolved` $\rightarrow$ `Verified`.
   - `CategoryId`: Foreign key pointing to `Category`.
   - `Reports`: Collection of all linked citizen reports.

5. **`Report`**:
   - The citizen's individual submission.
   - `Description`: Detailed textual explanation of the issue.
   - `PhotoPath`: Relative file path to the uploaded photo (`/uploads/reports/...`).
   - `Latitude` & `Longitude`: Exact geographic coordinates.
   - `CreatedAt`: UTC timestamp of submission.
   - `IssueId`: Nullable foreign key linking this report to a parent `Issue`.
   - `UserId`: Identity foreign key linking to `ApplicationUser`.

6. **`Assignment`**:
   - Work dispatch record managed by Department Managers.
   - `IssueId`: The target issue to repair.
   - `DepartmentId`: The municipal unit responsible for work.
   - `AssignedStaffUserId`: The field worker assigned to the task.
   - `AssignedAt`: Dispatch timestamp.
   - `Status`: `Assigned` $\rightarrow$ `InProgress` $\rightarrow$ `Completed`.

7. **`Evidence`**:
   - Post-repair physical verification artifact.
   - `AssignmentId`: Link to the work order.
   - `PhotoPath`: Image proof of completed repair work (`/uploads/evidence/...`).
   - `Note`: Field crew notes describing materials used or repair actions.
   - `UploadedAt`: Timestamp of evidence submission.

8. **`StatusHistory`**:
   - Immutable audit trail tracking every status change across an issue's lifespan.
   - `PreviousStatus`, `NewStatus`, `Note`, `ChangedAt`.

---

## 4. Role-Based Access Control (RBAC) & User Workspaces

FixMyCity features four distinct user roles with dedicated routing, custom navigation sidebars, and tailored workspaces.

### 4.1 Citizen (Community Resident)
*Default role for all newly registered accounts.*
- **Landing & Dashboard:** Access to `/Home/Dashboard`, showcasing real-time community statistics (Total Reports, Pending Action, Resolved & Fixed, My Submissions), live community incident stream, and resolution rate progress bar.
- **Reporting Interface:** Fast submission via `/Reports/Create` with HTML5 geolocation and photo capture.
- **Personal Records:** Track submitted issues via `/Reports/MyReports`, filter by status, and export personal history to CSV.
- **Civic Impact Tracking:** View calculated impact score and community reach at `/Reports/Impact`.
- **Audit Notifications:** View chronological updates to personal reports at `/Reports/Notifications`.
- **Public Map:** Explore neighborhood issue density via the interactive Leaflet map at `/Map/Index`.
- **Verification Portal:** Inspect repair evidence and resolution history at `/Reports/Verify/{id}`.
- **Profile Customization:** Update personal details, contact number, avatar, and password via `/Profile/Index`.

### 4.2 Department Staff (Field Crews & Technicians)
*Field workers responsible for executing physical repairs.*
- **Staff Workspace:** Routed automatically to `/Staff/Index` upon login.
- **Civic Priority Radar:** Top banner highlights the highest-priority issue requiring immediate field attention (calculated from `PriorityScore + Reports.Count`).
- **Status Advancement:** Directly update issue states (`InProgress`, `Resolved`, `Verified`), which automatically generates historical audit entries.
- **Evidence Submission:** Access `/Staff/Evidence/{id}` to upload proof-of-work photographs and technical notes before an issue can be closed.

### 4.3 Department Manager (Operations & Dispatch)
*Municipal supervisors overseeing department workloads and dispatching teams.*
- **Manager Workspace:** Routed automatically to `/Manager/Index` upon login.
- **Dispatch Radar:** Identifies unassigned high-priority issues that need staffing.
- **Issue Assignment Engine:** Form to assign open issues to specific departments and delegate to individual field staff.
- **Assignment Progression:** Review active assignments, monitor progress, and mark tasks `Completed` (which automatically transitions the associated parent issue to `Resolved`).

### 4.4 Administrator (City Operations Control)
*Authorized municipal directors with oversight over all system data.*
- **Admin Command Center:** High-level executive dashboard at `/Admin/Index` detailing city-wide KPIs.
- **Report Review & Moderation:** Full control at `/Admin/Reports` to inspect submissions and override statuses.
- **User Role Management:** Portal at `/Admin/Users` allowing administrators to promote or reassign user roles between `Citizen`, `DepartmentStaff`, and `DepartmentManager`.
- **Department Administration:** Maintain municipal department entities at `/Admin/Departments`.
- **Category Management:** Configure issue categories and map them to responsible departments at `/Admin/Categories`.
- **Urban Hotspot Analytics:** View aggregate issue breakdowns by category at `/Admin/Analytics` to detect recurring infrastructure failures.

---

## 5. Core Functional Modules & Algorithms

### 5.1 Smart Geolocation & Incident Reporting
- **File:** `src/FixMyCity.Web/Controllers/ReportsController.cs` & `Views/Reports/Create.cshtml`
- **Functionality:** 
  1. Citizens select an issue category from a dynamically seeded dropdown.
  2. The browser's HTML5 Geolocation API (`navigator.geolocation.getCurrentPosition`) enables one-click GPS coordinate capture with fallback to manual coordinate input.
  3. Image upload with client-side preview (`FileReader` API) and server-side disk storage under `/wwwroot/uploads/reports/` using unique GUID prefixes.
  4. Authentication detection: If authenticated, automatically links the user ID; for guest/demo mode, allows selection or fallback assignment.

### 5.2 Spatial Duplicate Detection & Priority Boost Algorithm
- **File:** `src/FixMyCity.Web/Controllers/ReportsController.cs` (lines 124–131)
- **Concept:** When multiple citizens report the same incident, the platform avoids cluttering the system with duplicate work tickets.
- **Algorithm:**
  $$\Delta \text{Lat} = |\text{Lat}_{\text{new}} - \text{Lat}_{\text{existing}}| < 0.00045^\circ \quad (\approx 50\text{ meters})$$
  $$\Delta \text{Lng} = |\text{Lng}_{\text{new}} - \text{Lng}_{\text{existing}}| < 0.00045^\circ \quad (\approx 50\text{ meters})$$
- **Behavior:**
  - If a report exists in the same category within ~50 meters, the new report is automatically linked to the existing `IssueId`.
  - The parent issue's priority score is boosted:
    $$\text{Issue.PriorityScore} \mathrel{+}= 2$$
  - This immediately signals to managers that multiple community members are impacted by this specific hazard.

### 5.3 Explainable Civic Priority Radar
- **Files:** `ManagerController.cs` (line 33), `StaffController.cs` (line 29)
- **Calculation:**
  $$\text{Urgency Rank} = \text{Issue.PriorityScore} + \text{Issue.Reports.Count}$$
- **UI Integration:** An animated high-visibility radar card at the top of the Staff and Manager consoles immediately exposes the top-ranked civic problem, ensuring urgent hazards are addressed first without manual sorting.

### 5.4 Two-Way Department Dispatch & Work Order Pipeline
- **Files:** `ManagerController.cs`, `StaffController.cs`
- **Workflow:**
  1. Unassigned issues appear in the Manager dispatch queue.
  2. Manager selects an Issue, targets a Department, and picks a specific staff member.
  3. An `Assignment` record is created with status `Assigned`, and the Issue status is automatically transitioned to `InProgress`.
  4. The assigned staff member sees the task in their workspace, travels to the location, and updates the status to `InProgress`.
  5. Upon completion, the staff member uploads proof-of-work photos and repair notes via `/Staff/Evidence/{id}`.
  6. Marking the assignment `Completed` automatically marks the Issue as `Resolved`.

### 5.5 Resolution Verification & Citizen Auditing
- **File:** `Views/Reports/Verify.cshtml`
- **Functionality:**
  - Citizens can visit `/Reports/Verify/{id}` to inspect the full historical timeline of their report.
  - The timeline queries `StatusHistory` to show every stage transition with timestamps and actor notes.
  - Supporting reports notice: Displays how many other citizens reported the same issue nearby.
  - Photo Evidence Gallery: Displays the actual before and after repair photos uploaded by city staff, providing proof of resolution.

### 5.6 Interactive GIS Problem Map
- **Files:** `Controllers/MapController.cs`, `Views/Map/Index.cshtml`
- **Technology:** Leaflet.js with OpenStreetMap cartography.
- **Features:**
  - Automatically loads coordinates for all community reports.
  - Dynamic interactive map markers with popups displaying the category name, report description, and exact coordinates.
  - Responsive tabular directory below the map showing readable area descriptions and coordinates.

### 5.7 Civic Gamification, Impact Score & CSV Export
- **File:** `Controllers/ReportsController.cs` (Actions: `Impact`, `ExportMyReports`)
- **Impact Score Metric:**
  $$\text{Impact Score} = (\text{Total Reports} \times 10) + (\text{Resolved/Verified Reports} \times 20)$$
- **Community Reach:** Aggregates total citizen reports clustered into the user's initiated issues:
  $$\text{Community Reach} = \sum \text{Issue.Reports.Count}$$
- **CSV Data Export:** One-click generation of `fixmycity-my-reports.csv` including Report ID, Category, Description, Status, Latitude, Longitude, and Created Timestamp.

### 5.8 Urban Hotspot Analytics & Administrative Operations
- **Files:** `Controllers/AdminController.cs`, `Views/Admin/Analytics.cshtml`
- **Features:**
  - Real-time LINQ grouping of reports by category:
    ```csharp
    var byCategory = await _context.Reports
        .Include(r => r.Category)
        .GroupBy(r => r.Category!.Name)
        .Select(g => new { Category = g.Key, Count = g.Count() })
        .ToListAsync();
    ```
  - Allows municipal leaders to allocate resources to recurring problem areas (e.g., road maintenance vs. sanitation).

### 5.9 User Profile Management & Security
- **Files:** `Controllers/ProfileController.cs`, `Views/Profile/Index.cshtml`
- **Features:**
  - Edit full name, contact phone number, and residential street address.
  - Avatar management: image upload supporting `.jpg`, `.jpeg`, `.png`, `.gif` up to 2MB, stored in `wwwroot/uploads/profile-pictures/`.
  - Password change with validation through ASP.NET Identity's `ChangePasswordAsync` and immediate session refresh via `RefreshSignInAsync`.

---

## 6. Comprehensive Controller & Routing Reference

| Controller | Action / Route | HTTP Method | Authorized Role | Description |
|---|---|---|---|---|
| **Home** | `/Home/Index` | `GET` | Anonymous | Public landing page with features overview and login modals |
| **Home** | `/Home/Dashboard` | `GET` | Authenticated | Citizen command center with live feed and KPIs (redirects staff/managers) |
| **Home** | `/Home/About` | `GET` | Anonymous | Mission statement, SDG alignment, and platform vision |
| **Reports** | `/Reports/Index` | `GET` | Anonymous | Public incident directory with category & status filters |
| **Reports** | `/Reports/Create` | `GET` | Anonymous / User | Form to report a new problem with GPS & photo |
| **Reports** | `/Reports/Create` | `POST` | Anonymous / User | Handles duplicate check, file saving, and database insertion |
| **Reports** | `/Reports/MyReports` | `GET` | Citizen | View personal submitted reports with status filtering |
| **Reports** | `/Reports/Verify/{id}` | `GET` | Anonymous / User | Detailed audit view with status timeline and repair photos |
| **Reports** | `/Reports/Notifications` | `GET` | Citizen | Chronological notifications feed of report status changes |
| **Reports** | `/Reports/Impact` | `GET` | Citizen | Displays impact score and community reach statistics |
| **Reports** | `/Reports/ExportMyReports` | `GET` | Citizen | Generates and downloads a CSV export of user reports |
| **Map** | `/Map/Index` | `GET` | Anonymous / User | Interactive GIS map displaying all incident pins |
| **Profile** | `/Profile/Index` | `GET` | Authenticated | View profile information, picture, and contact details |
| **Profile** | `/Profile/Index` | `POST` | Authenticated | Update user profile and upload new avatar photo |
| **Profile** | `/Profile/ChangePassword` | `POST` | Authenticated | Change account password via Identity |
| **Staff** | `/Staff/Index` | `GET` | DepartmentStaff | Field workspace with Priority Radar and issue list |
| **Staff** | `/Staff/UpdateStatus` | `POST` | DepartmentStaff | Update issue status (`InProgress`, `Resolved`, `Verified`) |
| **Staff** | `/Staff/Evidence/{id}` | `GET` | DepartmentStaff | Form to upload repair photos and notes |
| **Staff** | `/Staff/Evidence` | `POST` | DepartmentStaff | Saves repair photo to disk and creates `Evidence` record |
| **Manager** | `/Manager/Index` | `GET` | DepartmentManager | Assignment overview and dispatch radar |
| **Manager** | `/Manager/Assign` | `POST` | DepartmentManager | Assigns issue to department and staff member |
| **Manager** | `/Manager/UpdateAssignment` | `POST` | DepartmentManager | Updates assignment status and auto-resolves issue |
| **Admin** | `/Admin/Index` | `GET` | Administrator | City operations dashboard with executive metrics |
| **Admin** | `/Admin/Reports` | `GET` | Administrator | Review and moderate all community reports |
| **Admin** | `/Admin/UpdateReportStatus` | `POST` | Administrator | Administrative status override with audit logging |
| **Admin** | `/Admin/Users` | `GET` | Administrator | User list with role update capabilities |
| **Admin** | `/Admin/UpdateUserRole` | `POST` | Administrator | Dynamically promotes/demotes user roles |
| **Admin** | `/Admin/Departments` | `GET` / `POST` | Administrator | Lists and creates municipal departments |
| **Admin** | `/Admin/Categories` | `GET` / `POST` | Administrator | Lists and creates categories linked to departments |
| **Admin** | `/Admin/Analytics` | `GET` | Administrator | Hotspot analysis by category volume |
| **Identity** | `/Identity/Account/Login` | `GET` / `POST` | Anonymous | User authentication with role-based redirection |
| **Identity** | `/Identity/Account/Register` | `GET` / `POST` | Anonymous | Citizen registration with default `Citizen` role |
| **Identity** | `/Identity/Account/Logout` | `POST` | Authenticated | Signs out user and redirects to home page |

---

## 7. Database Initialization, Migration & Seeding System

In `src/FixMyCity.Web/Program.cs`, the application includes an automated bootstrap routine:

1. **Automatic Database Migration:**
   ```csharp
   await database.Database.MigrateAsync();
   ```
   Ensures the MySQL database schema is always up to date upon startup without requiring manual CLI migrations.

2. **Smart Database Connection Resilience:**
   Checks the connection string from `appsettings.json` and overrides password with `MYSQL_PASSWORD` environment variable if present. If connection fails due to password mismatch (common in local XAMPP setups), it automatically falls back to an empty password (`password=""`) to ensure out-of-the-box local developer convenience.

3. **Core Reference Data Seeding:**
   Seeds standard municipal departments and issue categories if the database is fresh:
   - **Departments:** Roads & Transport, Waste Management, Water & Utilities, Public Lighting.
   - **Categories:** Pothole / Road Damage, Broken Traffic Signal, Garbage / Waste, Water Leak / Drainage, Broken Streetlight, Damaged Public Property.

4. **Demonstration Activity Seeding:**
   Populates realistic initial civic reports across Dhaka coordinates (`23.78° N, 90.40° E`) with mixed statuses (`Reported`, `InProgress`, `Resolved`, `Verified`) so the dashboard and problem map are visually informative and functional upon initial deployment.

---

## 8. Summary of Accomplishments & Future Recommendations

### Key Accomplishments
- Implemented an end-to-end civic complaint resolution lifecycle connecting 4 distinct stakeholders.
- Intelligent spatial clustering preventing duplicate work orders and objectively measuring problem severity.
- Automated priority scoring with the "Civic Priority Radar" directing field crews to the most critical hazards.
- Evidence-based accountability requiring photographic proof before issue closure.
- Modern, responsive, aesthetic UI with custom color systems, mobile offcanvas drawer, and interactive maps.

### Recommended Future Enhancements
1. **AI Image Verification:** Integrate computer vision (e.g., Google Vision API or Gemini Flash) to automatically verify whether uploaded images match the selected category (e.g., verifying a pothole photo).
2. **Push & SMS Notifications:** Integrate Twilio or Firebase Cloud Messaging for real-time mobile SMS/push alerts when an issue status changes.
3. **SLA Deadlines & Auto-Escalation:** Add target turnaround times (e.g., 48 hours for potholes) that automatically escalate neglected issues to department heads.
4. **Mobile Native Application:** Build a Flutter or React Native mobile client utilizing the existing backend APIs.
