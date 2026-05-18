using EduHealth.Data.Entities;

namespace EduHealth.Repositories.Interfaces
{
    public interface IExaminationRepository
    {
        Task<(List<HealthVisit> Items, int TotalCount)> GetPagedAsync(
            string? studentCode,
            string? classCode,
            DateOnly? fromDate,
            DateOnly? toDate,
            string? diseaseCode,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default);

        Task<HealthVisit?> GetByIdAsync(int visitId, CancellationToken cancellationToken = default);
        Task<Student?> GetStudentByCodeAsync(string studentCode, CancellationToken cancellationToken = default);
        Task<DiseaseType?> GetDiseaseTypeByIdAsync(int diseaseId, CancellationToken cancellationToken = default);
        Task<DiseaseType?> GetDiseaseTypeByCodeAsync(string diseaseCode, CancellationToken cancellationToken = default);
        Task<HealthVisit?> GetByCodeAsync(string visitCode, CancellationToken cancellationToken = default);

        Task<Medicine?> GetMedicineByCodeAsync(string medCode, CancellationToken cancellationToken = default);

        Task AddVisitAsync(HealthVisit visit, CancellationToken cancellationToken = default);
        Task AddPrescriptionsAsync(IReadOnlyList<VisitPrescription> prescriptions, CancellationToken cancellationToken = default);
        Task AddStockLogsAsync(IReadOnlyList<MedicineStockLog> logs, CancellationToken cancellationToken = default);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
