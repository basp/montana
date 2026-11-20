using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Montana.Migrations;

/// <inheritdoc />
public partial class _20261120190443_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projects",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Slug = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, collation: "NOCASE"),
                ColorHex = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Projects", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Slug = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE"),
                ColorHex = table.Column<string>(type: "TEXT", maxLength: 9, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TimeEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Description = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                EndedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                DurationSeconds = table.Column<long>(type: "INTEGER", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ProjectId = table.Column<Guid>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeEntries", x => x.Id);
                table.CheckConstraint("CK_TimeEntries_EndedAt_Not_Before_StartedAt", "\"EndedAt\" IS NULL OR \"EndedAt\" >= \"StartedAt\"");
                table.ForeignKey(
                    name: "FK_TimeEntries_Projects_ProjectId",
                    column: x => x.ProjectId,
                    principalTable: "Projects",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "TimeEntryTags",
            columns: table => new
            {
                TagsId = table.Column<Guid>(type: "TEXT", nullable: false),
                TimeEntriesId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TimeEntryTags", x => new { x.TagsId, x.TimeEntriesId });
                table.ForeignKey(
                    name: "FK_TimeEntryTags_Tags_TagsId",
                    column: x => x.TagsId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_TimeEntryTags_TimeEntries_TimeEntriesId",
                    column: x => x.TimeEntriesId,
                    principalTable: "TimeEntries",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Projects_Slug",
            table: "Projects",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Slug",
            table: "Tags",
            column: "Slug",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_ProjectId",
            table: "TimeEntries",
            column: "ProjectId");

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntries_StartedAt",
            table: "TimeEntries",
            column: "StartedAt");

        migrationBuilder.CreateIndex(
            name: "IX_TimeEntryTags_TimeEntriesId",
            table: "TimeEntryTags",
            column: "TimeEntriesId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TimeEntryTags");

        migrationBuilder.DropTable(
            name: "Tags");

        migrationBuilder.DropTable(
            name: "TimeEntries");

        migrationBuilder.DropTable(
            name: "Projects");
    }
}
