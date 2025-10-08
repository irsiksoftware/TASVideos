using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TASVideos.Data.Migrations;

/// <inheritdoc />
public partial class RemoveFourGameTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "game_goals");
        migrationBuilder.DropTable(name: "game_game_groups");
        migrationBuilder.DropTable(name: "game_genres");
        migrationBuilder.DropTable(name: "games");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Tables removed, migration is not reversible
    }
}
