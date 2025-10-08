using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TASVideos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "citext", nullable: false),
                    submission_id = table.Column<int>(type: "integer", nullable: false),
                    additional_authors = table.Column<string>(type: "citext", nullable: true),
                    frames = table.Column<int>(type: "integer", nullable: false),
                    rerecord_count = table.Column<int>(type: "integer", nullable: false),
                    system_frame_rate_id = table.Column<int>(type: "integer", nullable: true),
                    emulator_version = table.Column<string>(type: "citext", nullable: true),
                    event_name = table.Column<string>(type: "citext", nullable: false),
                    event_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    create_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_events_game_system_frame_rates_system_frame_rate_id",
                        column: x => x.system_frame_rate_id,
                        principalTable: "game_system_frame_rates",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_events_submissions_submission_id",
                        column: x => x.submission_id,
                        principalTable: "submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_authors",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_authors", x => new { x.user_id, x.event_id });
                    table.ForeignKey(
                        name: "fk_event_authors_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_authors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_files",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    path = table.Column<string>(type: "citext", nullable: false),
                    type = table.Column<string>(type: "citext", nullable: false),
                    description = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_files", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_files_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_flags",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    flag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_flags", x => new { x.event_id, x.flag_id });
                    table.ForeignKey(
                        name: "fk_event_flags_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_flags_flags_flag_id",
                        column: x => x.flag_id,
                        principalTable: "flags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_tags",
                columns: table => new
                {
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_tags", x => new { x.event_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_event_tags_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "event_urls",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    event_id = table.Column<int>(type: "integer", nullable: false),
                    url = table.Column<string>(type: "citext", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    display_name = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_urls", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_urls_events_event_id",
                        column: x => x.event_id,
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_authors_event_id",
                table: "event_authors",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_files_event_id",
                table: "event_files",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_flags_event_id",
                table: "event_flags",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_flags_flag_id",
                table: "event_flags",
                column: "flag_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_event_id",
                table: "event_tags",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_tags_tag_id",
                table: "event_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_urls_event_id",
                table: "event_urls",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_urls_type",
                table: "event_urls",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "ix_events_submission_id",
                table: "events",
                column: "submission_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_system_frame_rate_id",
                table: "events",
                column: "system_frame_rate_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_authors");

            migrationBuilder.DropTable(
                name: "event_files");

            migrationBuilder.DropTable(
                name: "event_flags");

            migrationBuilder.DropTable(
                name: "event_tags");

            migrationBuilder.DropTable(
                name: "event_urls");

            migrationBuilder.DropTable(
                name: "events");
        }
    }
}
