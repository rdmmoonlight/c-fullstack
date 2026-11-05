namespace AumoBackend.Data;

using AumoBackend.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using DataProtectionKeyEntity = Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey;

public partial class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ChartOfAccount> ChartOfAccounts { get; set; } = null!;

    // Menggunakan type alias agar tidak bentrok dengan AumoBackend.Models.DataProtectionKey
    public virtual DbSet<DataProtectionKeyEntity> DataProtectionKeys { get; set; } = null!;

    public virtual DbSet<GeneralJournal> GeneralJournals { get; set; } = null!;

    public virtual DbSet<GeneralLedgerPermanentAccount> GeneralLedgerPermanentAccounts { get; set; } = null!;

    public virtual DbSet<GeneralLedgerTemporaryAccount> GeneralLedgerTemporaryAccounts { get; set; } = null!;

    public virtual DbSet<LoginActivity> LoginActivities { get; set; } = null!;

    public virtual DbSet<Notification> Notifications { get; set; } = null!;

    public virtual DbSet<Period> Periods { get; set; } = null!;

    public virtual DbSet<RecoveryCode> RecoveryCodes { get; set; } = null!;

    public virtual DbSet<SecuritySetting> SecuritySettings { get; set; } = null!;

    public virtual DbSet<TransactionCounter> TransactionCounters { get; set; } = null!;

    public virtual DbSet<TrustedDevice> TrustedDevices { get; set; } = null!;

    public virtual DbSet<UserSession> UserSessions { get; set; } = null!;

    public virtual DbSet<AdjustedTrialBalance> AdjustedTrialBalance { get; set; } = null!;

    public virtual DbSet<PostClosingTrialBalance> PostClosingTrialBalance { get; set; } = null!;

    public virtual DbSet<UnadjustedTrialBalance> UnadjustedTrialBalance { get; set; } = null!;

    public virtual DbSet<Worksheet> Worksheet { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("postgres_fdw");

        builder.Entity<ChartOfAccount>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_ChartOfAccounts_UserId");

            entity.HasIndex(e => new { e.UserId, e.ReferenceNumber }, "IX_ChartOfAccounts_UserId_ReferenceNumber").IsUnique();

            entity.Property(e => e.AccountName).HasMaxLength(100);

            // Balance dihitung di memori dari GeneralJournal (bukan kolom tabel).
            entity.Ignore(e => e.Balance);
        });

        builder.Entity<GeneralJournal>(entity =>
        {
            entity.HasKey(e => e.LineId);

            entity.ToTable("GeneralJournal");

            entity.HasIndex(e => e.AccountId, "IX_GeneralJournal_AccountId");

            entity.HasIndex(e => e.JournalEntryId, "IX_GeneralJournal_JournalEntryId");

            entity.HasIndex(e => new { e.UserId, e.EntryDate }, "IX_GeneralJournal_UserId_EntryDate");

            entity.HasIndex(e => new { e.UserId, e.TransactionNumber }, "IX_GeneralJournal_UserId_TransactionNumber");

            entity.Property(e => e.Credit).HasPrecision(18, 2);
            entity.Property(e => e.Debit).HasPrecision(18, 2);
            entity.Property(e => e.JournalType).HasMaxLength(50);
            entity.Property(e => e.LineDescription).HasMaxLength(250);
            entity.Property(e => e.LineOrder).HasDefaultValue(0);
            entity.Property(e => e.TransactionNumber).HasMaxLength(30);

            entity.HasOne(d => d.Account).WithMany(p => p.GeneralJournals)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GeneralLedgerPermanentAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GeneralLedgerPermanentAccounts_pkey");

            entity.HasIndex(e => new { e.UserId, e.PeriodId, e.AccountId, e.EntryDate, e.Id }, "IX_GLPermanent_FetchOrdering");

            entity.HasIndex(e => new { e.UserId, e.PeriodId }, "IX_GLPermanent_UserId_PeriodId");

            entity.HasIndex(e => e.JournalEntryLineId, "IX_GeneralLedgerPermanentAccounts_JournalEntryLineId");

            entity.Property(e => e.Credit).HasPrecision(18, 2);
            entity.Property(e => e.Debit).HasPrecision(18, 2);
            entity.Property(e => e.RunningBalance).HasPrecision(18, 2);
            entity.Property(e => e.TransactionNumber).HasMaxLength(100);

            entity.HasOne(d => d.Account).WithMany(p => p.GeneralLedgerPermanentAccounts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_GLPermanent_ChartOfAccounts");

            entity.HasOne(d => d.JournalEntryLine).WithMany(p => p.GeneralLedgerPermanentAccounts)
                .HasForeignKey(d => d.JournalEntryLineId)
                .HasConstraintName("FK_GLPermanent_GeneralJournal_LineId");

            entity.HasOne(d => d.Period).WithMany(p => p.GeneralLedgerPermanentAccounts)
                .HasForeignKey(d => d.PeriodId)
                .HasConstraintName("FK_GLPermanent_Periods");
        });

        builder.Entity<GeneralLedgerTemporaryAccount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("GeneralLedgerTemporaryAccounts_pkey");

            entity.HasIndex(e => new { e.UserId, e.PeriodId, e.AccountId, e.EntryDate, e.Id }, "IX_GLTemporary_FetchOrdering");

            entity.HasIndex(e => new { e.UserId, e.PeriodId }, "IX_GLTemporary_UserId_PeriodId");

            entity.HasIndex(e => e.JournalEntryLineId, "IX_GeneralLedgerTemporaryAccounts_JournalEntryLineId");

            entity.Property(e => e.Credit).HasPrecision(18, 2);
            entity.Property(e => e.Debit).HasPrecision(18, 2);
            entity.Property(e => e.RunningBalance).HasPrecision(18, 2);
            entity.Property(e => e.TransactionNumber).HasMaxLength(100);

            entity.HasOne(d => d.Account).WithMany(p => p.GeneralLedgerTemporaryAccounts)
                .HasForeignKey(d => d.AccountId)
                .HasConstraintName("FK_GLTemporary_ChartOfAccounts");

            entity.HasOne(d => d.JournalEntryLine).WithMany(p => p.GeneralLedgerTemporaryAccounts)
                .HasForeignKey(d => d.JournalEntryLineId)
                .HasConstraintName("FK_GLTemporary_GeneralJournal_LineId");

            entity.HasOne(d => d.Period).WithMany(p => p.GeneralLedgerTemporaryAccounts)
                .HasForeignKey(d => d.PeriodId)
                .HasConstraintName("FK_GLTemporary_Periods");
        });

        builder.Entity<LoginActivity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_LoginActivities_UserId_CreatedAt");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt }, "IX_Notifications_UserId_IsRead_CreatedAt");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        builder.Entity<Period>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_Periods_IsSelected_Unique")
                .IsUnique()
                .HasFilter("(\"IsSelected\" = true)");

            entity.HasIndex(e => e.UserId, "IX_Periods_UserId");

            entity.Property(e => e.IsClosed).HasDefaultValue(false);
            entity.Property(e => e.IsSelected).HasDefaultValue(false);
            entity.Property(e => e.PeriodName).HasMaxLength(50);
        });

        builder.Entity<RecoveryCode>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RecoveryCodes_pkey");

            entity.HasIndex(e => e.UserId, "IX_RecoveryCodes_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CodeHash).HasMaxLength(255);
            entity.Property(e => e.Used).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_RecoveryCodes_AspNetUsers");
        });

        builder.Entity<SecuritySetting>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("SecuritySettings_pkey");

            entity.Property(e => e.UserId).ValueGeneratedNever();
            entity.Property(e => e.EmailVerified).HasDefaultValue(false);
            entity.Property(e => e.LoginNotificationEnabled).HasDefaultValue(true);
            entity.Property(e => e.SessionTimeoutMinutes).HasDefaultValue(30);
            entity.Property(e => e.TwoFactorEnabled).HasDefaultValue(false);

            entity.HasOne(d => d.User).WithOne()
                .HasForeignKey<SecuritySetting>(d => d.UserId)
                .HasConstraintName("FK_SecuritySettings_AspNetUsers");
        });

        builder.Entity<TransactionCounter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TransactionCounters_pkey");

            entity.HasIndex(e => new { e.UserId, e.CounterKey }, "IX_TransactionCounters_UserId_CounterKey").IsUnique();

            entity.Property(e => e.CounterKey).HasMaxLength(10);
        });

        builder.Entity<TrustedDevice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TrustedDevices_pkey");

            entity.HasIndex(e => e.UserId, "IX_TrustedDevices_UserId");

            entity.HasIndex(e => e.DeviceIdentifier, "TrustedDevices_DeviceIdentifier_key").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Browser).HasMaxLength(100);
            entity.Property(e => e.DeviceIdentifier).HasMaxLength(255);
            entity.Property(e => e.DeviceName).HasMaxLength(100);
            entity.Property(e => e.IsTrusted).HasDefaultValue(true);
            entity.Property(e => e.OperatingSystem).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_TrustedDevices_AspNetUsers");
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.IsActive }, "IX_UserSessions_UserId_IsActive");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId);
        });

        builder.Entity<Worksheet>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Worksheet_pkey");

            entity.ToTable("Worksheet");

            entity.HasIndex(e => new { e.UserId, e.PeriodId }, "IX_Worksheet_UserId_PeriodId");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.AccountType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.IsTotalRow).HasDefaultValue(false);
            entity.Property(e => e.NormalBalanceIsDebit).HasDefaultValue(false);
            entity.Property(e => e.UnadjustedDebit).HasPrecision(18, 2);
            entity.Property(e => e.UnadjustedCredit).HasPrecision(18, 2);
            entity.Property(e => e.AdjustmentDebit).HasPrecision(18, 2);
            entity.Property(e => e.AdjustmentCredit).HasPrecision(18, 2);
            entity.Property(e => e.AdjustedDebit).HasPrecision(18, 2);
            entity.Property(e => e.AdjustedCredit).HasPrecision(18, 2);
            entity.Property(e => e.IncomeStatementDebit).HasPrecision(18, 2);
            entity.Property(e => e.IncomeStatementCredit).HasPrecision(18, 2);
            entity.Property(e => e.FinancialPositionDebit).HasPrecision(18, 2);
            entity.Property(e => e.FinancialPositionCredit).HasPrecision(18, 2);
        });

        builder.Entity<AdjustedTrialBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TrialBalanceAdjusted_pkey");

            entity.ToTable("TrialBalanceAdjusted");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreditBalance).HasPrecision(15, 2);
            entity.Property(e => e.DebitBalance).HasPrecision(15, 2);
            entity.Property(e => e.IsTotalRow).HasDefaultValue(false);
        });

        builder.Entity<PostClosingTrialBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TrialBalancePostClosing_pkey");

            entity.ToTable("TrialBalancePostClosing");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreditBalance).HasPrecision(15, 2);
            entity.Property(e => e.DebitBalance).HasPrecision(15, 2);
            entity.Property(e => e.IsTotalRow).HasDefaultValue(false);
        });

        builder.Entity<UnadjustedTrialBalance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("TrialBalanceUnadjusted_pkey");

            entity.ToTable("TrialBalanceUnadjusted");

            entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.Property(e => e.AccountName).HasMaxLength(255);
            entity.Property(e => e.AccountNumber).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.CreditBalance).HasPrecision(15, 2);
            entity.Property(e => e.DebitBalance).HasPrecision(15, 2);
            entity.Property(e => e.IsTotalRow).HasDefaultValue(false);
        });

        builder.HasSequence<int>("GeneralJournal_JournalEntryId_seq");

        OnModelCreatingPartial(builder);
    }

    partial void OnModelCreatingPartial(ModelBuilder builder);
}