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

        public async Task<Medicine?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            name = name.Trim();
            return await _context.Medicines.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
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
            var today = VietnamTimeHelper.TodayDateOnly;

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
                query = query.Where(x =>
                    (x.MedicineBatches
                        .Where(b => b.Status == "ACTIVE" && b.RemainingQuantity > 0 && b.ExpiryDate >= today)
                        .Sum(b => (int?)b.RemainingQuantity) ?? 0) <= x.WarningThreshold);
            }

            if (expiring == true)
            {
                var threshold = today.AddDays(30);
                query = query.Where(x => x.MedicineBatches.Any(b =>
                    b.Status == "ACTIVE" &&
                    b.RemainingQuantity > 0 &&
                    b.ExpiryDate >= today &&
                    b.ExpiryDate <= threshold));
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            if (items.Count > 0)
            {
                var medicineIds = items.Select(x => x.MedicineId).ToList();
                var summaries = await _context.MedicineBatches
                    .AsNoTracking()
                    .Where(x =>
                        medicineIds.Contains(x.MedicineId) &&
                        x.Status == "ACTIVE" &&
                        x.RemainingQuantity > 0 &&
                        x.ExpiryDate >= today)
                    .GroupBy(x => x.MedicineId)
                    .Select(x => new
                    {
                        MedicineId = x.Key,
                        StockQuantity = x.Sum(b => b.RemainingQuantity),
                        NearestExpiryDate = x.Min(b => b.ExpiryDate)
                    })
                    .ToDictionaryAsync(x => x.MedicineId, cancellationToken);

                foreach (var medicine in items)
                {
                    if (summaries.TryGetValue(medicine.MedicineId, out var summary))
                    {
                        medicine.StockQuantity = summary.StockQuantity;
                        medicine.NearestExpiryDate = summary.NearestExpiryDate;
                    }
                    else
                    {
                        medicine.StockQuantity = 0;
                        medicine.NearestExpiryDate = null;
                    }
                }
            }

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

        public async Task<List<MedicineBatch>> GetBatchesAsync(int medicineId, CancellationToken cancellationToken = default)
        {
            return await _context.MedicineBatches
                .AsNoTracking()
                .Where(x => x.MedicineId == medicineId)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ReceivedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task<MedicineBatch?> GetBatchByCodeAsync(int medicineId, string batchCode, CancellationToken cancellationToken = default)
        {
            batchCode = batchCode.Trim();
            return await _context.MedicineBatches
                .FirstOrDefaultAsync(x => x.MedicineId == medicineId && x.Code == batchCode, cancellationToken);
        }

        public async Task<List<MedicineBatch>> GetAvailableBatchesAsync(int medicineId, CancellationToken cancellationToken = default)
        {
            var today = VietnamTimeHelper.TodayDateOnly;
            return await _context.MedicineBatches
                .Where(x =>
                    x.MedicineId == medicineId &&
                    x.Status == "ACTIVE" &&
                    x.RemainingQuantity > 0 &&
                    x.ExpiryDate >= today)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ReceivedAt)
                .ToListAsync(cancellationToken);
        }

        public async Task AddBatchAsync(MedicineBatch batch, CancellationToken cancellationToken = default)
        {
            await _context.MedicineBatches.AddAsync(batch, cancellationToken);
        }

        public async Task RecalculateInventoryAsync(Medicine medicine, CancellationToken cancellationToken = default)
        {
            var today = VietnamTimeHelper.TodayDateOnly;
            var available = _context.MedicineBatches
                .Where(x =>
                    x.MedicineId == medicine.MedicineId &&
                    x.Status == "ACTIVE" &&
                    x.RemainingQuantity > 0 &&
                    x.ExpiryDate >= today);

            medicine.StockQuantity = await available.SumAsync(x => (int?)x.RemainingQuantity, cancellationToken) ?? 0;
            medicine.NearestExpiryDate = await available
                .OrderBy(x => x.ExpiryDate)
                .Select(x => (DateOnly?)x.ExpiryDate)
                .FirstOrDefaultAsync(cancellationToken);
            medicine.UpdatedAt = VietnamTimeHelper.Now;
            _context.Medicines.Update(medicine);
        }

        public async Task<List<MedicineStockLog>> GetMovementsAsync(int medicineId, string? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = _context.MedicineStockLogs
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.MedicineBatch)
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

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
