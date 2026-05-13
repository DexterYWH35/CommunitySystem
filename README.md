# Community Services Portal

This repository currently contains a .NET 8 ASP.NET Core MVC application named `CommunitySystem`.

The application is a community content portal with:

- role-based accounts
- posts and comments
- likes for posts and comments
- image upload for posts
- lost & found module
- complaints module
- marketplace module
- SQLite with Entity Framework Core
- Bootstrap-styled Razor UI using the current magenta / plum design system

## Current Status

Implemented so far:

- ASP.NET Core MVC project structure
- SQLite-backed `ApplicationDbContext`
- ASP.NET Core Identity authentication
- role setup for `Admin` and `User`
- public landing homepage
- role-protected posts and comments areas
- post CRUD
- comment CRUD
- inline comment posting from post details
- post likes
- comment likes
- post image upload
- homepage recent posts section
- homepage most liked posts section
- seeded sample data for development/testing
- custom login, register, and access denied pages
- chat sessions with products owner
- supporting chat session with admin/ management users
- complaint cases (INCLUDING CRUD)
- lost and found cases (INCLUDING CRUD)
- marketplace module (INCLUDING CRUD)

## Project Location

Main application:

- [CommunitySystem](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem)

Important startup and infrastructure files:

- [Program.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Program.cs)
- [ApplicationDbContext.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/ApplicationDbContext.cs)
- [DatabaseInitializer.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/DatabaseInitializer.cs)
- [SeedData.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/SeedData.cs)
- [appsettings.json](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/appsettings.json)

## Where Data Is Stored

### 1. Application Data

Structured application data is currently stored in SQLite:

- database file: [communitysystem.db](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/communitysystem.db)

This includes:

- users
- roles
- posts
- comments
- post likes
- comment likes

SQLite support files may also appear beside it:

- `communitysystem.db-shm`
- `communitysystem.db-wal`

### 2. Uploaded Images

Uploaded post images are stored on disk under:

- `CommunitySystem/wwwroot/uploads/posts/`

These are not stored inside the database as binary blobs. The database stores the image path reference, and the actual image file is saved in `wwwroot`.

### 3. Configuration Data

Configuration is currently stored in:

- [appsettings.json](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/appsettings.json)
- [appsettings.Development.json](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/appsettings.Development.json)
- [launchSettings.json](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Properties/launchSettings.json)

Current database connection:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=communitysystem.db"
}
```

## Current Data Capture Model

### Accounts

Stored through ASP.NET Core Identity:

- email / username
- hashed password
- role membership
- login/session state

Relevant files:

- [ApplicationUser.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Models/ApplicationUser.cs)
- [AccountController.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Controllers/AccountController.cs)
- [RoleNames.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Security/RoleNames.cs)


## Development Seed Data

The application currently seeds development users and content from:

- [SeedData.cs](/Users/dexteryap/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/SeedData.cs)

Current development seed accounts:

- Admin: `admin@communitysystem.local` / `Admin123`
- User: `user@communitysystem.local` / `User123`

These are for local development only and must not be kept for production.

## How The App Starts

On startup, the app currently:

1. builds the ASP.NET Core application
2. connects to SQLite
3. ensures schema compatibility through `DatabaseInitializer`
4. seeds roles, users, and sample content through `SeedData`
5. starts the MVC application

## Production Preparation Checklist

Before deploying to production, complete the following in order.

### 1. Replace Development Credentials

- Remove hardcoded development seed passwords from [SeedData.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/SeedData.cs)
- Do not seed default admin/user passwords in production
- Create the first admin user through a secure provisioning process

### 2. Move Secrets Out Of Source Code

- Move connection strings to secure environment-specific configuration
- Use environment variables, secret stores, or deployment platform secrets
- Do not keep production credentials in `appsettings.json`

### 3. Replace SQLite If Needed

- Decide whether SQLite is acceptable for production traffic
- If not, move to SQL Server, PostgreSQL, or another managed relational database
- Add proper EF Core migrations instead of relying on runtime schema patching only

### 4. Introduce Proper EF Core Migrations

- Create formal migrations for:
  - Identity tables
  - posts
  - comments
  - likes
  - image path columns
- Stop depending only on manual schema adjustment in [DatabaseInitializer.cs](/Documents/dotNET/Community%20Services%20Portal/CommunitySystem/Data/DatabaseInitializer.cs)

### 5. Harden File Uploads

- Validate allowed image types explicitly
- Validate max upload size
- Sanitize file names and metadata
- Consider virus scanning if required by policy
- Move uploaded files to durable storage in production
  - example: cloud blob storage or mounted persistent volume

### 6. Review Access Control

- Reconfirm role rules for `Admin` and `User`
- Decide whether public users should see post previews only or more
- Add audit logging for content changes if needed

### 7. Enable Security Controls

- enforce HTTPS
- configure secure cookies
- set production `AllowedHosts`
- review anti-forgery coverage
- review password policy
- enable account lockout if required
- add email verification / password reset if needed

### 8. Add Logging And Monitoring

- configure structured application logging
- capture application exceptions centrally
- monitor failed login attempts
- monitor upload failures
- monitor database health and disk usage

### 9. Add Backup And Recovery Planning

- define database backup strategy
- define uploaded image backup strategy
- document restore steps
- test recovery before go-live

### 10. Clean Seed And Test Data

- remove sample posts/comments if not needed
- remove seeded likes used for testing
- verify no development-only content remains in production

### 11. Review UI / UX For Production

- add validation messages for image upload limits
- add empty-state handling for missing images
- add friendly error handling for unauthorized actions
- confirm mobile layout across the main pages

### 12. Add Operational Documentation

- document deployment steps
- document rollback steps
- document how to create admins
- document how to rotate credentials
- document where production uploads are stored

## Recommended Next Improvements

Recommended follow-up work after the first production-ready pass:

- create formal migrations
- move seeded credentials into secure provisioning
- add profile / account management pages
- add image deletion / replacement controls on edit
- add moderation and audit history
- add pagination and search
- add automated tests

## Local Run

From the app folder:

```bash
cd ".../Community Services Portal/CommunitySystem"
dotnet run
```

Build check:

```bash
dotnet build
```

