using EduHealth.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<SchoolClass> SchoolClasses => Set<SchoolClass>();
        public DbSet<AllergyType> AllergyTypes => Set<AllergyType>();
        public DbSet<DiseaseType> DiseaseTypes => Set<DiseaseType>();
        public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
        public DbSet<Medicine> Medicines => Set<Medicine>();
        public DbSet<Student> Students => Set<Student>();

        public DbSet<StudentAllergy> StudentAllergies => Set<StudentAllergy>();
        public DbSet<StudentVaccination> StudentVaccinations => Set<StudentVaccination>();
        public DbSet<HealthVisit> HealthVisits => Set<HealthVisit>();
        public DbSet<VisitPrescription> VisitPrescriptions => Set<VisitPrescription>();
        public DbSet<MedicineStockLog> MedicineStockLogs => Set<MedicineStockLog>();
        public DbSet<SystemAlert> SystemAlerts => Set<SystemAlert>();
        public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            base.OnModelCreating(modelBuilder);
        }
    }
}