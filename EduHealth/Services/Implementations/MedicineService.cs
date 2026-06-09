using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Medicines;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace EduHealth.Services.Implementations
{
    public class MedicineService : IMedicineService
    {
        private static readonly HashSet<string> AllowedUnits = new(StringComparer.OrdinalIgnoreCase)
        {
            "VIEN", "GOI", "CHAI", "HOP", "TUYP", "ONG", "LO", "CUON", "CAI"
        };

        private readonly IMedicineRepository _medicineRepository;
        private readonly ISystemLogWriter _logWriter;
        private readonly AppDbContext _context;

        public MedicineService(IMedicineRepository medicineRepository, ISystemLogWriter logWriter, AppDbContext context)
        {
            _medicineRepository = medicineRepository;
            _logWriter = logWriter;
            _context = context;
        }

        public async Task<(IReadOnlyList<MedicineListItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetPagedAsync(
            MedicineListQueryDto query,
            CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _medicineRepository.GetPagedAsync(
                query.Keyword,
                query.Status,
                query.LowStock,
                query.Expiring,
                page,
                pageSize,
                cancellationToken);

            var list = new List<MedicineListItemDto>(items.Count);
            var expiringThreshold = VietnamTimeHelper.TodayDateOnly.AddDays(30);

            foreach (var m in items)
            {
                var nearestExpiry = m.NearestExpiryDate;
                var isLowStock = m.StockQuantity <= m.WarningThreshold;
                var isExpiring = nearestExpiry.HasValue && nearestExpiry.Value <= expiringThreshold;

                list.Add(new MedicineListItemDto
                {
                    Id = m.Code,
                    Name = m.Name,
                    ActiveIngredient = m.ActiveIngredient,
                    Unit = m.Unit,
                    Packaging = m.Packaging,
                    WarningThreshold = m.WarningThreshold,
                    CurrentStock = m.StockQuantity,
                    NearestExpiryDate = nearestExpiry,
                    IsLowStock = isLowStock,
                    IsExpiringSoon = isExpiring,
                    Status = m.Status
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);
            return (list, total, totalPages, page, pageSize);
        }

        public async Task<(bool Found, MedicineDetailDto? Data)> GetDetailAsync(string id, CancellationToken cancellationToken = default)
        {
            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (false, null);
            }

            await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);
            var batches = await _medicineRepository.GetBatchesAsync(medicine.MedicineId, cancellationToken);
            var nearestExpiry = medicine.NearestExpiryDate;
            var today = VietnamTimeHelper.TodayDateOnly;
            var expiringThreshold = VietnamTimeHelper.TodayDateOnly.AddDays(30);
            var fefoBatchId = batches
                .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0 && x.ExpiryDate >= today)
                .OrderBy(x => x.ExpiryDate)
                .ThenBy(x => x.ReceivedAt)
                .Select(x => (int?)x.MedicineBatchId)
                .FirstOrDefault();

            var dto = new MedicineDetailDto
            {
                Id = medicine.Code,
                Name = medicine.Name,
                ActiveIngredient = medicine.ActiveIngredient,
                Unit = medicine.Unit,
                Packaging = medicine.Packaging,
                WarningThreshold = medicine.WarningThreshold,
                CurrentStock = medicine.StockQuantity,
                NearestExpiryDate = nearestExpiry,
                Status = medicine.Status,
                Note = medicine.Note,
                IsLowStock = medicine.StockQuantity <= medicine.WarningThreshold,
                IsExpiringSoon = nearestExpiry.HasValue && nearestExpiry.Value <= expiringThreshold,
                Batches = batches.Select(x =>
                {
                    var isExpired = x.ExpiryDate < today;
                    return new MedicineBatchItemDto
                    {
                        Id = x.Code,
                        BatchNumber = x.BatchNumber,
                        ReceivedAt = x.ReceivedAt,
                        ExpiryDate = x.ExpiryDate,
                        InitialQuantity = x.InitialQuantity,
                        RemainingQuantity = x.RemainingQuantity,
                        Status = isExpired && x.RemainingQuantity > 0 && x.Status == "ACTIVE" ? "EXPIRED" : x.Status,
                        Note = x.Note,
                        IsExpired = isExpired,
                        IsExpiringSoon = !isExpired && x.ExpiryDate <= expiringThreshold && x.RemainingQuantity > 0 && x.Status == "ACTIVE",
                        IsFefoPriority = fefoBatchId == x.MedicineBatchId
                    };
                }).ToList(),
                CreatedAt = medicine.CreatedAt,
                UpdatedAt = medicine.UpdatedAt
            };

            return (true, dto);
        }

        public async Task<(bool Success, int? StatusCode, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, MedicineDetailDto? Data)> CreateAsync(
            int performedByUserId,
            CreateMedicineRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (request.InitialQuantity.HasValue || request.ExpiryDate.HasValue || !string.IsNullOrWhiteSpace(request.BatchNumber))
            {
                if (!request.InitialQuantity.HasValue || request.InitialQuantity.Value <= 0)
                    errors.Add(("initialQuantity", "INVALID_QUANTITY", "initialQuantity must be greater than 0."));
                if (!request.ExpiryDate.HasValue || request.ExpiryDate.Value <= VietnamTimeHelper.TodayDateOnly)
                    errors.Add(("expiryDate", "INVALID_EXPIRY_DATE", "expiryDate must be later than today."));
            }

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add(("name", "REQUIRED", "name bắt buộc."));

            if (string.IsNullOrWhiteSpace(request.Unit) || !AllowedUnits.Contains(request.Unit.Trim()))
                errors.Add(("unit", "INVALID_UNIT", "unit không hợp lệ."));

            if (request.WarningThreshold <= 0)
                errors.Add(("warningThreshold", "INVALID_WARNING_THRESHOLD", "Mức cảnh báo phải lớn hơn 0."));

            if (errors.Count > 0)
                return (false, 400, "Dữ liệu không hợp lệ.", errors, null);

            var name = request.Name.Trim();

            var existingMedicine = await _medicineRepository.GetByNameAsync(name, cancellationToken);
            if (existingMedicine is not null)
            {
                if (request.InitialQuantity.HasValue && request.ExpiryDate.HasValue)
                {
                    var stockInResult = await StockInAsync(existingMedicine.Code, performedByUserId, new StockInMedicineRequestDto
                    {
                        Quantity = request.InitialQuantity.Value,
                        ExpiryDate = request.ExpiryDate.Value,
                        BatchNumber = request.BatchNumber,
                        Note = request.Note
                    }, cancellationToken);

                    if (!stockInResult.Success)
                        return (false, 400, stockInResult.Message, stockInResult.Errors, null);

                    var existingDetail = (await GetDetailAsync(existingMedicine.Code, cancellationToken)).Data;
                    return (true, 200, "Medicine already existed; a new batch was added.", Array.Empty<(string, string, string)>(), existingDetail);
                }

                return (false, 409, "Thuốc đã tồn tại.", new[] { ("name", "MEDICINE_ALREADY_EXISTS", "Tên thuốc đã tồn tại trong hệ thống.") }, null);
            }

            var now = VietnamTimeHelper.Now;

            var medicine = new Medicine
            {
                Code = string.Empty,
                Name = name,
                ActiveIngredient = request.ActiveIngredient?.Trim(),
                Unit = request.Unit.Trim().ToUpperInvariant(),
                Packaging = request.Packaging?.Trim(),
                WarningThreshold = request.WarningThreshold,
                StockQuantity = 0,
                Status = "ACTIVE",
                Note = request.Note?.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                await _medicineRepository.AddAsync(medicine, cancellationToken);
                await _medicineRepository.SaveChangesAsync(cancellationToken);

                medicine.Code = $"MED{medicine.MedicineId:D3}";
                _medicineRepository.Update(medicine);

                if (request.InitialQuantity.HasValue && request.ExpiryDate.HasValue)
                {
                    var batch = new MedicineBatch
                    {
                        Code = GenerateTemporaryBatchCode(),
                        MedicineId = medicine.MedicineId,
                        BatchNumber = request.BatchNumber?.Trim(),
                        ReceivedAt = now,
                        ExpiryDate = request.ExpiryDate.Value,
                        InitialQuantity = request.InitialQuantity.Value,
                        RemainingQuantity = request.InitialQuantity.Value,
                        Status = "ACTIVE",
                        Note = request.Note?.Trim(),
                        CreatedByUserId = performedByUserId > 0 ? performedByUserId : null,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _medicineRepository.AddBatchAsync(batch, cancellationToken);
                    await _medicineRepository.SaveChangesAsync(cancellationToken);
                    batch.Code = $"MBT{batch.MedicineBatchId:D6}";

                    await _medicineRepository.AddMovementAsync(new MedicineStockLog
                    {
                        MedicineId = medicine.MedicineId,
                        MedicineBatchId = batch.MedicineBatchId,
                        UserId = performedByUserId,
                        Quantity = batch.InitialQuantity,
                        StockBefore = 0,
                        StockAfter = batch.InitialQuantity,
                        Type = "IMPORT",
                        ExpiryDate = batch.ExpiryDate,
                        BatchNumber = batch.BatchNumber,
                        Note = batch.Note,
                        CreatedAt = now
                    }, cancellationToken);
                }

                await _medicineRepository.SaveChangesAsync(cancellationToken);
                await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
                await _medicineRepository.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = performedByUserId > 0 ? performedByUserId : null,
                Module = "MEDICINES",
                Action = "CREATE_MEDICINE",
                TargetType = "Medicine",
                TargetId = medicine.Code,
                TargetLabel = medicine.Name,
                Description = "Tạo thuốc mới",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            var detail = (await GetDetailAsync(medicine.Code, cancellationToken)).Data;
            return (true, 201, "Tạo thuốc thành công.", Array.Empty<(string, string, string)>(), detail);
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateAsync(
            string id,
            UpdateMedicineRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (request.Name is null && request.ActiveIngredient is null && request.Unit is null && request.Packaging is null && request.WarningThreshold is null && request.Note is null)
            {
                return (false, "Dữ liệu không hợp lệ.", new[] { ("body", "NO_FIELDS", "Ít nhất 1 field phải được gửi lên.") }, null);
            }

            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (false, "Không tìm thấy thuốc.", new[] { ("id", "MEDICINE_NOT_FOUND", "Không tồn tại thuốc với id đã cung cấp.") }, null);
            }

            var errors = new List<(string Field, string Code, string Message)>();

            if (request.Name is not null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    errors.Add(("name", "REQUIRED", "name bắt buộc."));
                }
                else if (await _medicineRepository.AnyNameAsync(request.Name.Trim(), medicine.MedicineId, cancellationToken))
                {
                    errors.Add(("name", "MEDICINE_ALREADY_EXISTS", "Tên thuốc đã tồn tại trong hệ thống."));
                }
                else
                {
                    medicine.Name = request.Name.Trim();
                }
            }

            if (request.ActiveIngredient is not null)
            {
                medicine.ActiveIngredient = string.IsNullOrWhiteSpace(request.ActiveIngredient) ? null : request.ActiveIngredient.Trim();
            }

            if (request.Unit is not null)
            {
                if (string.IsNullOrWhiteSpace(request.Unit) || !AllowedUnits.Contains(request.Unit.Trim()))
                    errors.Add(("unit", "INVALID_UNIT", "unit không hợp lệ."));
                else
                    medicine.Unit = request.Unit.Trim().ToUpperInvariant();
            }

            if (request.Packaging is not null)
            {
                medicine.Packaging = string.IsNullOrWhiteSpace(request.Packaging) ? null : request.Packaging.Trim();
            }

            if (request.WarningThreshold.HasValue)
            {
                if (request.WarningThreshold.Value <= 0)
                    errors.Add(("warningThreshold", "INVALID_WARNING_THRESHOLD", "Mức cảnh báo phải lớn hơn 0."));
                else
                    medicine.WarningThreshold = request.WarningThreshold.Value;
            }

            if (request.Note is not null)
            {
                medicine.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
            }

            if (errors.Count > 0)
                return (false, "Dữ liệu không hợp lệ.", errors, null);

            medicine.UpdatedAt = VietnamTimeHelper.Now;
            _medicineRepository.Update(medicine);
            await _medicineRepository.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "MEDICINES",
                Action = "UPDATE_MEDICINE",
                TargetType = "Medicine",
                TargetId = medicine.Code,
                TargetLabel = medicine.Name,
                Description = "Cập nhật thông tin thuốc",
                Status = "SUCCESS",
                Metadata = new { }
            }, cancellationToken);

            return (true, "Cập nhật thuốc thành công.", Array.Empty<(string, string, string)>(), new
            {
                id = medicine.Code,
                name = medicine.Name,
                activeIngredient = medicine.ActiveIngredient,
                unit = medicine.Unit,
                packaging = medicine.Packaging,
                warningThreshold = medicine.WarningThreshold,
                note = medicine.Note,
                updatedAt = medicine.UpdatedAt
            });
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, object? Data)> UpdateStatusAsync(
            string id,
            UpdateMedicineStatusRequestDto request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Status))
                return (false, "Dữ liệu không hợp lệ.", new[] { ("status", "INVALID_STATUS", "status không hợp lệ.") }, null);

            var status = request.Status.Trim().ToUpperInvariant();
            if (status is not ("ACTIVE" or "INACTIVE"))
            {
                return (false, "Dữ liệu không hợp lệ.", new[] { ("status", "INVALID_STATUS", "status chỉ được phép là ACTIVE hoặc INACTIVE.") }, null);
            }

            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (false, "Không tìm thấy thuốc.", new[] { ("id", "MEDICINE_NOT_FOUND", "Không tồn tại thuốc với id đã cung cấp.") }, null);
            }

            medicine.Status = status;
            medicine.UpdatedAt = VietnamTimeHelper.Now;
            _medicineRepository.Update(medicine);
            await _medicineRepository.SaveChangesAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = null,
                Module = "MEDICINES",
                Action = "UPDATE_MEDICINE_STATUS",
                TargetType = "Medicine",
                TargetId = medicine.Code,
                TargetLabel = medicine.Name,
                Description = $"Cập nhật trạng thái thuốc thành {status}",
                Status = "SUCCESS",
                Metadata = new { status = status, reason = request.Reason?.Trim() }
            }, cancellationToken);

            return (true, "Cập nhật trạng thái thuốc thành công.", Array.Empty<(string, string, string)>(), new
            {
                id = medicine.Code,
                status = medicine.Status,
                reason = request.Reason?.Trim(),
                updatedAt = medicine.UpdatedAt
            });
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, StockMovementResponseDto? Data)> StockInAsync(
            string id,
            int performedByUserId,
            StockInMedicineRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (request.Quantity <= 0)
                errors.Add(("quantity", "INVALID_QUANTITY", "quantity phải > 0."));

            if (request.ExpiryDate <= VietnamTimeHelper.TodayDateOnly)
                errors.Add(("expiryDate", "INVALID_EXPIRY_DATE", "expiryDate phải lớn hơn ngày hiện tại."));

            if (errors.Count > 0)
                return (false, "Dữ liệu không hợp lệ.", errors, null);

            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (false, "Không tìm thấy thuốc.", new[] { ("id", "MEDICINE_NOT_FOUND", "Không tồn tại thuốc với id đã cung cấp.") }, null);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);

            var stockBefore = medicine.StockQuantity;
            var now = VietnamTimeHelper.Now;
            var batch = new MedicineBatch
            {
                Code = GenerateTemporaryBatchCode(),
                MedicineId = medicine.MedicineId,
                BatchNumber = request.BatchNumber?.Trim(),
                ReceivedAt = now,
                ExpiryDate = request.ExpiryDate,
                InitialQuantity = request.Quantity,
                RemainingQuantity = request.Quantity,
                Status = "ACTIVE",
                Note = request.Note?.Trim(),
                CreatedByUserId = performedByUserId,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _medicineRepository.AddBatchAsync(batch, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);
            batch.Code = $"MBT{batch.MedicineBatchId:D6}";

            var log = new MedicineStockLog
            {
                MedicineId = medicine.MedicineId,
                MedicineBatchId = batch.MedicineBatchId,
                UserId = performedByUserId,
                Quantity = request.Quantity,
                StockBefore = stockBefore,
                StockAfter = stockBefore + request.Quantity,
                Type = "IMPORT",
                Reason = null,
                ExpiryDate = batch.ExpiryDate,
                BatchNumber = batch.BatchNumber,
                Note = batch.Note,
                CreatedAt = now
            };

            await _medicineRepository.AddMovementAsync(log, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);
            await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = performedByUserId,
                Module = "MEDICINES",
                Action = "STOCK_IN",
                TargetType = "MedicineBatch",
                TargetId = batch.Code,
                TargetLabel = medicine.Name,
                Description = $"Nhập kho thuốc {medicine.Name}",
                Status = "SUCCESS",
                Metadata = new
                {
                    quantity = request.Quantity,
                    stockBefore,
                    stockAfter = medicine.StockQuantity,
                    expiryDate = request.ExpiryDate,
                    batchNumber = request.BatchNumber,
                    note = request.Note
                }
            }, cancellationToken);

            return (true, "Nhập kho thành công.", Array.Empty<(string, string, string)>(), new StockMovementResponseDto
            {
                MedicineId = medicine.Code,
                BatchId = batch.Code,
                MovementId = $"MSL{log.LogId:D3}",
                Type = log.Type,
                Quantity = log.Quantity,
                StockBefore = log.StockBefore,
                StockAfter = log.StockAfter,
                ExpiryDate = log.ExpiryDate,
                BatchNumber = log.BatchNumber,
                Reason = log.Reason,
                CreatedAt = log.CreatedAt
            });
        }

        public async Task<(bool Success, string Message, IReadOnlyList<(string Field, string Code, string Message)> Errors, StockMovementResponseDto? Data)> DisposeAsync(
            string id,
            int performedByUserId,
            DisposeMedicineRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var errors = new List<(string Field, string Code, string Message)>();

            if (request.Quantity <= 0)
                errors.Add(("quantity", "INVALID_QUANTITY", "quantity phải > 0."));

            if (string.IsNullOrWhiteSpace(request.Reason))
                errors.Add(("reason", "INVALID_REASON", "reason bắt buộc."));

            if (errors.Count > 0)
                return (false, "Dữ liệu không hợp lệ.", errors, null);

            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (false, "Không tìm thấy thuốc.", new[] { ("id", "MEDICINE_NOT_FOUND", "Không tồn tại thuốc với id đã cung cấp.") }, null);
            }

            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);

            List<MedicineBatch> batches;
            if (!string.IsNullOrWhiteSpace(request.BatchId))
            {
                var selectedBatch = await _medicineRepository.GetBatchByCodeAsync(medicine.MedicineId, request.BatchId, cancellationToken);
                if (selectedBatch is null)
                    return (false, "Batch not found.", new[] { ("batchId", "MEDICINE_BATCH_NOT_FOUND", "The requested batch does not exist.") }, null);
                batches = new List<MedicineBatch> { selectedBatch };
            }
            else
            {
                batches = await _medicineRepository.GetAvailableBatchesAsync(medicine.MedicineId, cancellationToken);
                if (!string.IsNullOrWhiteSpace(request.BatchNumber))
                    batches = batches.Where(x => x.BatchNumber == request.BatchNumber.Trim()).ToList();
            }

            var disposableStock = batches
                .Where(x => x.Status == "ACTIVE" && x.RemainingQuantity > 0)
                .Sum(x => x.RemainingQuantity);

            if (request.Quantity > disposableStock)
            {
                return (false, "Số lượng hủy không hợp lệ.", new[] { ("quantity", "DISPOSE_QUANTITY_EXCEEDS_STOCK", "Số lượng hủy vượt quá số lượng tồn kho.") }, null);
            }

            var stockBefore = medicine.StockQuantity;
            var stockCursor = stockBefore;
            var remainingToDispose = request.Quantity;
            var logs = new List<MedicineStockLog>();
            var now = VietnamTimeHelper.Now;

            foreach (var batch in batches.OrderBy(x => x.ExpiryDate).ThenBy(x => x.ReceivedAt))
            {
                if (remainingToDispose == 0)
                    break;

                var quantity = Math.Min(batch.RemainingQuantity, remainingToDispose);
                var affectsAvailableStock = batch.Status == "ACTIVE" &&
                    batch.ExpiryDate >= VietnamTimeHelper.TodayDateOnly;
                batch.RemainingQuantity -= quantity;
                batch.Status = batch.RemainingQuantity == 0 ? "DISPOSED" : "ACTIVE";
                batch.UpdatedAt = now;
                remainingToDispose -= quantity;
                var movementStockBefore = stockCursor;
                if (affectsAvailableStock)
                    stockCursor -= quantity;

                var batchLog = new MedicineStockLog
                {
                    MedicineId = medicine.MedicineId,
                    MedicineBatchId = batch.MedicineBatchId,
                    UserId = performedByUserId,
                    Quantity = quantity,
                    StockBefore = movementStockBefore,
                    StockAfter = stockCursor,
                    Type = "DISPOSE",
                    Reason = request.Reason.Trim().ToUpperInvariant(),
                    ExpiryDate = batch.ExpiryDate,
                    BatchNumber = batch.BatchNumber,
                    Note = request.Note?.Trim(),
                    CreatedAt = now
                };
                logs.Add(batchLog);
                await _medicineRepository.AddMovementAsync(batchLog, cancellationToken);
            }

            await _medicineRepository.SaveChangesAsync(cancellationToken);
            await _medicineRepository.RecalculateInventoryAsync(medicine, cancellationToken);
            await _medicineRepository.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
            var log = logs[0];

            await _logWriter.WriteAsync(new SystemLogWriteRequest
            {
                ActorUserId = performedByUserId,
                Module = "MEDICINES",
                Action = "DISPOSE_MEDICINE",
                TargetType = "Medicine",
                TargetId = medicine.Code,
                TargetLabel = medicine.Name,
                Description = $"Hủy thuốc {medicine.Name}",
                Status = "SUCCESS",
                Metadata = new
                {
                    quantity = request.Quantity,
                    stockBefore,
                    stockAfter = medicine.StockQuantity,
                    reason = request.Reason,
                    expiryDate = request.ExpiryDate,
                    batchNumber = request.BatchNumber,
                    note = request.Note
                }
            }, cancellationToken);

            return (true, "Hủy thuốc thành công.", Array.Empty<(string, string, string)>(), new StockMovementResponseDto
            {
                MedicineId = medicine.Code,
                BatchId = logs.Count == 1 ? batches[0].Code : null,
                MovementId = $"MSL{log.LogId:D3}",
                Type = log.Type,
                Quantity = request.Quantity,
                StockBefore = stockBefore,
                StockAfter = medicine.StockQuantity,
                ExpiryDate = log.ExpiryDate,
                BatchNumber = log.BatchNumber,
                Reason = log.Reason,
                CreatedAt = log.CreatedAt
            });
        }

        public async Task<(IReadOnlyList<MedicineMovementItemDto> Items, int TotalItems, int TotalPages, int Page, int PageSize)> GetMovementsAsync(
            string id,
            int page,
            int pageSize,
            string? type,
            DateTime? from,
            DateTime? to,
            CancellationToken cancellationToken = default)
        {
            var medicine = await _medicineRepository.GetByCodeAsync(id, cancellationToken);
            if (medicine is null)
            {
                return (Array.Empty<MedicineMovementItemDto>(), 0, 0, page, pageSize);
            }

            var total = await _medicineRepository.CountMovementsAsync(medicine.MedicineId, type, from, to, cancellationToken);
            var logs = await _medicineRepository.GetMovementsAsync(medicine.MedicineId, type, from, to, page, pageSize, cancellationToken);

            var items = logs.Select(x => new MedicineMovementItemDto
            {
                MovementId = $"MSL{x.LogId:D3}",
                BatchId = x.MedicineBatch?.Code,
                Type = x.Type,
                Quantity = x.Quantity,
                StockBefore = x.StockBefore,
                StockAfter = x.StockAfter,
                BatchNumber = x.BatchNumber,
                ExpiryDate = x.ExpiryDate,
                Reason = x.Reason,
                ReferenceType = null,
                ReferenceId = null,
                CreatedBy = new MedicineMovementCreatedByDto
                {
                    UserId = x.User.Code,
                    FullName = x.User.FullName
                },
                CreatedAt = x.CreatedAt
            }).ToList();

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize);
            return (items, total, totalPages, page, pageSize);
        }

        public async Task<IReadOnlyList<MedicineAlertItemDto>> GetAlertsAsync(string type, CancellationToken cancellationToken = default)
        {
            var upper = string.IsNullOrWhiteSpace(type) ? "ALL" : type.Trim().ToUpperInvariant();
            var threshold = VietnamTimeHelper.TodayDateOnly.AddDays(30);

            var (items, _) = await _medicineRepository.GetPagedAsync(null, "ACTIVE", null, null, 1, 1000, cancellationToken);
            var result = new List<MedicineAlertItemDto>();

            foreach (var m in items)
            {
                var nearestExpiry = m.NearestExpiryDate;
                var isLow = m.StockQuantity <= m.WarningThreshold;
                var isExp = nearestExpiry.HasValue && nearestExpiry.Value <= threshold;

                if (upper is "LOW_STOCK" or "ALL")
                {
                    if (isLow)
                    {
                        result.Add(new MedicineAlertItemDto
                        {
                            MedicineId = m.Code,
                            MedicineName = m.Name,
                            AlertType = "LOW_STOCK",
                            CurrentStock = m.StockQuantity,
                            WarningThreshold = m.WarningThreshold,
                            NearestExpiryDate = nearestExpiry,
                            Message = "Thuốc sắp hết hàng."
                        });
                    }
                }

                if (upper is "EXPIRING" or "ALL")
                {
                    if (isExp)
                    {
                        result.Add(new MedicineAlertItemDto
                        {
                            MedicineId = m.Code,
                            MedicineName = m.Name,
                            AlertType = "EXPIRING",
                            CurrentStock = m.StockQuantity,
                            WarningThreshold = m.WarningThreshold,
                            NearestExpiryDate = nearestExpiry,
                            Message = "Thuốc sắp hết hạn."
                        });
                    }
                }
            }

            return result;
        }

        private static string GenerateTemporaryBatchCode()
        {
            return $"TMP{Guid.NewGuid():N}"[..20];
        }
    }
}
