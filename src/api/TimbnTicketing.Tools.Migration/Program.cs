using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using TimbnTicketing.Core.Entities;
using TimbnTicketing.Infrastructure.Data;
using TimbnTicketing.Tools.Migration;

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
var outputPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "TimbnTicketing.Infrastructure", "Data", "kcgo-users.json");

if (args.Length == 0 || args[0] is not ("export" or "import"))
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  export <mysql-connection-string>      Read KCGameOn MySQL DB → kcgo-users.json");
    Console.WriteLine("  import <sqlserver-connection-string>   Import kcgo-users.json → Timbn SQL Server");
    return;
}

var command = args[0];
var connectionString = args.Length > 1 ? args[1] : null;

if (connectionString is null)
{
    Console.Error.WriteLine("Connection string is required.");
    return;
}

if (command == "export")
{
    await ExportAsync(connectionString);
}
else if (command == "import")
{
    await ImportAsync(connectionString);
}

async Task ExportAsync(string mysqlConnectionString)
{
    Console.WriteLine("Connecting to KCGameOn MySQL database...");

    var users = new List<KcgoUser>();

    await using var connection = new MySqlConnection(mysqlConnectionString);
    await connection.OpenAsync();

    // Only export users who have at least one order
    var sql = """
        SELECT DISTINCT
            ua.ID,
            ua.Username,
            ua.FirstName,
            ua.LastName,
            ua.Email,
            ua.Submission_Date
        FROM useraccount ua
        INNER JOIN payTable pt ON pt.userName = ua.Username
        """;

    await using var cmd = new MySqlCommand(sql, connection);
    await using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        users.Add(new KcgoUser
        {
            KcgoId = reader.GetInt32("ID"),
            Username = reader.GetString("Username"),
            FirstName = GetStringOrEmpty(reader, "FirstName"),
            LastName = GetStringOrEmpty(reader, "LastName"),
            Email = GetStringOrEmpty(reader, "Email"),
            CreatedAt = reader.IsDBNull(reader.GetOrdinal("Submission_Date"))
                ? DateTimeOffset.UnixEpoch
                : new DateTimeOffset(reader.GetDateTime("Submission_Date"), TimeSpan.Zero),
        });
    }

    var fullPath = Path.GetFullPath(outputPath);
    await File.WriteAllTextAsync(fullPath, JsonSerializer.Serialize(users, jsonOptions));

    Console.WriteLine($"Exported {users.Count} users to {fullPath}");
}

async Task ImportAsync(string sqlServerConnectionString)
{
    var fullPath = Path.GetFullPath(outputPath);

    if (!File.Exists(fullPath))
    {
        Console.Error.WriteLine($"File not found: {fullPath}");
        Console.Error.WriteLine("Run 'export' first to generate the JSON file.");
        return;
    }

    var json = await File.ReadAllTextAsync(fullPath);
    var users = JsonSerializer.Deserialize<List<KcgoUser>>(json, jsonOptions) ?? [];

    Console.WriteLine($"Loaded {users.Count} users from {fullPath}");

    var dbOptions = new DbContextOptionsBuilder<PlatformDbContext>()
        .UseSqlServer(sqlServerConnectionString)
        .Options;

    await using var db = new PlatformDbContext(dbOptions);

    // Find the KCGameOn org
    var org = await db.Organizations.FirstOrDefaultAsync(o => o.Slug == "kcgameon");
    if (org is null)
    {
        Console.Error.WriteLine("KCGameOn organization not found. Seed the database first.");
        return;
    }

    // Get or create the default role for imported members
    var defaultRole = await db.Roles.FirstOrDefaultAsync(r => r.OrganizationId == org.Id && r.IsDefault);
    if (defaultRole is null)
    {
        Console.Error.WriteLine("No default role found for KCGameOn org. Create one first.");
        return;
    }

    // Define metadata fields
    var metadataFields = new Dictionary<string, string>
    {
        ["legacyUsername"] = "Legacy Username",
        ["username"] = "Username"
    };

    // Ensure metadata field definitions exist
    var existingFields = await db.UserOrganizationMetadataInfo
        .Where(m => m.OrganizationId == org.Id)
        .ToListAsync();

    foreach (var (name, label) in metadataFields)
    {
        if (existingFields.Any(f => f.MetadataName == name))
            continue;

        var isPublic = name is "legacyUsername" or "username";

        db.UserOrganizationMetadataInfo.Add(new UserOrganizationMetadataInfo
        {
            OrganizationId = org.Id,
            MetadataName = name,
            DisplayLabel = label,
            IsPublic = isPublic,
            IsRequired = false,
        });
    }

    await db.SaveChangesAsync();

    // Reload metadata fields to get IDs
    var fieldLookup = await db.UserOrganizationMetadataInfo
        .Where(m => m.OrganizationId == org.Id)
        .ToDictionaryAsync(m => m.MetadataName, m => m.Id);

    // Get existing emails to avoid duplicates
    var existingEmails = (await db.Users
        .Select(u => u.Email.ToLower())
        .ToListAsync())
        .ToHashSet();

    var imported = 0;
    var skipped = 0;

    foreach (var kcgoUser in users)
    {
        var email = kcgoUser.Email.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(email) || existingEmails.Contains(email))
        {
            skipped++;
            continue;
        }

        existingEmails.Add(email);

        var user = new User
        {
            AuthProviderId = $"kcgo-migrated-{kcgoUser.KcgoId}",
            Email = kcgoUser.Email.Trim(),
            FirstName = string.IsNullOrWhiteSpace(kcgoUser.FirstName) ? "Unknown" : kcgoUser.FirstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(kcgoUser.LastName) ? "Unknown" : kcgoUser.LastName.Trim(),
        };

        db.Users.Add(user);

        // Create org membership
        db.UserOrganizations.Add(new UserOrganization
        {
            UserId = user.Id,
            OrganizationId = org.Id,
            RoleId = defaultRole.Id,
        });

        // Add metadata values
        AddMetadata(db, user.Id, org.Id, fieldLookup, "legacyUsername", kcgoUser.Username);

        imported++;

        // Batch save every 100 users
        if (imported % 100 == 0)
        {
            await db.SaveChangesAsync();
            Console.WriteLine($"  Imported {imported} users...");
        }
    }

    await db.SaveChangesAsync();
    Console.WriteLine($"Done. Imported {imported} users, skipped {skipped} (empty email or duplicate).");
}

static void AddMetadata(PlatformDbContext db, Guid userId, Guid orgId, Dictionary<string, Guid> fieldLookup, string fieldName, string? value)
{
    if (string.IsNullOrWhiteSpace(value))
        return;

    if (!fieldLookup.TryGetValue(fieldName, out var metadataInfoId))
        return;

    db.UserOrganizationMetadataValues.Add(new UserOrganizationMetadataValue
    {
        UserId = userId,
        OrganizationId = orgId,
        MetadataInfoId = metadataInfoId,
        MetadataValue = value.Trim(),
    });
}

static string GetStringOrEmpty(MySqlDataReader reader, string column)
{
    var ordinal = reader.GetOrdinal(column);
    return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
}

static string? GetNullableString(MySqlDataReader reader, string column)
{
    var ordinal = reader.GetOrdinal(column);
    if (reader.IsDBNull(ordinal)) return null;
    var value = reader.GetString(ordinal).Trim();
    return string.IsNullOrWhiteSpace(value) ? null : value;
}
