# Fidelitas Hub - Client SOP Versioning

## What was added

The SOP module is integrated directly into **Masters -> Clients -> Edit Client**.

For every client the Edit Client page now provides:

- Upload New SOP Version
- Automatic version numbering (Version 1, Version 2, Version 3, ...)
- Automatic document display name based on the client name
- Uploaded By
- Uploaded Date and Time
- Current / Archived status
- Full version history
- View action for every version

## Productivity integration

The **Productivity -> Client -> SOP -> View SOP** action now opens the current SOP version automatically.

## File storage

Uploaded files are stored outside `wwwroot` under:

`App_Data/SOP/<ClientId>/`

They are served through the `ClientController.ViewSop` action rather than as public static files.

## Supported format

- PDF only
- Maximum size: 25 MB
- The application checks both the `.pdf` extension and the `%PDF-` file signature.

## Database update

Run this SQL script against the Fidelitas Hub database before deploying the new application:

`Database/20260904_AddClientSopDocuments.sql`

An EF Core migration is also included:

`Backend/Data/Migrations/20260904010000_AddClientSopDocuments.cs`

Do not run both database mechanisms in a way that attempts to create the same table twice. If using the SQL script manually, the script is idempotent for table creation. If your deployment process uses EF migrations, use the EF migration path instead.

## First MHP upload

Open:

`Masters -> Clients -> MHP -> Edit`

Upload the MHP SOP PDF. The system will automatically create **Version 1**.
