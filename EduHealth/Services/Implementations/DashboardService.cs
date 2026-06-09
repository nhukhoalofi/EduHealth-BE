using EduHealth.Data;
using EduHealth.DTOs.Dashboard;
using EduHealth.Helpers;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardOverviewDto> GetAdminOverviewAsync()
        {
            var today = VietnamTimeHelper.Now.Date;
            var todayDateOnly = DateOnly.FromDateTime(today);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            return new AdminDashboardOverviewDto
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalClasses = await _context.SchoolClasses.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.Status == "ACTIVE"),
                LockedUsers = await _context.Users.CountAsync(u => u.Status == "LOCKED"),
                TotalMedicines = await _context.Medicines.CountAsync(),
                LowStockMedicinesCount = await _context.Medicines.CountAsync(m =>
                    (m.MedicineBatches
                        .Where(b => b.Status == "ACTIVE" && b.RemainingQuantity > 0 && b.ExpiryDate >= todayDateOnly)
                        .Sum(b => (int?)b.RemainingQuantity) ?? 0) <= m.WarningThreshold),
                TotalVisitsToday = await _context.HealthVisits.CountAsync(v => v.VisitDate >= today),
                TotalVisitsThisMonth = await _context.HealthVisits.CountAsync(v => v.VisitDate >= startOfMonth),
                VaccinationCampaignsActive = await _context.VaccinationCampaigns.CountAsync(c => c.Status == "ACTIVE")
            };
        }

        public async Task<NurseDashboardOverviewDto> GetNurseOverviewAsync()
        {
            var today = VietnamTimeHelper.Now.Date;
            var todayDateOnly = DateOnly.FromDateTime(today);
            var thirtyDaysFromNow = todayDateOnly.AddDays(30);

            var totalVisitsToday = await _context.HealthVisits.CountAsync(v => v.VisitDate >= today);

            var recentExaminations = await _context.HealthVisits
                .Include(v => v.Student)
                .OrderByDescending(v => v.VisitDate)
                .Take(5)
                .Select(v => new RecentExaminationDto
                {
                    VisitId = v.VisitId,
                    Code = v.Code,
                    StudentName = v.Student != null ? v.Student.FullName : "Không rõ",
                    VisitDate = v.VisitDate,
                    Diagnosis = v.Diagnosis ?? "Không rõ"
                })
                .ToListAsync();

            var medicineInventory = _context.Medicines
                .Select(m => new
                {
                    Medicine = m,
                    StockQuantity = m.MedicineBatches
                        .Where(b => b.Status == "ACTIVE" && b.RemainingQuantity > 0 && b.ExpiryDate >= todayDateOnly)
                        .Sum(b => (int?)b.RemainingQuantity) ?? 0,
                    NearestExpiryDate = m.MedicineBatches
                        .Where(b => b.Status == "ACTIVE" && b.RemainingQuantity > 0 && b.ExpiryDate >= todayDateOnly)
                        .Min(b => (DateOnly?)b.ExpiryDate)
                });

            var lowStockMedicines = await medicineInventory
                .Where(x => x.StockQuantity <= x.Medicine.WarningThreshold)
                .Select(x => new MedicineAlertDto
                {
                    MedicineId = x.Medicine.MedicineId,
                    Code = x.Medicine.Code,
                    Name = x.Medicine.Name,
                    StockQuantity = x.StockQuantity,
                    WarningThreshold = x.Medicine.WarningThreshold,
                    ExpiryDate = x.NearestExpiryDate.HasValue
                        ? x.NearestExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                        : null
                })
                .ToListAsync();

            var expiringMedicines = await medicineInventory
                .Where(x => x.NearestExpiryDate >= todayDateOnly && x.NearestExpiryDate <= thirtyDaysFromNow)
                .OrderBy(x => x.NearestExpiryDate)
                .Take(10)
                .Select(x => new MedicineAlertDto
                {
                    MedicineId = x.Medicine.MedicineId,
                    Code = x.Medicine.Code,
                    Name = x.Medicine.Name,
                    StockQuantity = x.StockQuantity,
                    WarningThreshold = x.Medicine.WarningThreshold,
                    ExpiryDate = x.NearestExpiryDate.HasValue
                        ? x.NearestExpiryDate.Value.ToDateTime(TimeOnly.MinValue)
                        : null
                })
                .ToListAsync();

            var pendingVaccinationsCount = await _context.StudentVaccinations
                .CountAsync(v => v.Status == "PENDING" || v.Status == "NOT_VACCINATED");

            return new NurseDashboardOverviewDto
            {
                TotalVisitsToday = totalVisitsToday,
                RecentExaminations = recentExaminations,
                LowStockMedicines = lowStockMedicines,
                ExpiringMedicines = expiringMedicines,
                PendingVaccinationsCount = pendingVaccinationsCount
            };
        }

        public async Task<IReadOnlyList<TopDiseaseDto>> GetTopDiseasesAsync(int top = 5, CancellationToken cancellationToken = default)
        {
            var now = VietnamTimeHelper.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // Tách query để EF Core có thể thực thi chính xác.
            // Bước 1: Group by DiseaseId và đếm
            var topDiseaseIds = await _context.HealthVisits
                .Where(v => v.VisitDate >= startOfMonth && v.DiseaseId != null)
                .GroupBy(v => v.DiseaseId)
                .Select(g => new
                {
                    DiseaseId = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync(cancellationToken);

            // Bước 2: Load Name từ bảng DiseaseTypes theo danh sách Id đã lấy
            var ids = topDiseaseIds.Select(x => x.DiseaseId).ToList();
            var diseaseTypes = await _context.DiseaseTypes
                .Where(dt => ids.Contains(dt.DiseaseId))
                .ToDictionaryAsync(dt => dt.DiseaseId, dt => dt.DiseaseName, cancellationToken);

            // Bước 3: Map ra DTO
            return topDiseaseIds.Select(item => new TopDiseaseDto
            {
                DiseaseName = diseaseTypes.TryGetValue(item.DiseaseId!.Value, out var name) ? name : "Không rõ",
                Count = item.Count
            }).ToList();
        }
    }
}
