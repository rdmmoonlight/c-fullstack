#pragma warning disable CS1591
// <copyright file="20260809100000_RenameJournalEntryReferenceNumberToTransactionNumber.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class RenameJournalEntryReferenceNumberToTransactionNumber : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Kolom JournalEntries.ReferenceNumber di-rename ke TransactionNumber
            // secara manual lewat Neon SQL Editor (lihat
            // Migrations/manual-neon-run-transaction-number.sql), jadi Up() di sini
            // dibiarkan kosong agar tidak error/duplikat saat aplikasi start.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
