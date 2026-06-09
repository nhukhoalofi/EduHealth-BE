using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IMedicineRepository
    {
        Task<Medicine?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
        Task<Medicine?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Medicine?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
        Task<bool> AnyNameAsync(string name, int? excludeMedicineId = null, CancellationToken cancellationToken = default);

        Task<(List<Medicine> Items, int TotalCount)> GetPagedAsync(
            string? keyword,
            string? status,
            bool? lowStock,
            bool? expiring,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task AddAsync(Medicine medicine, CancellationToken cancellationToken = default);
        void Update(Medicine medicine);

        Task<List<MedicineBatch>> GetBatchesAsync(int medicineId, CancellationToken cancellationToken = default);
        Task<MedicineBatch?> GetBatchByCodeAsync(int medicineId, string batchCode, CancellationToken cancellationToken = default);
        Task<List<MedicineBatch>> GetAvailableBatchesAsync(int medicineId, CancellationToken cancellationToken = default);
        Task AddBatchAsync(MedicineBatch batch, CancellationToken cancellationToken = default);
        Task RecalculateInventoryAsync(Medicine medicine, CancellationToken cancellationToken = default);

        Task<List<MedicineStockLog>> GetMovementsAsync(int medicineId, string? type, DateTime? from, DateTime? to, int page, int pageSize, CancellationToken cancellationToken = default);
        Task<int> CountMovementsAsync(int medicineId, string? type, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);

        Task AddMovementAsync(MedicineStockLog log, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
