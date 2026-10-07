// Sinh bởi Scaffold-DbContext (Database First) với tùy chọn -NoOnConfiguring.
// Connection string được truyền vào từ WebMVC qua AddDataAccessLayer, không ghi cứng ở đây.
using System;
using System.Collections.Generic;
using AIVES.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIVES.DataAccess;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<ExamParticipant> ExamParticipants { get; set; }

    public virtual DbSet<ExamSession> ExamSessions { get; set; }

    public virtual DbSet<LecturerSubject> LecturerSubjects { get; set; }

    public virtual DbSet<ParticipantQuestion> ParticipantQuestions { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Subject> Subjects { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Accounts_Email").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(256)
                .IsUnicode(false);
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ExamParticipant>(entity =>
        {
            entity.HasIndex(e => new { e.SessionId, e.StudentCode }, "UQ_ExamParticipants_Student").IsUnique();

            entity.Property(e => e.FullName).HasMaxLength(100);
            entity.Property(e => e.Status)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.StudentCode)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Session).WithMany(p => p.ExamParticipants)
                .HasForeignKey(d => d.SessionId)
                .HasConstraintName("FK_ExamParticipants_Sessions");
        });

        modelBuilder.Entity<ExamSession>(entity =>
        {
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysutcdatetime())");
            entity.Property(e => e.Status)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(150);

            entity.HasOne(d => d.CreatedByNavigation).WithMany()
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSessions_Accounts");

            entity.HasOne(d => d.Subject).WithMany()
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExamSessions_Subjects");
        });

        modelBuilder.Entity<LecturerSubject>(entity =>
        {
            entity.HasKey(e => new { e.LecturerId, e.SubjectId });

            entity.Property(e => e.AssignedAt).HasDefaultValueSql("(sysutcdatetime())");

            entity.HasOne(d => d.Lecturer).WithMany(p => p.LecturerSubjects)
                .HasForeignKey(d => d.LecturerId)
                .HasConstraintName("FK_LecturerSubjects_Accounts");

            entity.HasOne(d => d.Subject).WithMany(p => p.LecturerSubjects)
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK_LecturerSubjects_Subjects");
        });

        modelBuilder.Entity<ParticipantQuestion>(entity =>
        {
            entity.HasKey(e => new { e.ParticipantId, e.QuestionId });

            entity.HasOne(d => d.Participant).WithMany(p => p.ParticipantQuestions)
                .HasForeignKey(d => d.ParticipantId)
                .HasConstraintName("FK_ParticipantQuestions_Participants");

            entity.HasOne(d => d.Question).WithMany()
                .HasForeignKey(d => d.QuestionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParticipantQuestions_Questions");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.Property(e => e.Content).HasMaxLength(1000);
            entity.Property(e => e.ExpectedPoints).HasMaxLength(1000);
            entity.Property(e => e.IsActive).HasDefaultValue(true);

            entity.HasOne(d => d.Subject).WithMany()
                .HasForeignKey(d => d.SubjectId)
                .HasConstraintName("FK_Questions_Subjects");
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(e => e.Code, "UQ_Subjects_Code").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.LanguageUpdatedBy)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(150);
            entity.Property(e => e.SttLanguage)
                .HasMaxLength(10)
                .IsUnicode(false);
            entity.Property(e => e.TtsLanguage)
                .HasMaxLength(10)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
