# SportHub — Sports Facility Reservation & Management System

# 1. Project Overview

SportHub is a sports facility reservation and management web application developed as an academic project using ASP.NET Core MVC.

The system is designed to provide a convenient platform for users to explore indoor and outdoor sports facilities, check available reservation slots, make bookings and manage their reservation records.

It also includes administrative features for managing sports facilities, users, reservations, maintenance schedules and operational reports.

The project focuses on web application development, responsive interface design, database integration and role-based access control.

**Project Type:** Academic Web Application  
**Development Status:** Academic Prototype / In Progress  
**Framework:** ASP.NET Core MVC (.NET 10)  
**Database:** Microsoft SQL Server Express LocalDB

# 2. Technology Stack

## Backend Development
- C#
- ASP.NET Core MVC
- Entity Framework Core
- ASP.NET Core Cookie Authentication

## Frontend Development
- HTML
- CSS
- JavaScript
- Bootstrap
- Razor Views

## Database
- Microsoft SQL Server Express LocalDB
- Entity Framework Core Migrations

## Development Tools
- Microsoft Visual Studio
- SQL Server Object Explorer
- NuGet Package Manager

## Additional Components
- QRCoder for reservation QR codes
- Custom PDF receipt generation
- SMTP-based email verification and password reset

# 3. Main System Features

## 3.1 Public Visitor Features

Public visitors can:

- View the SportHub homepage and banner carousel.
- Browse indoor and outdoor sports facilities.
- View facility information and images.
- Read information about the system, booking guidelines and safety rules.
- Register an account or log in to access reservation features.

## 3.2 Member Features

Registered members can:

- Register and activate an account through email verification.
- Log in and log out using cookie-based authentication.
- Request a password reset through email.
- View and update personal profile information.
- Upload and manage a profile image.
- Browse available sports facilities.
- View facility prices, locations, capacities and images.
- Check available reservation time slots.
- Select a reservation date, time and participant count.
- Create a reservation according to the system's booking rules.
- Use simulated payment options.
- View reservation history and booking statuses.
- Cancel eligible reservations and provide a cancellation reason.
- View reservation QR passes.
- Download reservation calendar files (.ics).
- Download available PDF e-receipts.

## 3.3 Administrator Features

Administrator functions include:

- Accessing the administrative dashboard.
- Viewing reservation and system statistics.
- Managing registered users.
- Viewing account verification and lockout information.
- Managing facility categories.
- Creating, editing and deactivating sports facilities.
- Managing facility images and cover images.
- Configuring facility prices, capacities and locations.
- Managing reservation time slots.
- Scheduling maintenance and unavailable periods.
- Managing homepage banner images.
- Reviewing and updating reservation statuses.
- Reviewing simulated payment records.
- Viewing operational reports and charts.

Administrative features require an account with the appropriate permissions. The public registration process is intended for Member accounts.

# 4. User Roles and Access Control

SportHub contains two main authenticated user roles:

**Admin**

Administrators manage operational functions, including facilities, users, reservations, simulated payments and reporting.

**Member**

Members browse facilities, make reservations, manage their profiles and review personal booking information.

Public visitors can browse public pages without signing in.

Role-based authorization is used to restrict access to protected application functions.

**Important:** The publicly shared source code does not automatically create a default administrator or demonstration member account. Administrator accounts must be configured through a controlled setup process.

# 5. Reservation and Payment Workflow

The intended reservation process is:

1. Browse available sports facilities.
2. Register or log in to a Member account.
3. Select a sports facility.
4. Review facility details and available time slots.
5. Select the preferred reservation date and time.
6. Specify the number of participants.
7. Submit the reservation.
8. Complete the simulated payment process.
9. Review the reservation status.
10. Access the reservation QR pass, calendar file or PDF receipt when available.

## Payment Simulation

SportHub includes academic payment simulations for:

- Touch 'n Go (TNG) eWallet
- DuitNow
- Online Banking

**These are simulated payment methods only.**

The application does not process real banking transactions or transfer real money. Users should not enter real banking credentials during testing.

# 6. System Requirements

To work with the source code locally, prepare the following development environment:

- Microsoft Visual Studio with ASP.NET and web development support
- .NET 10 SDK
- Microsoft SQL Server Express LocalDB
- NuGet package restore capability
- A supported web browser

The project is intended for a Windows development environment using Visual Studio and SQL Server LocalDB.

# 7. Project Setup

## Step 1: Download or Clone the Repository

Download the source code from GitHub or clone the repository into a local directory.

## Step 2: Open the Project

Open the SportHub solution or project file using Microsoft Visual Studio.

## Step 3: Verify the Development Environment

Ensure that:

- .NET 10 SDK is installed.
- SQL Server Express LocalDB is available.
- Visual Studio can restore the required NuGet packages.

## Step 4: Configure the Database

SportHub uses SQL Server Express LocalDB.

The application is configured in `Program.cs` to use a local database file named `SportHub.mdf`.

The database file is not included in the public repository.

A local database must be prepared before the application can run successfully. The project contains Entity Framework Core migrations for its database structure.

See `README_DB.txt` for database information and setup considerations.

## Step 5: Configure Email Settings

SportHub uses SMTP settings for email verification and password reset.

Sensitive email credentials must not be stored in public source-code files.

For local development, configure email settings using Visual Studio User Secrets or another secure configuration method.

The project also includes a development email outbox fallback when SMTP settings are not configured.

## Step 6: Restore and Build

Restore the required NuGet packages and build the solution.

## Step 7: Run the Application

Select the HTTPS launch profile and run the application from Visual Studio.

In the Development environment, `Program.cs` attempts to apply pending Entity Framework Core migrations and initialise missing facility and time-slot demonstration data.

Database preparation and configuration must be completed successfully before these startup operations can run.

## Step 8: Register a Member Account

Use the application's registration page to create a Member account.

Email verification may be required before logging in. Use the configured SMTP service or the local development email testing mechanism.

No default administrator or member login credentials are distributed with this repository.

# 8. Main NuGet Dependencies

The project includes the following NuGet packages:

- Microsoft.EntityFrameworkCore.Design — 10.0.8
- Microsoft.EntityFrameworkCore.SqlServer — 10.0.8
- Microsoft.EntityFrameworkCore.Tools — 10.0.8
- QRCoder — 1.8.0

NuGet packages should normally be restored automatically when building the project.

PDF receipt generation is handled by the project's custom `ReceiptPdfService`.

# 9. Email Configuration and Security

Email verification and password recovery require valid email settings or the configured development testing mechanism.

For security:

- Do not publish SMTP usernames and passwords.
- Do not upload Gmail App Passwords.
- Do not expose real user data or authentication tokens.
- Do not commit Visual Studio User Secrets.
- Avoid uploading local database files containing personal information.
- Never use real banking credentials during payment testing.

The public repository should contain source code, project assets and safe configuration examples only.

# 10. Database Information

**Database Engine:** SQL Server Express LocalDB  
**LocalDB Instance:** `(localdb)\MSSQLLocalDB`  
**Database Name:** `SportHubDb`  
**Database File:** `SportHub.mdf`

The project uses Entity Framework Core for database access and migrations.

Local database files are excluded from the public repository.

For more information about the database tables and development setup, refer to `README_DB.txt`.

# 11. Project Assets

SportHub includes assets used for its web interface, such as:

- Project logo and interface graphics
- Sports facility images
- Homepage banner images
- CSS and JavaScript resources
- Bootstrap-based interface components

Some facility images are used as demonstration content. Their inclusion in this academic prototype does not imply ownership of third-party images.

# 12. Project Status and Limitations

SportHub is an academic prototype and ongoing development project.

Although the codebase includes reservation, account management, payment simulation and administrative features, not every feature has been independently verified for production use.

Current limitations include:

- Real payment gateway integration is not provided.
- A local SQL Server database is required.
- Email functionality depends on development configuration.
- Administrator setup is not automated through default public credentials.
- Additional testing, security review and deployment configuration would be necessary before production use.

The project is provided primarily for academic demonstration, development learning and portfolio purposes.

# 13. Development Contributions

Development work included:

- Web interface layout and navigation design.
- Responsive page structure and visual presentation.
- Logo and interface prototyping.
- Customisation and refinement of application pages.
- Application code modifications and integration.
- Development troubleshooting and testing.

AI-assisted development tools were used for coding guidance, review, debugging and implementation support.

The project contains academic development work and should not be interpreted as a fully production-ready commercial application.

# 14. Disclaimer

SportHub is developed for educational purposes.

The payment features are simulations and do not represent real financial transactions.

Demonstration content and images are used to illustrate the application's interface and intended functionality.

All external assets remain subject to their respective ownership and usage rights.