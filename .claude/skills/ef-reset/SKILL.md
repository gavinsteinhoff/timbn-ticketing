---
name: ef-reset
description: Drops and recreates the local database, re-running all migrations and seed data.
user-invocable: true
disable-model-invocation: true
---

Drop and recreate the local EF Core database, re-applying all migrations and seed data.

All commands run from `src/api/`.

## Steps

1. **Drop the database:** `dotnet-ef database drop --force --project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"`
2. **Recreate with migrations:** `dotnet-ef database update --project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"`
3. **On success**, confirm the database has been reset and seed data applied.
4. **On failure**, show the error output and suggest fixes.
