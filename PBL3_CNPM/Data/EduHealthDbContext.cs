using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using EduHealthSystem.Models;

namespace EduHealthSystem.Data;

public partial class EduHealthDbContext : DbContext
{
    public EduHealthDbContext()
    {
    }

    public EduHealthDbContext(DbContextOptions<EduHealthDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AllergyType> AllergyTypes { get; set; }

    public virtual DbSet<Class> Classes { get; set; }

    public virtual DbSet<DiseaseType> DiseaseTypes { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<HealthVisit> HealthVisits { get; set; }

    public virtual DbSet<Medicine> Medicines { get; set; }

    public virtual DbSet<MedicineStockLog> MedicineStockLogs { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentAllergy> StudentAllergies { get; set; }

    public virtual DbSet<StudentVaccination> StudentVaccinations { get; set; }

    public virtual DbSet<SystemAlert> SystemAlerts { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Vaccination> Vaccinations { get; set; }

    public virtual DbSet<VisitPrescription> VisitPrescriptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer("Name=EduHealthDb");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AllergyType>(entity =>
        {
            entity.HasKey(e => e.AllergyId).HasName("PK__AllergyT__A49EBE62F2A5A018");
        });

        modelBuilder.Entity<Class>(entity =>
        {
            entity.HasKey(e => e.ClassId).HasName("PK__Classes__CB1927A0DFB9E57D");

            entity.HasOne(d => d.Grade).WithMany(p => p.Classes)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Classes_Grades");
        });

        modelBuilder.Entity<DiseaseType>(entity =>
        {
            entity.HasKey(e => e.DiseaseId).HasName("PK__DiseaseT__69B533A9FDF70A08");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradeId).HasName("PK__Grades__54F87A373B640F42");
        });

        modelBuilder.Entity<HealthVisit>(entity =>
        {
            entity.HasKey(e => e.VisitId).HasName("PK__HealthVi__4D3AA1BE205AEA9E");

            entity.Property(e => e.VisitDate).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Disease).WithMany(p => p.HealthVisits).HasConstraintName("FK_HealthVisits_DiseaseType");

            entity.HasOne(d => d.Nurse).WithMany(p => p.HealthVisits)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HealthVisits_Users_Nurse");

            entity.HasOne(d => d.Student).WithMany(p => p.HealthVisits).HasConstraintName("FK_HealthVisits_Students");
        });

        modelBuilder.Entity<Medicine>(entity =>
        {
            entity.HasKey(e => e.MedicineId).HasName("PK__Medicine__4F2128F0217E3AFC");
        });

        modelBuilder.Entity<MedicineStockLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PK__Medicine__5E5499A8AED968DC");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.Medicine).WithMany(p => p.MedicineStockLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicineStockLogs_Medicines");

            entity.HasOne(d => d.User).WithMany(p => p.MedicineStockLogs)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicineStockLogs_Users");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Students__32C52A79C2F0722F");

            entity.HasOne(d => d.Class).WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Classes");

            entity.HasOne(d => d.ParentUser).WithMany(p => p.Students)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Students_Users_Parent");
        });

        modelBuilder.Entity<StudentAllergy>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__StudentA__FBDF78C9BE1EAFCF");

            entity.HasOne(d => d.Allergy).WithMany(p => p.StudentAllergies)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentAllergies_AllergyTypes");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentAllergies).HasConstraintName("FK_StudentAllergies_Students");
        });

        modelBuilder.Entity<StudentVaccination>(entity =>
        {
            entity.HasKey(e => e.RecordId).HasName("PK__StudentV__FBDF78C97949EFA1");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentVaccinations).HasConstraintName("FK_StudentVaccinations_Students");

            entity.HasOne(d => d.Vaccine).WithMany(p => p.StudentVaccinations)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_StudentVaccinations_Vaccinations");
        });

        modelBuilder.Entity<SystemAlert>(entity =>
        {
            entity.HasKey(e => e.AlertId).HasName("PK__SystemAl__EBB16AED866E66AD");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CCAC43875DFD");

            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<Vaccination>(entity =>
        {
            entity.HasKey(e => e.VaccineId).HasName("PK__Vaccinat__45DC68E944521F7B");
        });

        modelBuilder.Entity<VisitPrescription>(entity =>
        {
            entity.HasKey(e => e.PrescriptionId).HasName("PK__VisitPre__401308120AE9694F");

            entity.HasOne(d => d.Medicine).WithMany(p => p.VisitPrescriptions)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VisitPrescriptions_Medicines");

            entity.HasOne(d => d.Visit).WithMany(p => p.VisitPrescriptions).HasConstraintName("FK_VisitPrescriptions_HealthVisits");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
