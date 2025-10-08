using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TASVideos.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSubmissionFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_events_submission_id",
                table: "events");

            migrationBuilder.AddColumn<bool>(
                name: "is_event_submission",
                table: "submissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "ix_events_submission_id",
                table: "events",
                column: "submission_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_events_submission_id",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_event_submission",
                table: "submissions");

            migrationBuilder.CreateIndex(
                name: "ix_events_submission_id",
                table: "events",
                column: "submission_id");
        }
    }
}
