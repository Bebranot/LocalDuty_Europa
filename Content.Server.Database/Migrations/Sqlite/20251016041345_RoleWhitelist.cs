using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class RoleWhitelist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DROP TABLE IF EXISTS role_whitelists;
    ");

            migrationBuilder.Sql(@"
        CREATE TABLE IF NOT EXISTS role_whitelist (
            role_whitelist_id INTEGER PRIMARY KEY AUTOINCREMENT,
            player_id TEXT NOT NULL,
            first_time_added TEXT NOT NULL,
            first_time_added_by TEXT NOT NULL,
            how_many_times_added INTEGER NOT NULL DEFAULT 0,
            in_whitelist INTEGER NOT NULL DEFAULT 0,
            last_time_added TEXT NOT NULL,
            last_time_added_by TEXT NOT NULL,
            last_time_removed TEXT NULL,
            last_time_removed_by TEXT NULL,
            FOREIGN KEY (player_id) REFERENCES player (user_id) ON DELETE CASCADE
        );
    ");

            migrationBuilder.Sql(@"
        CREATE TABLE IF NOT EXISTS role_whitelist_log (
            role_whitelist_log_id INTEGER PRIMARY KEY AUTOINCREMENT,
            admin_id TEXT NOT NULL,
            player_id TEXT NOT NULL,
            role_whitelist_action TEXT NOT NULL,
            time TEXT NOT NULL
        );
    ");

            migrationBuilder.Sql(@"
        CREATE UNIQUE INDEX IF NOT EXISTS ""IX_role_whitelist_player_id"" ON role_whitelist (player_id);
        CREATE INDEX IF NOT EXISTS ""IX_role_whitelist_log_admin_id"" ON role_whitelist_log (admin_id);
        CREATE INDEX IF NOT EXISTS ""IX_role_whitelist_log_player_id"" ON role_whitelist_log (player_id);
    ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        DROP TABLE IF EXISTS role_whitelist_log;
        DROP TABLE IF EXISTS role_whitelist;
    ");

            migrationBuilder.Sql(@"
        CREATE TABLE IF NOT EXISTS role_whitelists (
            player_user_id TEXT NOT NULL,
            role_id TEXT NOT NULL,
            PRIMARY KEY (player_user_id, role_id),
            FOREIGN KEY (player_user_id) REFERENCES player (user_id) ON DELETE CASCADE
        );
    ");
        }
    }
}
