#pragma warning disable CS1591
// <copyright file="20260730020000_AddUnclassifiedAccounts.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddUnclassifiedAccounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only migration: memastikan dua akun sistem "Unclassified"
            // tersedia untuk menampung transaksi mobile sebelum diklasifikasikan.
            // Idempoten: aman dijalankan berkali-kali / di database yang sudah
            // memiliki akun ini secara manual.
            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""ReferenceNumber"", ""AccountName"", ""Type"", ""Role"", ""IsActive"")
                SELECT 499, 'Unclassified Income', 'OperatingIncome', 'UnclassifiedIncome', TRUE
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""ChartOfAccounts"" WHERE ""ReferenceNumber"" = 499
                );
            ");

            migrationBuilder.Sql(@"
                INSERT INTO ""ChartOfAccounts"" (""ReferenceNumber"", ""AccountName"", ""Type"", ""Role"", ""IsActive"")
                SELECT 599, 'Unclassified Expense', 'OperatingExpenses', 'UnclassifiedExpense', TRUE
                WHERE NOT EXISTS (
                    SELECT 1 FROM ""ChartOfAccounts"" WHERE ""ReferenceNumber"" = 599
                );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""ChartOfAccounts""
                WHERE ""Role"" IN ('UnclassifiedIncome', 'UnclassifiedExpense');
            ");
        }
    }
}
