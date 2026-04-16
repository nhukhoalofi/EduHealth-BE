using EduHealth.Data;
using EduHealth.DTOs.Dashboard;
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
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            return new AdminDashboardOverviewDto
            {
                TotalStudents = await _context.Students.CountAsync(),
                TotalClasses = await _context.SchoolClasses.CountAsync(),
                TotalUsers = await _context.Users.CountAsync(),
                ActiveUsers = await _context.Users.CountAsync(u => u.Status == "ACTIVE"),
                LockedUsers = await _context.Users.CountAsync(u => u.Status == "LOCKED"),
                TotalMedicines = await _context.Medicines.CountAsync(),
                LowStockMedicinesCount = await _context.Medicines.CountAsync(m => m.StockQuantity <= m.WarningThreshold),
                TotalVisitsToday = await _context.HealthVisits.CountAsync(v => v.VisitDate >= today),
                TotalVisitsThisMonth = await _context.HealthVisits.CountAsync(v => v.VisitDate >= startOfMonth),
                VaccinationCampaignsActive = await _context.VaccinationCampaigns.CountAsync(c => c.Status == "ACTIVE")
            };
        }

        public async Task<NurseDashboardOverviewDto> GetNurseOverviewAsync()
        {
            var today = DateTime.UtcNow.Date;
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

            var lowStockMedicines = await _context.Medicines
                .Where(m => m.StockQuantity <= m.WarningThreshold)
                .Select(m => new MedicineAlertDto
                {
                    MedicineId = m.MedicineId,
                    Code = m.Code,
                    Name = m.Name,
                    StockQuantity = m.StockQuantity,
                    WarningThreshold = m.WarningThreshold,
                    ExpiryDate = null
                })
                .ToListAsync();

            var expiringLogsQuery = await _context.MedicineStockLogs
                .Include(l => l.Medicine)
                .Where(l => l.Type == "IMPORT" && l.ExpiryDate != null && l.ExpiryDate <= thirtyDaysFromNow && l.ExpiryDate >= todayDateOnly)
                .OrderBy(l => l.ExpiryDate)
                .Take(10)
                .Select(l => new 
                {
                    l.MedicineId,
                    l.Medicine.Code,
                    l.Medicine.Name,
                    l.Medicine.StockQuantity,
                    l.Medicine.WarningThreshold,
                    l.ExpiryDate
                })
                .ToListAsync();

            var expiringMedicines = expiringLogsQuery
                .GroupBy(l => l.MedicineId)
                .Select(g => g.First())
                .Select(l => new MedicineAlertDto
                {
                    MedicineId = l.MedicineId,
                    Code = l.Code,
                    Name = l.Name,
                    StockQuantity = l.StockQuantity,
                    WarningThreshold = l.WarningThreshold,
                    ExpiryDate = l.ExpiryDate?.ToDateTime(TimeOnly.MinValue)
                })
                .ToList();

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
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

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