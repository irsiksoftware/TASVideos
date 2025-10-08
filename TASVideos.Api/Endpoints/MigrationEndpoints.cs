using Microsoft.AspNetCore.Mvc;
using TASVideos.Data;
using TASVideos.Data.Migrations;

namespace TASVideos.Api;

internal static class MigrationEndpoints
{
	public static IApplicationBuilder MapMigrations(this WebApplication app)
	{
		var group = app.MapGroup("/api/migration")
			.WithTags("Migration")
			.RequireAuthorization();

		group.MapPost("/submissions", MigrateSubmissionFiles)
			.WithName("MigrateSubmissionFiles")
			.WithSummary("Migrates submission movie files to the new MovieFiles table");

		group.MapPost("/publications", MigratePublicationFiles)
			.WithName("MigratePublicationFiles")
			.WithSummary("Migrates publication movie files to the new MovieFiles table");

		group.MapPost("/publication-files", MigratePublicationFileRecords)
			.WithName("MigratePublicationFileRecords")
			.WithSummary("Migrates publication_files movie files to the new MovieFiles table");

		group.MapPost("/all", MigrateAll)
			.WithName("MigrateAll")
			.WithSummary("Migrates all movie files to the new MovieFiles table");

		return app;
	}

	private static async Task<IResult> MigrateSubmissionFiles([FromServices] ApplicationDbContext db)
	{
		try
		{
			var migrator = new DataMigrator(db);
			await migrator.MigrateSubmissionFiles();
			return Results.Ok(new { message = "Submission files migrated successfully" });
		}
		catch (Exception ex)
		{
			return Results.Problem(
				detail: ex.Message,
				statusCode: 500,
				title: "Migration failed");
		}
	}

	private static async Task<IResult> MigratePublicationFiles([FromServices] ApplicationDbContext db)
	{
		try
		{
			var migrator = new DataMigrator(db);
			await migrator.MigratePublicationFiles();
			return Results.Ok(new { message = "Publication files migrated successfully" });
		}
		catch (Exception ex)
		{
			return Results.Problem(
				detail: ex.Message,
				statusCode: 500,
				title: "Migration failed");
		}
	}

	private static async Task<IResult> MigratePublicationFileRecords([FromServices] ApplicationDbContext db)
	{
		try
		{
			var migrator = new DataMigrator(db);
			await migrator.MigratePublicationFileRecords();
			return Results.Ok(new { message = "Publication file records migrated successfully" });
		}
		catch (Exception ex)
		{
			return Results.Problem(
				detail: ex.Message,
				statusCode: 500,
				title: "Migration failed");
		}
	}

	private static async Task<IResult> MigrateAll([FromServices] ApplicationDbContext db)
	{
		try
		{
			var migrator = new DataMigrator(db);
			await migrator.MigrateSubmissionFiles();
			await migrator.MigratePublicationFiles();
			await migrator.MigratePublicationFileRecords();
			return Results.Ok(new { message = "All movie files migrated successfully" });
		}
		catch (Exception ex)
		{
			return Results.Problem(
				detail: ex.Message,
				statusCode: 500,
				title: "Migration failed");
		}
	}
}
