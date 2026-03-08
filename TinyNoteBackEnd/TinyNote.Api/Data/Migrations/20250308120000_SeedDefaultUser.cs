using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyNote.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO users (""Id"", ""UserName"", ""Email"", ""PasswordHash"", ""CreatedAt"")
                VALUES (
                    'd44dc55f-e08c-4db2-a918-3093f1e11848'::uuid,
                    'default_user',
                    'default@example.com',
                    '$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy',
                    NOW()
                )
                ON CONFLICT (""Id"") DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"DELETE FROM users WHERE ""Id"" = 'd44dc55f-e08c-4db2-a918-3093f1e11848'::uuid;");
        }
    }
}
