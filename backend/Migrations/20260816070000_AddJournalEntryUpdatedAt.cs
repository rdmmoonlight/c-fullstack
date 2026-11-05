#pragma warning disable CS1591
// <copyright file="20260816070000_AddJournalEntryUpdatedAt.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddJournalEntryUpdatedAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kolom JournalEntries.UpdatedAt ditambahkan manual lewat Neon SQL
            // Editor (lihat Migrations/manual-neon-run-journal-updatedat.sql),
            // jadi Up() di sini dibiarkan kosong agar tidak error/duplikat
            // saat aplikasi start.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
