using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class ExaminationRepository : IExaminationRepository
    {
        private readonly AppDbContext _context;

        public ExaminationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(List<HealthVisit> Items, int TotalCount)> GetPagedAsync(
            string? studentCode,
            string? classCode,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? diseaseCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.HealthVisits
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(s => s.User)
                .Include(x => x.Student).ThenInclude(s => s.Class)
                .Include(x => x.Nurse)
                .Include(x => x.DiseaseType)
                .Include(x => x.VisitPrescriptions)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(studentCode))
            {
                var sc = studentCode.Trim();
                query = query.Where(x => x.Student.Code == sc);
            }

            if (!string.IsNullOrWhiteSpace(classCode))
            {
                var cc = classCode.Trim();
                query = query.Where(x => x.Student.Class.Code == cc);
            }

            if (!string.IsNullOrWhiteSpace(diseaseCode))
            {
                var dc = diseaseCode.Trim();
                query = query.Where(x => x.DiseaseType != null && x.DiseaseType.Code == dc);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.VisitDate >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
                query = query.Where(x => x.VisitDate <= to);
            }

            var total = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.VisitDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task<HealthVisit?> GetByIdAsync(int visitId, CancellationToken cancellationToken = default)
        {
            return await _context.HealthVisits
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(s => s.User)
                .Include(x => x.Student).ThenInclude(s => s.Class)
                .Include(x => x.Nurse)
                .Include(x => x.DiseaseType)
                .Include(x => x.VisitPrescriptions).ThenInclude(vp => vp.Medicine)
                .FirstOrDefaultAsync(x => x.VisitId == visitId, cancellationToken);
        }

        public async Task<HealthVisit?> GetByCodeAsync(string visitCode, CancellationToken cancellationToken = default)
        {
            visitCode = visitCode.Trim();
            return await _context.HealthVisits
                .AsNoTracking()
                .Include(x => x.Student).ThenInclude(s => s.User)
                .Include(x => x.Student).ThenInclude(s => s.Class)
                .Include(x => x.Nurse)
                .Include(x => x.DiseaseType)
                .Include(x => x.VisitPrescriptions).ThenInclude(vp => vp.Medicine)
                .FirstOrDefaultAsync(x => x.Code == visitCode, cancellationToken);
        }

        public async Task<Student?> GetStudentByCodeAsync(string studentCode, CancellationToken cancellationToken = default)
        {
            return await _context.Students
                .Include(x => x.User)
                .Include(x => x.Class)
                .FirstOrDefaultAsync(x => x.Code == studentCode, cancellationToken);
        }

        public async Task<DiseaseType?> GetDiseaseTypeByIdAsync(int diseaseId, CancellationToken cancellationToken = default)
        {
            return await _context.DiseaseTypes
                .FirstOrDefaultAsync(x => x.DiseaseId == diseaseId, cancellationToken);
        }

        public async Task<DiseaseType?> GetDiseaseTypeByCodeAsync(string diseaseCode, CancellationToken cancellationToken = default)
        {
            return await _context.DiseaseTypes
                .FirstOrDefaultAsync(x => x.Code == diseaseCode, cancellationToken);
        }

        public async Task<Medicine?> GetMedicineByCodeAsync(string medCode, CancellationToken cancellationToken = default)
        {
            return await _context.Medicines
                .FirstOrDefaultAsync(x => x.Code == medCode, cancellationToken);
        }

        public async Task AddVisitAsync(HealthVisit visit, CancellationToken cancellationToken = default)
        {
            await _context.HealthVisits.AddAsync(visit, cancellationToken);
        }

        public async Task AddPrescriptionsAsync(IReadOnlyList<VisitPrescription> prescriptions, CancellationToken cancellationToken = default)
        {
            await _context.VisitPrescriptions.AddRangeAsync(prescriptions, cancellationToken);
        }

        public async Task AddStockLogsAsync(IReadOnlyList<MedicineStockLog> logs, CancellationToken cancellationToken = default)
        {
            await _context.MedicineStockLogs.AddRangeAsync(logs, cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
