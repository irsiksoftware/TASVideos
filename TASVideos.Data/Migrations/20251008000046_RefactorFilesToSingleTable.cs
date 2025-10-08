using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TASVideos.Data.Migrations
{
    /// <inheritdoc />
    public partial class RefactorFilesToSingleTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "movie_file_id",
                table: "submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "movie_file_id",
                table: "publications",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "compression_type",
                table: "publication_files",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "movie_file_id",
                table: "publication_files",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "movie_files",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    file_data = table.Column<byte[]>(type: "bytea", nullable: false),
                    compression_type = table.Column<int>(type: "integer", nullable: false),
                    original_length = table.Column<int>(type: "integer", nullable: false),
                    file_name = table.Column<string>(type: "citext", nullable: false),
                    create_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    last_update_timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_movie_files", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_submissions_movie_file_id",
                table: "submissions",
                column: "movie_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_publications_movie_file_id",
                table: "publications",
                column: "movie_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_publication_files_movie_file_id",
                table: "publication_files",
                column: "movie_file_id");

            migrationBuilder.AddForeignKey(
                name: "fk_publication_files_movie_files_movie_file_id",
                table: "publication_files",
                column: "movie_file_id",
                principalTable: "movie_files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_publications_movie_files_movie_file_id",
                table: "publications",
                column: "movie_file_id",
                principalTable: "movie_files",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_submissions_movie_files_movie_file_id",
                table: "submissions",
                column: "movie_file_id",
                principalTable: "movie_files",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_publication_files_movie_files_movie_file_id",
                table: "publication_files");

            migrationBuilder.DropForeignKey(
                name: "fk_publications_movie_files_movie_file_id",
                table: "publications");

            migrationBuilder.DropForeignKey(
                name: "fk_submissions_movie_files_movie_file_id",
                table: "submissions");

            migrationBuilder.DropTable(
                name: "movie_files");

            migrationBuilder.DropIndex(
                name: "ix_submissions_movie_file_id",
                table: "submissions");

            migrationBuilder.DropIndex(
                name: "ix_publications_movie_file_id",
                table: "publications");

            migrationBuilder.DropIndex(
                name: "ix_publication_files_movie_file_id",
                table: "publication_files");

            migrationBuilder.DropColumn(
                name: "movie_file_id",
                table: "submissions");

            migrationBuilder.DropColumn(
                name: "movie_file_id",
                table: "publications");

            migrationBuilder.DropColumn(
                name: "movie_file_id",
                table: "publication_files");

            migrationBuilder.AlterColumn<int>(
                name: "compression_type",
                table: "publication_files",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
