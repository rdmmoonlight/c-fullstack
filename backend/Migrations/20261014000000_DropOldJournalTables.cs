#pragma warning disable CS1591
// <copyright file="20261014000000_DropOldJournalTables.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class DropOldJournalTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tabel JournalEntries dan JournalEntryLines dihapus manual lewat Neon
            // SQL Editor (lihat Migrations/manual-neon-run-drop-old-journal-tables.sql),
            // jadi Up() di sini dibiarkan kosong agar tidak error saat aplikasi start.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
