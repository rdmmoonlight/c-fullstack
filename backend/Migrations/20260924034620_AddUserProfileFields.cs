#pragma warning disable CS1591
// <copyright file="20260924034620_AddUserProfileFields.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>

#nullable disable

namespace AumoBackend.Migrations
{
    using Microsoft.EntityFrameworkCore.Migrations;

    public partial class AddUserProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop Index secara aman (IF EXISTS)
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_JournalEntries_ReferenceNumber\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_ChartOfAccounts_ReferenceNumber\";");

            // 2. Drop Column secara aman (IF EXISTS)
            migrationBuilder.Sql("ALTER TABLE \"LoginActivities\" DROP COLUMN IF EXISTS \"Description\";");
            migrationBuilder.Sql("ALTER TABLE \"JournalEntries\" DROP COLUMN IF EXISTS \"MobileNote\";");
            migrationBuilder.Sql("ALTER TABLE \"JournalEntries\" DROP COLUMN IF EXISTS \"NeedsClassification\";");
            migrationBuilder.Sql("ALTER TABLE \"JournalEntries\" DROP COLUMN IF EXISTS \"Source\";");

            // 3. Rename Column secara aman
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF EXISTS (
                        SELECT 1 
                        FROM information_schema.columns 
                        WHERE table_name = 'JournalEntries' 
                          AND column_name = 'ReferenceNumber'
                    ) THEN 
                        ALTER TABLE ""JournalEntries"" RENAME COLUMN ""ReferenceNumber"" TO ""TransactionNumber"";
                    END IF;
                END $$;
            ");

            // 4. Alter Columns UserSessions & LoginActivities
            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "RefreshTokenHash",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "OperatingSystem",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.AlterColumn<string>(
                name: "DeviceName",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Browser",
                table: "UserSessions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "UserAgent",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "OperatingSystem",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "IpAddress",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(45)",
                oldMaxLength: 45);

            migrationBuilder.AlterColumn<string>(
                name: "Device",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Country",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Browser",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "LoginActivities",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "JournalType",
                table: "JournalEntries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // 5. Tambah Kolom UserId secara aman (hanya jika belum ada)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'JournalEntries' AND column_name = 'UserId'
                    ) THEN 
                        ALTER TABLE ""JournalEntries"" ADD COLUMN ""UserId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'ChartOfAccounts' AND column_name = 'UserId'
                    ) THEN 
                        ALTER TABLE ""ChartOfAccounts"" ADD COLUMN ""UserId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'AspNetUsers' AND column_name = 'AvatarUrl'
                    ) THEN 
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""AvatarUrl"" text NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'AspNetUsers' AND column_name = 'Bio'
                    ) THEN 
                        ALTER TABLE ""AspNetUsers"" ADD COLUMN ""Bio"" text NULL;
                    END IF;
                END $$;
            ");

            // 6. Create Table secara aman (CREATE TABLE IF NOT EXISTS)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""DataProtectionKeys"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""FriendlyName"" text NULL,
                    ""Xml"" text NULL,
                    CONSTRAINT ""PK_DataProtectionKeys"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""Folders"" (
                    ""Id"" uuid NOT NULL,
                    ""UserId"" uuid NOT NULL,
                    ""Name"" character varying(150) NOT NULL,
                    ""ParentFolderId"" uuid NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Folders"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Folders_Folders_ParentFolderId"" FOREIGN KEY (""ParentFolderId"") REFERENCES ""Folders"" (""Id"") ON DELETE CASCADE
                );

                CREATE TABLE IF NOT EXISTS ""Periods"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""UserId"" uuid NOT NULL,
                    ""PeriodName"" character varying(100) NOT NULL,
                    ""StartDate"" timestamp with time zone NOT NULL,
                    ""EndDate"" timestamp with time zone NOT NULL,
                    ""IsClosed"" boolean NOT NULL,
                    ""IsSelected"" boolean NOT NULL,
                    CONSTRAINT ""PK_Periods"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""TransactionCounters"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""UserId"" uuid NOT NULL,
                    ""CounterKey"" character varying(10) NOT NULL,
                    ""LastSequence"" integer NOT NULL,
                    CONSTRAINT ""PK_TransactionCounters"" PRIMARY KEY (""Id"")
                );

                CREATE TABLE IF NOT EXISTS ""EconomicDocuments"" (
                    ""Id"" integer GENERATED BY DEFAULT AS IDENTITY,
                    ""UserId"" uuid NOT NULL,
                    ""Title"" character varying(200) NOT NULL,
                    ""Category"" character varying(50) NOT NULL,
                    ""ReferenceNumber"" character varying(100) NULL,
                    ""JournalEntryId"" integer NULL,
                    ""FolderId"" uuid NULL,
                    ""FileName"" character varying(255) NOT NULL,
                    ""FilePath"" character varying(500) NOT NULL,
                    ""CloudPublicId"" character varying(150) NULL,
                    ""FileSize"" bigint NOT NULL,
                    ""ContentType"" character varying(100) NULL,
                    ""UploadedBy"" text NOT NULL,
                    ""UploadDate"" timestamp with time zone NOT NULL,
                    ""Description"" text NULL,
                    CONSTRAINT ""PK_EconomicDocuments"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_EconomicDocuments_Folders_FolderId"" FOREIGN KEY (""FolderId"") REFERENCES ""Folders"" (""Id"") ON DELETE CASCADE,
                    CONSTRAINT ""FK_EconomicDocuments_JournalEntries_JournalEntryId"" FOREIGN KEY (""JournalEntryId"") REFERENCES ""JournalEntries"" (""Id"")
                );
            ");

            // 7. Buat Index Baru secara aman
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_JournalEntries_UserId_TransactionNumber\" ON \"JournalEntries\" (\"UserId\", \"TransactionNumber\");");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_ChartOfAccounts_UserId_ReferenceNumber\" ON \"ChartOfAccounts\" (\"UserId\", \"ReferenceNumber\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EconomicDocuments_Category\" ON \"EconomicDocuments\" (\"Category\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EconomicDocuments_FolderId\" ON \"EconomicDocuments\" (\"FolderId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EconomicDocuments_JournalEntryId\" ON \"EconomicDocuments\" (\"JournalEntryId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EconomicDocuments_ReferenceNumber\" ON \"EconomicDocuments\" (\"ReferenceNumber\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_EconomicDocuments_UserId\" ON \"EconomicDocuments\" (\"UserId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Folders_ParentFolderId\" ON \"Folders\" (\"ParentFolderId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Folders_UserId\" ON \"Folders\" (\"UserId\");");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS \"IX_TransactionCounters_UserId_CounterKey\" ON \"TransactionCounters\" (\"UserId\", \"CounterKey\");");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
