---
name: ef-add
description: Adds a migration to the entity framework code first database. Pass the migration name as an argument (e.g. /ef-add AddVenueCapacity).
user-invocable: true
disable-model-invocation: true
---

Add an EF Core migration and update the database using the name provided in `$ARGUMENTS`.

All commands run from `src/api/` with `--project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api`.

## Steps

1. **Add migration:** `dotnet-ef migrations add $ARGUMENTS --project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"`
2. **Update database:** `dotnet-ef database update --project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"`
3. **If any step fails:**
   - Remove the failed migration: `dotnet-ef migrations remove --project ./TimbnTicketing.Infrastructure --startup-project ./TimbnTicketing.Api -- --ConnectionStrings:Ticketing="Server=(localdb)\MSSQLLocalDB;Database=TimbnTicketing;Trusted_Connection=True;TrustServerCertificate=True"`
   - Analyze the error output
   - Propose fixes to the relevant entity or configuration files
   - Ask the user to accept the fixes before applying them
   - Re-add the migration with the same name and re-run the database update
4. **On success**, show the user the generated migration file names and confirm the database is up to date
