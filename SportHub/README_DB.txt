# SPORTHUB DATABASE SETUP & INFORMATION

## 1. Database Overview

SportHub uses Microsoft SQL Server Express LocalDB as its local development database.

The application uses Entity Framework Core to manage database operations, relationships and migrations.

**Database Engine:** Microsoft SQL Server Express LocalDB  
**LocalDB Instance:** `(localdb)\MSSQLLocalDB`  
**Database Name:** `SportHubDb`  
**Database File:** `SportHub.mdf`  
**Application Framework:** ASP.NET Core MVC (.NET 10)  
**ORM:** Entity Framework Core

## 2. Important Database Information

The application is configured to use a local database file named `SportHub.mdf`, located in the application's project directory.

The public GitHub repository does not include the original `SportHub.mdf` or `SportHub_log.ldf` files.

These files are excluded to avoid publishing local database records and development data.

The source code includes Entity Framework Core migrations, which define the database structure.

Before running the application, developers must ensure that SQL Server Express LocalDB is installed and the database configuration is valid.

**Important:** The included migrations provide the database structure, but successful database creation from a fresh checkout has not been independently verified. Additional local database setup may be required.

## 3. Database Configuration

The database connection is defined in `Program.cs`.

The application constructs the database file path from its runtime content root directory and uses SQL Server LocalDB with the following configuration:

- Data Source: `(LocalDB)\MSSQLLocalDB`
- Initial Catalog: `SportHubDb`
- AttachDbFilename: Path to `SportHub.mdf`
- Integrated Security: Enabled
- Multiple Active Result Sets: Enabled
- TrustServerCertificate: Enabled

The database connection must be available before the application can complete its startup database operations.

Developers using a different SQL Server environment may need to adjust the connection configuration locally.

## 4. Main Database Tables

The SportHub database model includes the following tables:

**Users**

Stores registered user account information, account roles and security-related status.

**LoginAttempts**

Supports tracking and management of unsuccessful login attempts.

**PasswordResetTokens**

Stores token information used for password recovery.

**EmailVerificationTokens**

Supports email verification for registered accounts.

**FacilityCategories**

Stores different sports facility categories.

**Facilities**

Stores facility information such as name, type, location, price and capacity.

**FacilityImages**

Stores information associated with sports facility images.

**TimeSlots**

Stores available reservation time slots.

**Reservations**

Stores sports facility reservation records.

**Payments**

Stores records associated with the academic simulated payment process.

**MaintenanceBlocks**

Represents maintenance or unavailable periods for facilities.

**HeroSlides**

Stores homepage banner information.

**__EFMigrationsHistory**

Tracks Entity Framework Core migrations that have been applied to the database.

## 5. Entity Framework Core Migrations

The project includes an Entity Framework Core `Migrations` directory.

In the Development environment, `Program.cs` executes:

```csharp
await dbContext.Database.MigrateAsync();
```

This operation attempts to apply pending Entity Framework Core migrations.

The application does not intentionally delete and recreate the entire database during each startup.

However, migration execution still requires a valid database connection and appropriate database permissions.

If migration or database attachment errors occur, the LocalDB configuration and database file location should be checked before continuing.

## 6. Demonstration Data Initialization

After the migration step, `Program.cs` calls:

```csharp
await DbSeeder.SeedAsync(dbContext);
```

The current `DbSeeder.cs` initialises missing sports facility and time-slot demonstration data.

The initialization logic includes sample sports categories and facilities used to demonstrate the application.

The current public version does not include the previous `SeedUsersAsync()` method.

Therefore:

- Default administrator accounts are not automatically created by this seeder.
- Default demonstration member accounts are not automatically created by this seeder.
- Existing database users are not deleted by the removal of the user-seeding method.
- Public Member accounts are expected to be created through the application's registration process.
- Administrator accounts require a separate controlled setup process.

The demonstration-data routines are intended to avoid inserting duplicate records under their defined checks.

## 7. Local Database Setup Considerations

### Step 1: Install SQL Server Express LocalDB

Ensure that SQL Server Express LocalDB is available on the development computer.

### Step 2: Open the Project

Open the SportHub solution or project in Visual Studio.

### Step 3: Check the Database Connection

Review the connection configuration in `Program.cs`.

The current configuration expects a local database named `SportHubDb` associated with a file named `SportHub.mdf`.

### Step 4: Prepare the Local Database

Use an appropriate SQL Server LocalDB database creation or attachment process.

The repository does not include a ready-made copy of the original database files.

The project contains EF Core migrations to support database schema setup.

### Step 5: Apply the Database Migrations

When the project runs in the Development environment, its startup code attempts to apply pending migrations automatically.

If the operation fails, review the error message and verify the database configuration.

Do not assume migrations have completed successfully unless the application starts without database errors.

### Step 6: Verify the Tables

After successful database setup and migration, inspect the database through SQL Server Object Explorer.

## 8. Viewing the Database in Visual Studio

1. Open Microsoft Visual Studio.
2. Select `View` from the menu.
3. Open `SQL Server Object Explorer`.
4. Expand `SQL Server`.
5. Find `(localdb)\MSSQLLocalDB`.
6. Expand `Databases`.
7. Find `SportHubDb`.
8. Expand `Tables`.
9. Right-click a table and select `View Data` to inspect its records.

If `SportHubDb` is not visible, refresh the database list.

If it is still missing, confirm that database creation or attachment succeeded and that the application is connecting to the intended LocalDB instance.

## 9. Application and Database Workflow

The intended Member workflow is:

1. Browse the SportHub website.
2. Register a Member account.
3. Complete email verification.
4. Log in to the application.
5. Select a sports facility.
6. View available dates and time slots.
7. Choose the participant count.
8. Confirm a reservation.
9. Complete a simulated payment.
10. Review the reservation details.
11. Access the QR pass or available PDF receipt.

The intended Administrator workflow includes:

1. Log in using an authorised administrator account.
2. Access the administrative dashboard.
3. Manage users and sports facilities.
4. Manage facility categories and images.
5. Configure time slots and maintenance periods.
6. Review reservations and simulated payments.
7. View operational reports.

Administrative access is subject to the application's role-based authorization rules.

## 10. Database Security and Privacy

To protect application users and development credentials:

- Do not publish real user account records.
- Do not commit local database files containing personal information.
- Do not expose authentication tokens.
- Do not store SMTP passwords in public source-code files.
- Do not publish real administrator login credentials.
- Avoid hardcoding reusable account passwords.
- Do not place banking credentials in the demonstration database.
- Keep local development secrets outside the public repository.

The public repository should contain the database models, migration files and configuration guidance, rather than private local database records.

## 11. Common Database Issues

**Issue: LocalDB connection failure**

Check that SQL Server Express LocalDB is installed and that the configured instance is available.

**Issue: Database file cannot be attached**

Check the database file path, permissions and whether the database is already attached to SQL Server.

**Issue: Database tables are missing**

Verify whether Entity Framework Core migrations have completed successfully.

**Issue: Database is not visible in SQL Server Object Explorer**

Refresh the LocalDB database list and confirm that the expected instance and database name are being used.

**Issue: No default administrator account is available**

This is expected in the current public version. Automatic administrator account creation has been removed.

A separate, properly authorised administrative setup process is required.

**Issue: Email verification does not send a real email**

Check the SMTP configuration or use the application's configured development email testing mechanism.

## 12. Additional Notes

- This database configuration is intended for local development.
- The original database files are not distributed in the public repository.
- The project contains database migrations and sample facility initialization logic.
- Account registration and email verification depend on the application configuration.
- Payment records represent academic simulated payments, not real banking transactions.
- Additional configuration and testing may be required before running the project on a new development computer.
- Production deployment would require a separate review of database hosting, security and configuration.

## 13. Disclaimer

This database documentation is provided for academic, development and portfolio reference.

SportHub is not presented as a production-ready commercial reservation platform.

Developers are responsible for configuring their own local environment and protecting any account or database information used during testing.