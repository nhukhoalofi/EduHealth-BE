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

        public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            var today = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            return new DashboardSummaryDto
            {
                TotalStudents = await _context.Students.CountAsync(cancellationToken),
                TotalClasses = await _context.SchoolClasses.CountAsync(cancellationToken),
                TotalMedicines = await _context.Medicines.CountAsync(cancellationToken),
                LowStockMedicines = await _context.Medicines.CountAsync(m => m.StockQuantity <= m.WarningThreshold, cancellationToken),
                TotalVisitsToday = await _context.HealthVisits.CountAsync(v => v.VisitDate >= today, cancellationToken),
                TotalVisitsThisMonth = await _context.HealthVisits.CountAsync(v => v.VisitDate >= startOfMonth, cancellationToken)
            };
        }

        public async Task<IReadOnlyList<TopDiseaseDto>> GetTopDiseasesAsync(int top = 5, CancellationToken cancellationToken = default)
        {
            var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            return await _context.HealthVisits
                .Where(v => v.VisitDate >= startOfMonth && v.Diagnosis != null)
                .GroupBy(v => v.Diagnosis!)
                .Select(g => new TopDiseaseDto
                {
                    DiseaseName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(top)
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<RecentVisitDto>> GetRecentVisitsAsync(int count = 5, CancellationToken cancellationToken = default)
        {
            return await _context.HealthVisits
                .Include(v => v.Student)
                .OrderByDescending(v => v.VisitDate)
                .Take(count)
                .Select(v => new RecentVisitDto
                {
                    VisitCode = v.Code,
                    StudentCode = v.Student!.Code,
                    StudentName = v.Student.FullName,
                    Diagnosis = v.Diagnosis ?? "Không rõ",
                    VisitDate = v.VisitDate
                })
                .ToListAsync(cancellationToken);
        }
    }
}