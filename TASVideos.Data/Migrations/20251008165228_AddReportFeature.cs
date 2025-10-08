using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TASVideos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    forum_post_id = table.Column<int>(type: "integer", nullable: false),
                    reporter_id = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "citext", nullable: false),
                    reported_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    resolved = table.Column<bool>(type: "boolean", nullable: false),
                    resolved_by_user_id = table.Column<int>(type: "integer", nullable: true),
                    resolved_on = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    resolution = table.Column<string>(type: "citext", nullable: true),
                    create_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reports", x => x.id);
                    table.ForeignKey(
                        name: "fk_reports_forum_posts_forum_post_id",
                        column: x => x.forum_post_id,
                        principalTable: "forum_posts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reports_users_reporter_id",
                        column: x => x.reporter_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reports_users_resolved_by_user_id",
                        column: x => x.resolved_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reports_forum_post_id",
                table: "reports",
                column: "forum_post_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reported_on",
                table: "reports",
                column: "reported_on");

            migrationBuilder.CreateIndex(
                name: "ix_reports_reporter_id",
                table: "reports",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "ix_reports_resolved",
                table: "reports",
                column: "resolved");

            migrationBuilder.CreateIndex(
                name: "ix_reports_resolved_by_user_id",
                table: "reports",
                column: "resolved_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reports");
        }
    }
}
