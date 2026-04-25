using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduHealth.Repositories.Implementations
{
    public class MedicineRepository : IMedicineRepository
    {
        private readonly AppDbContext _context;

        public MedicineRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Medicine?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            code = code.Trim();
            return await _context.Medicines.FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
        }

        public async Task<Medicine?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _context.Medicines.FirstOrDefaultAsync(x => x.MedicineId == id, cancellationToken);
        }

        public async Task<bool> AnyNameAsync(string name, int? excludeMedicineId = null, CancellationToken cancellationToken = default)
        {
            name = name.Trim();
            var q = _context.Medicines.AsQueryable();
            if (excludeMedicineId.HasValue)
            {
                q = q.Where(x => x.MedicineId != excludeMedicineId.Value);
            }

            return await q.AnyAsync(x => x.Name == name, cancellationToken);
        }

        public async Task<(List<Medicine> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? status,
            bool? lowStock,
            bool? expiring,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            var query = _context.Medicines.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(x => x.Name.Contains(k) || (x.ActiveIngredient != null && x.ActiveIngredient.Contains(k)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                var s = status.Trim();
                query = query.Where(x => x.Status == s);
            }

            if (lowStock == true)
            {
                query = query.Where(x => x.StockQuantity <= x.WarningThreshold);
            }

            if (expiring == true)
            {
                var threshold = VietnamTimeHelper.TodayDateOnly.AddDays(30);
                var candidates = _context.MedicineStockLogs
                    .AsNoTracking()
                    .Where(l => l.MedicineId == _context.Medicines.Select(m => m.MedicineId).FirstOrDefault() && l.ExpiryDate != null);
                // note: actual expiring filter applied in service using nearest expiry for each medicine.
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, total);
        }

        public async Task AddAsync(Medicine medicine, CancellationToken cancellationToken = default)
        {
            await _context.Medicines.AddAsync(medicine, cancellationToken);
        }

        public void Update(Medicine medicine)
        {
            _context.Medicines.Update(medicine);
        }

        public async Task<List<MedicineStockLog>> GetMovementsAsync(int medicineId, string? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.MedicineStockLogs
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => x.MedicineId == medicineId);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                query = query.Where(x => x.Type == t);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= to.Value);
            }

            return await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountMovementsAsync(int medicineId, string? type, DateTime? from, DateTime? to, CancellationToken cancellationToken = default)
        {
            var query = _context.MedicineStockLogs.AsNoTracking().Where(x => x.MedicineId == medicineId);

            if (!string.IsNullOrWhiteSpace(type))
            {
                var t = type.Trim();
                query = query.Where(x => x.Type == t);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.CreatedAt >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.CreatedAt <= to.Value);
            }

            return await query.CountAsync(cancellationToken);
        }

        public async Task AddMovementAsync(MedicineStockLog log, CancellationToken cancellationToken = default)
        {
            await _context.MedicineStockLogs.AddAsync(log, cancellationToken);
        }

        public async Task<DateOnly?> GetNearestExpiryDateAsync(int medicineId, CancellationToken cancellationToken = default)
        {
            return await _context.MedicineStockLogs
                .AsNoTracking()
                .Where(x => x.MedicineId == medicineId && x.ExpiryDate != null)
                .OrderBy(x => x.ExpiryDate)
                .Select(x => x.ExpiryDate)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
