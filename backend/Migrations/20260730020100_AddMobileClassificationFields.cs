#pragma warning disable CS1591
// <copyright file="20260730020100_AddMobileClassificationFields.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddMobileClassificationFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NeedsClassification",
                table: "JournalEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "JournalEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileNote",
                table: "JournalEntries",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NeedsClassification",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "JournalEntries");

            migrationBuilder.DropColumn(
                name: "MobileNote",
                table: "JournalEntries");
        }
    }
}
