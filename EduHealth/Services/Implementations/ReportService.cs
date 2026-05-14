using ClosedXML.Excel;
using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.Helpers;
using EduHealth.DTOs.Reports;
using EduHealth.DTOs.Dashboard;
using EduHealth.Services.Interfaces;
using EduHealth.Services.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduHealth.Services.Implementations
{
    public sealed class ReportService : IReportService
    {
        private readonly AppDbContext _context;
        private readonly INotificationTargetResolver _notificationTargetResolver;

        public ReportService(AppDbContext context, INotificationTargetResolver notificationTargetResolver)
        {
            _context = context;
            _notificationTargetResolver = notificationTargetResolver;
        }

        public async Task<AdminReportDashboardDto> GetAdminDashboardAsync(CancellationToken cancellationToken)
        {
            var filter = new AdminDashboardFilterDto();
            return await GetAdminDashboardAsync(filter, cancellationToken);
        }

        public async Task<AdminReportDashboardDto> GetAdminDashboardAsync(AdminDashboardFilterDto filter, CancellationToken cancellationToken)
        {
            // Determine classId from filter or null for all classes
            int? classId = filter?.ClassId;
            
            var aggregate = await BuildAggregateAsync(classId, filter, cancellationToken);

            return new AdminReportDashboardDto
            {
                Header = new ReportHeaderDto
                {
                    Title = "Báo cáo quản trị y tế học đường",
                    Description = "Đánh giá tổng quát sức khỏe học sinh toàn trường và theo dõi biến động bệnh lý định kỳ."
                },
                SummaryCards = BuildSummaryCards(aggregate),
                ChartData = aggregate.ClassRows.Select(ToChartRow).ToList(),
                ClassRows = aggregate.ClassRows.Select(ToClassRow).ToList(),
                SidePanel = BuildSidePanel(aggregate)
            };
        }

        public async Task<AdminClassDetailDto?> GetAdminClassDetailAsync(int classId, CancellationToken cancellationToken)
        {
            var aggregate = await BuildAggregateAsync(classId, cancellationToken);
            var classMetric = aggregate.ClassRows.FirstOrDefault();
            if (classMetric == null) return null;

            var total = classMetric.ClassSize;
            var stablePct = total == 0 ? 0 : (int)Math.Round(classMetric.Stable * 100.0 / total);
            var followUpPct = total == 0 ? 0 : (int)Math.Round(classMetric.FollowUp * 100.0 / total);
            var highRiskPct = total == 0 ? 0 : (int)Math.Round(classMetric.HighRisk * 100.0 / total);

            var completionRate = classMetric.VaccinationTotal == 0
                ? 0
                : (int)Math.Round(classMetric.VaccinationCompleted * 100.0 / classMetric.VaccinationTotal);

            var pending = Math.Max(0, classMetric.VaccinationTotal - classMetric.VaccinationCompleted);

            var highlightedIssues = new List<string>();
            if (classMetric.HighRisk > 0)
                highlightedIssues.Add($"Có {classMetric.HighRisk} học sinh thuộc nhóm nguy cơ cao.");
            if (classMetric.FollowUp > 0)
                highlightedIssues.Add($"Có {classMetric.FollowUp} học sinh cần theo dõi thêm.");
            if (pending > 0)
                highlightedIssues.Add($"Còn {pending} hồ sơ tiêm chủng chưa hoàn thành.");

            var riskAnalysis = new List<RiskAnalysisItemDto>();
            var idx = 1;
            foreach (var disease in classMetric.ContagiousDiseases.OrderByDescending(x => x.Value).Take(3))
            {
                riskAnalysis.Add(new RiskAnalysisItemDto
                {
                    Id = $"risk-{classId}-{idx++}",
                    Tone = "danger",
                    Title = $"{disease.Value} học sinh có dấu hiệu {disease.Key}",
                    Description = "Tổng hợp từ lần khám gần nhất có bệnh truyền nhiễm."
                });
            }

            if (pending > 0)
            {
                riskAnalysis.Add(new RiskAnalysisItemDto
                {
                    Id = $"risk-{classId}-{idx++}",
                    Tone = completionRate < 70 ? "danger" : "warning",
                    Title = "Tiêm chủng chưa hoàn tất",
                    Description = $"Còn {pending} bản ghi chưa đạt trạng thái DONE."
                });
            }

            return new AdminClassDetailDto
            {
                ClassId = classMetric.ClassId,
                ClassName = classMetric.ClassName,
                StudentCount = classMetric.ClassSize,
                TeacherName = classMetric.TeacherName ?? string.Empty,
                UrgencyLabel = classMetric.HighRisk > 0 ? "Đang theo dõi khẩn cấp" : "Theo dõi định kỳ",
                UrgencyTone = classMetric.HighRisk > 0 ? "danger" : "warning",
                RecipientStats = new RecipientStatsDto { Students = classMetric.ClassSize },
                Distribution = new ClassDistributionDto
                {
                    Stable = classMetric.Stable,
                    FollowUp = classMetric.FollowUp,
                    HighRisk = classMetric.HighRisk,
                    StablePct = stablePct,
                    FollowUpPct = followUpPct,
                    HighRiskPct = highRiskPct
                },
                Vaccination = new ClassVaccinationDto
                {
                    CompletionRate = completionRate,
                    Completed = classMetric.VaccinationCompleted,
                    Pending = pending,
                    StatusLabel = completionRate >= 90 ? "Hoàn thành tốt" : (completionRate >= 70 ? "Cần bổ sung hồ sơ tiêm chủng" : "Tỷ lệ hoàn thành thấp"),
                    StatusTone = completionRate >= 90 ? "success" : (completionRate >= 70 ? "warning" : "danger")
                },
                HighlightedIssues = highlightedIssues,
                RiskAnalysis = riskAnalysis
            };
        }

        public async Task<ReportClassDto?> GetClassReportAsync(int classId, CancellationToken cancellationToken)
        {
            var detail = await GetAdminClassDetailAsync(classId, cancellationToken);
            if (detail == null) return null;

            return new ReportClassDto
            {
                ClassId = detail.ClassId,
                ClassName = detail.ClassName,
                TeacherName = detail.TeacherName,
                TotalStudents = detail.StudentCount,
                HealthBreakdown = new HealthSummaryDto
                {
                    Healthy = detail.Distribution.Stable,
                    Sick = detail.Distribution.FollowUp,
                    Chronic = detail.Distribution.HighRisk
                },
                RiskList = new List<RiskStudentDto>()
            };
        }

        public async Task<ExportFileDto> ExportReportXlsxAsync(ExportRequestDto request, CancellationToken cancellationToken)
        {
            var aggregate = await BuildAggregateAsync(request.ClassId, cancellationToken);
            var summaryCards = BuildSummaryCards(aggregate);
            var format = (request.Format ?? "xlsx").Trim().ToLowerInvariant();
            var generatedAtVietnam = VietnamTimeHelper.Now;

            if (format == "pdf")
            {
                var pdfBytes = BuildAdminReportPdfBytes(aggregate, summaryCards);

                return new ExportFileDto
                {
                    FileName = $"AdminReport_{generatedAtVietnam:yyyyMMdd_HHmmss}.pdf",
                    ContentType = "application/pdf",
                    FileBytes = pdfBytes
                };
            }

            using var workbook = new XLWorkbook();
            var summarySheet = workbook.Worksheets.Add("Summary");

            summarySheet.Cell("A1").Value = "EduHealth Admin Report";
            summarySheet.Cell("A1").Style.Font.Bold = true;
            summarySheet.Cell("A1").Style.Font.FontSize = 16;

            summarySheet.Cell("A3").Value = "GeneratedAt(UTC+7)";
            summarySheet.Cell("B3").Value = generatedAtVietnam;
            summarySheet.Cell("B3").Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";

            summarySheet.Cell("A5").Value = "Metric";
            summarySheet.Cell("B5").Value = "Value";
            summarySheet.Range("A5:B5").Style.Font.Bold = true;

            var row = 6;
            foreach (var card in summaryCards)
            {
                summarySheet.Cell($"A{row}").Value = card.Label;
                summarySheet.Cell($"B{row}").Value = card.Value;
                row++;
            }

            summarySheet.Columns("A:B").AdjustToContents();

            var classSheet = workbook.Worksheets.Add("Classes");
            classSheet.Cell("A1").Value = "ClassName";
            classSheet.Cell("B1").Value = "ClassSize";
            classSheet.Cell("C1").Value = "Stable";
            classSheet.Cell("D1").Value = "FollowUp";
            classSheet.Cell("E1").Value = "HighRisk";
            classSheet.Cell("F1").Value = "VaccinationCompletionRate";
            classSheet.Range("A1:F1").Style.Font.Bold = true;

            var classRows = aggregate.ClassRows.Select(ToClassRow).ToList();
            for (var i = 0; i < classRows.Count; i++)
            {
                var r = i + 2;
                classSheet.Cell($"A{r}").Value = classRows[i].ClassName;
                classSheet.Cell($"B{r}").Value = classRows[i].ClassSize;
                classSheet.Cell($"C{r}").Value = classRows[i].Stable;
                classSheet.Cell($"D{r}").Value = classRows[i].FollowUp;
                classSheet.Cell($"E{r}").Value = classRows[i].HighRisk;
                classSheet.Cell($"F{r}").Value = $"{classRows[i].VaccinationCompletionRate}%";
            }
            classSheet.Columns("A:F").AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileDto
            {
                FileName = $"AdminReport_{generatedAtVietnam:yyyyMMdd_HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileBytes = stream.ToArray()
            };
        }

        public async Task<DirectiveResponseDto> CreateDirectiveAsync(DirectiveRequestDto request, int adminUserId, CancellationToken cancellationToken)
        {
            var createdBy = adminUserId > 0
                ? adminUserId
                : await _context.Users.AsNoTracking()
                    .Where(u => u.Role == "ADMIN")
                    .Select(u => u.UserId)
                    .FirstOrDefaultAsync(cancellationToken);

            var targetMode = _notificationTargetResolver.ResolveTargetMode(
                targetMode: null,
                classId: request.ClassId,
                recipientUserIds: request.RecipientUserIds,
                targetRoles: null);

            var recipients = targetMode == "NONE"
                ? new List<User>()
                : await _notificationTargetResolver.ResolveRecipientsAsync(new NotificationTargetResolveRequest
                {
                    TargetMode = targetMode,
                    ClassId = request.ClassId,
                    RecipientUserIds = request.RecipientUserIds
                }, cancellationToken);

            if (targetMode == "NONE" || (targetMode == "CLASS" && recipients.Count == 0))
            {
                recipients = await _notificationTargetResolver.ResolveRecipientsAsync(new NotificationTargetResolveRequest
                {
                    TargetMode = "ROLES",
                    TargetRoles = new[] { "NURSE" }
                }, cancellationToken);
            }

            var now = VietnamTimeHelper.Now;
            var notification = new Notification
            {
                Title = request.Title,
                Content = request.Content,
                Type = "DIRECTIVE",
                Visibility = "INTERNAL",
                Status = "PUBLISHED",
                CreatedByUserId = createdBy,
                CreatedAt = now,
                PublishedAt = now,
                ClassId = request.ClassId
            };

            foreach (var userId in recipients.Select(x => x.UserId))
            {
                notification.Recipients.Add(new NotificationRecipient
                {
                    UserId = userId,
                    IsRead = false,
                    SentAt = now,
                    Status = "SENT"
                });
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            return new DirectiveResponseDto
            {
                DirectiveId = notification.NotificationId,
                Status = "SENT",
                CreatedAt = now
            };
        }

        public async Task<SystemLogSummaryDto> GetSystemLogSummaryAsync(CancellationToken cancellationToken)
        {
            var total = await _context.SystemLogs.CountAsync(cancellationToken);
            return new SystemLogSummaryDto
            {
                TotalLogs = total,
                Breakdowns = new List<LogBreakdownDto>
                {
                    new LogBreakdownDto { Module = "STUDENT", Action = "CREATE", Role = "NURSE", Count = (int)(total * 0.4) },
                    new LogBreakdownDto { Module = "MEDICINE", Action = "UPDATE", Role = "ADMIN", Count = (int)(total * 0.6) }
                }
            };
        }

        public async Task<AdminNotificationPreviewResponseDto> PreviewNotificationsAsync(AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken)
        {
            var recipients = await _notificationTargetResolver.ResolvePreviewRecipientsAsync(
                request.ClassId,
                request.RecipientUserIds,
                cancellationToken);

            var previewRecipients = recipients
                .Select(u => new NotificationRecipientPreviewDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Role = u.Role
                })
                .ToList();

            return new AdminNotificationPreviewResponseDto
            {
                TotalRecipients = previewRecipients.Count,
                Recipients = previewRecipients
            };
        }

        public async Task<AdminNotificationResponseDto> SendNotificationsAsync(AdminNotificationRequestDto request, int adminId, CancellationToken cancellationToken)
        {
            var now = VietnamTimeHelper.Now;
            var targetMode = _notificationTargetResolver.ResolveTargetMode(
                targetMode: null,
                classId: request.ClassId,
                recipientUserIds: request.RecipientUserIds,
                targetRoles: null);
            var recipients = await _notificationTargetResolver.ResolveRecipientsAsync(new NotificationTargetResolveRequest
            {
                TargetMode = targetMode,
                ClassId = request.ClassId,
                RecipientUserIds = request.RecipientUserIds
            }, cancellationToken);

            var notification = new Notification
            {
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                Visibility = "INTERNAL",
                Status = "PUBLISHED",
                CreatedByUserId = adminId,
                CreatedAt = now,
                PublishedAt = now,
                ClassId = request.ClassId
            };

            foreach (var userId in recipients.Select(x => x.UserId))
            {
                notification.Recipients.Add(new NotificationRecipient
                {
                    UserId = userId,
                    IsRead = false,
                    SentAt = now,
                    Status = "SENT"
                });
            }

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync(cancellationToken);

            return new AdminNotificationResponseDto
            {
                NotificationId = notification.NotificationId,
                Status = "SENT",
                RecipientCount = notification.Recipients.Count
            };
        }

        public async Task<NurseReportDashboardDto> GetNurseDashboardAsync(int nurseId, CancellationToken cancellationToken)
        {
            var defaultFilter = new NurseReportFilterDto
            {
                TimeRange = "this-month",
                Grade = "all",
                ClassId = "all",
                ReportType = "overview"
            };

            return await GetNurseDashboardAsync(nurseId, defaultFilter, cancellationToken);
        }

        public async Task<NurseReportDashboardDto> GetNurseDashboardAsync(int nurseId, NurseReportFilterDto filter, CancellationToken cancellationToken)
        {
            _ = nurseId; // DB không có bảng phân công nurse-class/student

            var normalizedTimeRange = NormalizeTimeRange(filter.TimeRange);
            var normalizedReportType = NormalizeReportType(filter.ReportType);
            var normalizedGrade = string.IsNullOrWhiteSpace(filter.Grade) ? "all" : filter.Grade.Trim();
            var classId = ParseClassId(filter.ClassId);

            var (from, toExclusive) = ResolveDateRange(normalizedTimeRange, filter.FromDate, filter.ToDate);

            var classOptionsQuery = _context.SchoolClasses.AsNoTracking();
            if (!string.Equals(normalizedGrade, "all", StringComparison.OrdinalIgnoreCase))
                classOptionsQuery = classOptionsQuery.Where(c => c.Grade == normalizedGrade);

            var classOptions = await classOptionsQuery
                .OrderBy(c => c.ClassName)
                .Select(c => new NurseClassOptionDto
                {
                    Value = c.ClassId.ToString(),
                    Label = c.ClassName
                })
                .ToListAsync(cancellationToken);

            var classesQuery = _context.SchoolClasses.AsNoTracking();
            if (!string.Equals(normalizedGrade, "all", StringComparison.OrdinalIgnoreCase))
                classesQuery = classesQuery.Where(c => c.Grade == normalizedGrade);
            if (classId.HasValue)
                classesQuery = classesQuery.Where(c => c.ClassId == classId.Value);

            var classes = await classesQuery
                .Select(c => new { c.ClassId, c.ClassName, c.Grade })
                .OrderBy(c => c.ClassName)
                .ToListAsync(cancellationToken);

            var response = new NurseReportDashboardDto
            {
                Header = new ReportHeaderDto
                {
                    Title = "Báo cáo y tế tổng hợp",
                    Description = "Phân tích tình hình sức khỏe học sinh và hoạt động y tế theo lớp học."
                },
                AppliedFilters = new NurseAppliedFiltersDto
                {
                    TimeRange = normalizedTimeRange,
                    Grade = normalizedGrade,
                    ClassId = classId?.ToString() ?? "all",
                    ReportType = normalizedReportType
                },
                FilterOptions = new NurseFilterOptionsDto
                {
                    ClassOptions = classOptions
                },
                GeneratedAt = VietnamTimeHelper.Now
            };

            if (classes.Count == 0)
                return response;

            var classIds = classes.Select(c => c.ClassId).ToHashSet();

            var students = await _context.Students.AsNoTracking()
                .Where(s => classIds.Contains(s.ClassId))
                .Select(s => new { s.UserId, s.ClassId })
                .ToListAsync(cancellationToken);

            var studentIds = students.Select(x => x.UserId).ToHashSet();
            var studentClassMap = students.ToDictionary(x => x.UserId, x => x.ClassId);

            var visits = studentIds.Count == 0
                ? new List<(int VisitId, int StudentUserId, DateTime VisitDate, int? DiseaseId, string? Diagnosis)>()
                : await _context.HealthVisits.AsNoTracking()
                    .Where(v => studentIds.Contains(v.StudentUserId) && v.VisitDate >= from && v.VisitDate < toExclusive)
                    .Select(v => new ValueTuple<int, int, DateTime, int?, string?>(
                        v.VisitId, v.StudentUserId, v.VisitDate, v.DiseaseId, v.Diagnosis))
                    .ToListAsync(cancellationToken);

            var visitIds = visits.Select(v => v.Item1).ToHashSet();

            var prescriptions = visitIds.Count == 0
                ? new List<(int VisitId, int MedicineId, int Quantity)>()
                : await _context.VisitPrescriptions.AsNoTracking()
                    .Where(p => visitIds.Contains(p.VisitId))
                    .Select(p => new ValueTuple<int, int, int>(p.VisitId, p.MedicineId, p.Quantity))
                    .ToListAsync(cancellationToken);

            var vaccinations = studentIds.Count == 0
                ? new List<(int UserId, string Status, DateTime UpdatedAt)>()
                : await _context.StudentVaccinations.AsNoTracking()
                    .Where(v => studentIds.Contains(v.UserId))
                    .Select(v => new ValueTuple<int, string, DateTime>(v.UserId, v.Status, v.UpdatedAt))
                    .ToListAsync(cancellationToken);

            var diseaseTypes = await _context.DiseaseTypes.AsNoTracking()
                .Select(d => new { d.DiseaseId, d.DiseaseName, d.IsContagious })
                .ToListAsync(cancellationToken);

            var diseaseNameMap = diseaseTypes.ToDictionary(x => x.DiseaseId, x => x.DiseaseName);
            var contagiousIds = diseaseTypes.Where(x => x.IsContagious).Select(x => x.DiseaseId).ToHashSet();

            var metrics = classes.ToDictionary(
                c => c.ClassId,
                c => new NurseClassMetricInternal(c.ClassId, c.ClassName, c.Grade));

            foreach (var s in students) metrics[s.ClassId].StudentCount++;

            foreach (var v in visits)
            {
                if (!studentClassMap.TryGetValue(v.Item2, out var cid)) continue;
                metrics[cid].ExaminationCount++;
                metrics[cid].TrackingStudents.Add(v.Item2);
                if (v.Item4.HasValue && contagiousIds.Contains(v.Item4.Value)) metrics[cid].HasContagious = true;
            }

            var visitStudentMap = visits.ToDictionary(v => v.Item1, v => v.Item2);
            foreach (var p in prescriptions)
            {
                if (!visitStudentMap.TryGetValue(p.Item1, out var sid)) continue;
                if (!studentClassMap.TryGetValue(sid, out var cid)) continue;
                metrics[cid].MedicineDispenseCount += p.Item3;
            }

            foreach (var vac in vaccinations)
            {
                if (!studentClassMap.TryGetValue(vac.Item1, out var cid)) continue;
                metrics[cid].VaccinationTotal++;
                if (string.Equals(vac.Item2, "DONE", StringComparison.OrdinalIgnoreCase))
                    metrics[cid].VaccinationDone++;
            }

            response.ClassRows = metrics.Values
                .OrderBy(x => x.ClassName)
                .Select(x =>
                {
                    var tracking = x.TrackingStudents.Count;
                    var rate = x.VaccinationTotal == 0 ? 0 : (int)Math.Round(x.VaccinationDone * 100.0 / x.VaccinationTotal);
                    var status = (x.HasContagious || rate < 90) ? "alert" : (tracking > 0 ? "watch" : "safe");

                    return new NurseClassRowDto
                    {
                        Id = x.ClassId.ToString(),
                        ClassName = x.ClassName,
                        Grade = x.Grade ?? "all",
                        GradeLabel = string.IsNullOrWhiteSpace(x.Grade) ? "Chưa phân khối" : $"Khối {x.Grade}",
                        StudentCount = x.StudentCount,
                        ExaminationCount = x.ExaminationCount,
                        TrackingCount = tracking,
                        MedicineDispenseCount = x.MedicineDispenseCount,
                        VaccinationRate = rate,
                        Status = status
                    };
                })
                .ToList();

            response.Trend = BuildTrend(normalizedTimeRange, normalizedReportType, from, toExclusive, visits, prescriptions, vaccinations);

            response.DiseaseBreakdown = visits
                .GroupBy(v =>
                {
                    if (v.Item4.HasValue && diseaseNameMap.TryGetValue(v.Item4.Value, out var dn))
                        return (Id: v.Item4.Value.ToString(), Label: dn);

                    if (!string.IsNullOrWhiteSpace(v.Item5))
                        return (Id: $"diag-{v.Item5.Trim().ToLowerInvariant().Replace(" ", "-")}", Label: v.Item5.Trim());

                    return (Id: "other", Label: "Khác");
                })
                .Select(g => new NurseDiseaseBreakdownDto { Id = g.Key.Id, Label = g.Key.Label, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            var medicineIds = prescriptions.Select(x => x.Item2).Distinct().ToList();
            var medicineMap = medicineIds.Count == 0
                ? new Dictionary<int, (string Name, string? ActiveIngredient, string Unit, string? Packaging, int Stock, int Warning)>()
                : await _context.Medicines.AsNoTracking()
                    .Where(m => medicineIds.Contains(m.MedicineId))
                    .Select(m => new { m.MedicineId, m.Name, m.ActiveIngredient, m.Unit, m.Packaging, m.StockQuantity, m.WarningThreshold })
                    .ToDictionaryAsync(
                        x => x.MedicineId,
                        x => (Name: x.Name, ActiveIngredient: x.ActiveIngredient, Unit: x.Unit, Packaging: x.Packaging, Stock: x.StockQuantity, Warning: x.WarningThreshold),
                        cancellationToken);

            response.TopMedicines = prescriptions
                .GroupBy(x => x.Item2)
                .Select(g => new { MedicineId = g.Key, UsedQuantity = g.Sum(x => x.Item3) })
                .OrderByDescending(x => x.UsedQuantity)
                .Take(10)
                .Select(x =>
                {
                    medicineMap.TryGetValue(x.MedicineId, out var med);
                    var category = !string.IsNullOrWhiteSpace(med.ActiveIngredient) ? med.ActiveIngredient!
                        : !string.IsNullOrWhiteSpace(med.Unit) ? med.Unit
                        : (med.Packaging ?? string.Empty);

                    return new NurseTopMedicineDto
                    {
                        Id = x.MedicineId.ToString(),
                        Name = string.IsNullOrWhiteSpace(med.Name) ? $"Medicine #{x.MedicineId}" : med.Name,
                        Category = category,
                        UsedQuantity = x.UsedQuantity,
                        DeltaPercent = 0,
                        Trend = "stable",
                        StockStatus = med.Stock <= med.Warning ? "low" : "normal"
                    };
                })
                .ToList();

            response.RiskAlerts = await BuildRiskAlertsAsync(visits, vaccinations, contagiousIds, diseaseNameMap, cancellationToken);

            return response;
        }

        public async Task<ExportFileDto> ExportNurseReportAsync(int nurseId, NurseReportExportRequestDto request, CancellationToken cancellationToken)
        {
            var dashboard = await GetNurseDashboardAsync(nurseId, new NurseReportFilterDto
            {
                TimeRange = request.TimeRange,
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Grade = request.Grade,
                ClassId = request.ClassId,
                ReportType = request.ReportType
            }, cancellationToken);
            var generatedAtVietnam = dashboard.GeneratedAt;

            var format = (request.Format ?? "xlsx").Trim().ToLowerInvariant();
            if (format != "xlsx" && format != "pdf")
                throw new NotSupportedException("Chỉ hỗ trợ format pdf hoặc xlsx.");

            if (format == "pdf")
            {
                QuestPDF.Settings.License = LicenseType.Community;
                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(24);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                        page.Header().Column(col =>
                        {
                            col.Item().Text("Báo cáo y tế tổng hợp").Bold().FontSize(16);
                            col.Item().Text($"Thời gian xuất: {generatedAtVietnam:dd/MM/yyyy HH:mm:ss}");
                        });

                        page.Content().Column(col =>
                        {
                            col.Spacing(8);
                            col.Item().Text($"Bộ lọc: {dashboard.AppliedFilters.TimeRange} / {dashboard.AppliedFilters.ReportType}");
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(2);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1.5f);
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Text("ID").Bold();
                                    h.Cell().Text("Lớp").Bold();
                                    h.Cell().Text("Sĩ số").Bold();
                                    h.Cell().Text("Khám").Bold();
                                    h.Cell().Text("Theo dõi").Bold();
                                    h.Cell().Text("Tỷ lệ tiêm").Bold();
                                });

                                foreach (var row in dashboard.ClassRows)
                                {
                                    table.Cell().Text(row.Id);
                                    table.Cell().Text(row.ClassName);
                                    table.Cell().Text(row.StudentCount.ToString());
                                    table.Cell().Text(row.ExaminationCount.ToString());
                                    table.Cell().Text(row.TrackingCount.ToString());
                                    table.Cell().Text($"{row.VaccinationRate}%");
                                }
                            });
                        });
                    });
                });

                return new ExportFileDto
                {
                    FileName = $"NurseReport_{generatedAtVietnam:yyyyMMdd_HHmmss}.pdf",
                    ContentType = "application/pdf",
                    FileBytes = document.GeneratePdf()
                };
            }

            using var workbook = new XLWorkbook();
            var summary = workbook.Worksheets.Add("Summary");
            summary.Cell("A1").Value = "EduHealth Nurse Report";
            summary.Cell("A1").Style.Font.Bold = true;
            summary.Cell("A1").Style.Font.FontSize = 16;
            summary.Cell("A3").Value = "GeneratedAt(UTC+7)";
            summary.Cell("B3").Value = generatedAtVietnam;
            summary.Cell("B3").Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";

            var classes = workbook.Worksheets.Add("Classes");
            classes.Cell("A1").Value = "ClassId";
            classes.Cell("B1").Value = "ClassName";
            classes.Cell("C1").Value = "StudentCount";
            classes.Cell("D1").Value = "ExaminationCount";
            classes.Cell("E1").Value = "TrackingCount";
            classes.Cell("F1").Value = "MedicineDispenseCount";
            classes.Cell("G1").Value = "VaccinationRate";
            classes.Range("A1:G1").Style.Font.Bold = true;

            for (var i = 0; i < dashboard.ClassRows.Count; i++)
            {
                var r = i + 2;
                var row = dashboard.ClassRows[i];
                classes.Cell($"A{r}").Value = row.Id;
                classes.Cell($"B{r}").Value = row.ClassName;
                classes.Cell($"C{r}").Value = row.StudentCount;
                classes.Cell($"D{r}").Value = row.ExaminationCount;
                classes.Cell($"E{r}").Value = row.TrackingCount;
                classes.Cell($"F{r}").Value = row.MedicineDispenseCount;
                classes.Cell($"G{r}").Value = row.VaccinationRate;
            }
            classes.Columns("A:G").AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileDto
            {
                FileName = $"NurseReport_{generatedAtVietnam:yyyyMMdd_HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileBytes = stream.ToArray()
            };
        }

        private async Task<ReportAggregate> BuildAggregateAsync(int? classId, CancellationToken cancellationToken)
        {
            return await BuildAggregateAsync(classId, null, cancellationToken);
        }

        private async Task<ReportAggregate> BuildAggregateAsync(int? classId, AdminDashboardFilterDto? filter, CancellationToken cancellationToken)
        {
            var studentsQuery = _context.Students.AsNoTracking()
                .Where(s => !classId.HasValue || s.ClassId == classId.Value);

            // Apply disease type filter
            if (filter?.DiseaseTypeId.HasValue == true)
            {
                var studentIdsWithDisease = await _context.HealthVisits.AsNoTracking()
                    .Where(v => v.DiseaseId == filter.DiseaseTypeId.Value)
                    .Select(v => v.StudentUserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (studentIdsWithDisease.Count > 0)
                {
                    studentsQuery = studentsQuery.Where(s => studentIdsWithDisease.Contains(s.UserId));
                }
                else
                {
                    return new ReportAggregate();
                }
            }

            // Apply vaccination campaign filter
            if (filter?.VaccinationCampaignId.HasValue == true)
            {
                var studentIdsInCampaign = await _context.StudentVaccinations.AsNoTracking()
                    .Where(sv => sv.CampaignId == filter.VaccinationCampaignId.Value)
                    .Select(sv => sv.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (studentIdsInCampaign.Count > 0)
                {
                    studentsQuery = studentsQuery.Where(s => studentIdsInCampaign.Contains(s.UserId));
                }
                else
                {
                    return new ReportAggregate();
                }
            }

            var students = await studentsQuery
                .Select(s => new StudentRow
                {
                    UserId = s.UserId,
                    ClassId = s.ClassId,
                    ClassName = s.Class.ClassName,
                    TeacherName = s.Class.TeacherName ?? string.Empty
                })
                .ToListAsync(cancellationToken);

            if (students.Count == 0) return new ReportAggregate();

            var studentIds = students.Select(s => s.UserId).ToHashSet();

            // Apply date filter to visits
            var visitsQuery = _context.HealthVisits.AsNoTracking()
                .Where(v => studentIds.Contains(v.StudentUserId));

            if (filter?.FromDate.HasValue == true)
            {
                visitsQuery = visitsQuery.Where(v => v.VisitDate >= filter.FromDate.Value);
            }

            if (filter?.ToDate.HasValue == true)
            {
                visitsQuery = visitsQuery.Where(v => v.VisitDate <= filter.ToDate.Value);
            }

            var visits = await visitsQuery
                .Select(v => new VisitRow
                {
                    StudentUserId = v.StudentUserId,
                    VisitDate = v.VisitDate,
                    DiseaseId = v.DiseaseId
                })
                .ToListAsync(cancellationToken);

            var latestVisitByStudent = visits.Count > 0
                ? visits
                    .GroupBy(v => v.StudentUserId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.VisitDate).First())
                : new Dictionary<int, VisitRow>();

            var contagiousDiseaseIds = (await _context.DiseaseTypes.AsNoTracking()
                .Where(d => d.IsContagious)
                .Select(d => d.DiseaseId)
                .ToListAsync(cancellationToken))
                .ToHashSet();

            var diseaseNameMap = await _context.DiseaseTypes.AsNoTracking()
                .ToDictionaryAsync(d => d.DiseaseId, d => d.DiseaseName, cancellationToken);

            var vaccinations = await _context.StudentVaccinations.AsNoTracking()
                .Where(v => studentIds.Contains(v.UserId))
                .Select(v => new { v.UserId, v.Status })
                .ToListAsync(cancellationToken);

            var vaccinationByStudent = vaccinations
                .GroupBy(v => v.UserId)
                .ToDictionary(
                    g => g.Key,
                    g => new VaccinationStatus
                    {
                        Total = g.Count(),
                        Completed = g.Count(x => string.Equals(x.Status, "DONE", StringComparison.OrdinalIgnoreCase))
                    });

            var classMap = students
                .GroupBy(s => s.ClassId)
                .ToDictionary(
                    g => g.Key,
                    g => new ClassMetric
                    {
                        ClassId = g.Key,
                        ClassName = g.First().ClassName,
                        TeacherName = g.First().TeacherName,
                        ClassSize = g.Count()
                    });

            foreach (var s in students)
            {
                var hasVisit = latestVisitByStudent.TryGetValue(s.UserId, out var latestVisit);
                var hasContagious = hasVisit && latestVisit!.DiseaseId.HasValue && contagiousDiseaseIds.Contains(latestVisit.DiseaseId.Value);

                var vac = vaccinationByStudent.TryGetValue(s.UserId, out var v) ? v : new VaccinationStatus();
                var hasIncompleteVaccination = vac.Total > 0 && vac.Completed < vac.Total;

                var metric = classMap[s.ClassId];

                if (hasContagious)
                {
                    metric.HighRisk++;
                    if (latestVisit!.DiseaseId.HasValue && diseaseNameMap.TryGetValue(latestVisit.DiseaseId.Value, out var diseaseName))
                    {
                        metric.ContagiousDiseases[diseaseName] = metric.ContagiousDiseases.TryGetValue(diseaseName, out var c) ? c + 1 : 1;
                    }
                }
                else if (hasVisit || hasIncompleteVaccination)
                {
                    metric.FollowUp++;
                }
                else
                {
                    metric.Stable++;
                }

                metric.VaccinationTotal += vac.Total;
                metric.VaccinationCompleted += vac.Completed;

                if (hasVisit && (!metric.LastVisitAt.HasValue || latestVisit!.VisitDate > metric.LastVisitAt))
                {
                    metric.LastVisitAt = latestVisit.VisitDate;
                }
            }

            var lowSupplies = await _context.Medicines.AsNoTracking()
                .Where(m => m.StockQuantity <= m.WarningThreshold)
                .OrderBy(m => m.StockQuantity)
                .Select(m => new LowSupplyDto
                {
                    Id = $"sup-{m.MedicineId}",
                    Name = m.Name,
                    Remaining = m.StockQuantity.ToString(),
                    Tone = m.StockQuantity == 0 ? "danger" : "warning",
                    ThresholdLabel = $"Ngưỡng tối thiểu: {m.WarningThreshold}"
                })
                .ToListAsync(cancellationToken);

            return new ReportAggregate
            {
                ClassRows = classMap.Values.OrderBy(x => x.ClassName).ToList(),
                LowSupplies = lowSupplies
            };
        }

        private static List<ReportSummaryCardDto> BuildSummaryCards(ReportAggregate aggregate)
        {
            var totalStudents = aggregate.ClassRows.Sum(x => x.ClassSize);
            var stable = aggregate.ClassRows.Sum(x => x.Stable);
            var followUp = aggregate.ClassRows.Sum(x => x.FollowUp);
            var highRisk = aggregate.ClassRows.Sum(x => x.HighRisk);
            var vaccinationTotal = aggregate.ClassRows.Sum(x => x.VaccinationTotal);
            var vaccinationCompleted = aggregate.ClassRows.Sum(x => x.VaccinationCompleted);

            var stableRate = totalStudents == 0 ? 0 : Math.Round(stable * 100.0 / totalStudents, 1);
            var vaccinationRate = vaccinationTotal == 0 ? 0 : (int)Math.Round(vaccinationCompleted * 100.0 / vaccinationTotal);

            return new List<ReportSummaryCardDto>
            {
                new() { Id = "total-students", Label = "Tổng học sinh", Value = totalStudents.ToString() },
                new() { Id = "stable", Label = "Sức khỏe ổn định", Value = stable.ToString(), Note = $"{stableRate:0.0}% trên sĩ số hiện có" },
                new() { Id = "follow-up", Label = "Cần theo dõi", Value = followUp.ToString() },
                new() { Id = "critical", Label = "Cảnh báo y tế", Value = highRisk.ToString() },
                new() { Id = "vaccine-coverage", Label = "Tỷ lệ hoàn thành tiêm chủng", Value = $"{vaccinationRate}%", Progress = vaccinationRate }
            };
        }

        private static ReportChartDataDto ToChartRow(ClassMetric x)
        {
            var total = x.ClassSize == 0 ? 1 : x.ClassSize;
            return new ReportChartDataDto
            {
                ClassId = x.ClassId,
                Label = $"Lớp {x.ClassName}",
                Stable = x.Stable,
                FollowUp = x.FollowUp,
                HighRisk = x.HighRisk,
                StablePct = (int)Math.Round(x.Stable * 100.0 / total),
                FollowUpPct = (int)Math.Round(x.FollowUp * 100.0 / total),
                HighRiskPct = (int)Math.Round(x.HighRisk * 100.0 / total)
            };
        }

        private static ReportClassRowDto ToClassRow(ClassMetric x)
        {
            var completion = x.VaccinationTotal == 0 ? 0 : (int)Math.Round(x.VaccinationCompleted * 100.0 / x.VaccinationTotal);
            var (riskLabel, riskTone, rowTone) = ResolveRiskTone(x.HighRisk, x.FollowUp);

            return new ReportClassRowDto
            {
                ClassId = x.ClassId,
                ClassName = x.ClassName,
                ClassSize = x.ClassSize,
                Stable = x.Stable,
                FollowUp = x.FollowUp,
                HighRisk = x.HighRisk,
                VaccinationCompletionRate = completion,
                RiskLabel = riskLabel,
                RiskTone = riskTone,
                RowTone = rowTone
            };
        }

        private static ReportSidePanelDto BuildSidePanel(ReportAggregate aggregate)
        {
            var alerts = aggregate.ClassRows
                .Where(x => x.HighRisk > 0)
                .OrderByDescending(x => x.HighRisk)
                .Take(10)
                .Select(x =>
                {
                    var severe = x.HighRisk >= 3;
                    var latest = x.LastVisitAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
                    return new HighPriorityAlertDto
                    {
                        Id = $"alert-{x.ClassId}",
                        ClassId = x.ClassId,
                        ClassName = $"Lớp {x.ClassName}",
                        Severity = severe ? "KHẨN CẤP" : "THEO DÕI",
                        SeverityTone = severe ? "danger" : "warning",
                        Description = $"{x.HighRisk} học sinh nguy cơ cao",
                        Metric = $"{x.HighRisk} học sinh cảnh báo",
                        UpdatedAt = latest == string.Empty ? string.Empty : $"Cập nhật: {latest}",
                        UpdatedAtShort = latest
                    };
                })
                .ToList();

            var lowCoverage = aggregate.ClassRows
                .Select(x => new
                {
                    x.ClassId,
                    x.ClassName,
                    Coverage = x.VaccinationTotal == 0 ? 0 : (int)Math.Round(x.VaccinationCompleted * 100.0 / x.VaccinationTotal)
                })
                .Where(x => x.Coverage < 90)
                .OrderBy(x => x.Coverage)
                .Take(10)
                .Select(x => new LowVaccinationCoverageDto
                {
                    Id = $"vac-{x.ClassId}",
                    Label = $"Lớp {x.ClassName}",
                    Coverage = x.Coverage,
                    Tone = x.Coverage < 70 ? "danger" : "warning",
                    Note = x.Coverage < 90 ? "Chưa đạt mục tiêu 90%" : null
                })
                .ToList();

            return new ReportSidePanelDto
            {
                HighPriorityAlerts = alerts,
                LowSupplies = aggregate.LowSupplies,
                LowVaccinationCoverage = lowCoverage
            };
        }

        private static (string Label, string Tone, string RowTone) ResolveRiskTone(int highRisk, int followUp)
        {
            if (highRisk > 0) return ("Rất Cao", "danger", "danger");
            if (followUp > 0) return ("Trung bình", "warning", "warning");
            return ("Ổn định", "success", "default");
        }

        private static byte[] BuildAdminReportPdfBytes(ReportAggregate aggregate, List<ReportSummaryCardDto> summaryCards)
        {
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var classRows = aggregate.ClassRows.Select(ToClassRow).ToList();
            var generatedAtVietnam = VietnamTimeHelper.Now;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(24);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Spacing(4);
                        col.Item().Text("Báo cáo quản trị y tế học đường").Bold().FontSize(16);
                        col.Item().Text($"Thời gian xuất: {generatedAtVietnam:dd/MM/yyyy HH:mm:ss}");
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text("Tổng quan").Bold().FontSize(13);
                        foreach (var card in summaryCards)
                            col.Item().Text($"• {card.Label}: {card.Value}");

                        col.Item().PaddingTop(8).Text("Theo lớp").Bold().FontSize(13);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Lớp").Bold();
                                header.Cell().Text("Sĩ số").Bold();
                                header.Cell().Text("Ổn định").Bold();
                                header.Cell().Text("Theo dõi").Bold();
                                header.Cell().Text("Nguy cơ").Bold();
                                header.Cell().Text("Tỷ lệ tiêm").Bold();
                            });

                            foreach (var row in classRows)
                            {
                                table.Cell().Text(row.ClassName);
                                table.Cell().Text(row.ClassSize.ToString());
                                table.Cell().Text(row.Stable.ToString());
                                table.Cell().Text(row.FollowUp.ToString());
                                table.Cell().Text(row.HighRisk.ToString());
                                table.Cell().Text($"{row.VaccinationCompletionRate}%");
                            }
                        });
                    });

                    page.Footer()
                        .AlignRight()
                        .Text(x =>
                        {
                            x.Span("Trang ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static string NormalizeTimeRange(string? value)
        {
            var v = (value ?? "this-month").Trim().ToLowerInvariant();
            return v is "this-week" or "this-month" or "this-quarter" or "custom-range" ? v : "this-month";
        }

        private static string NormalizeReportType(string? value)
        {
            var v = (value ?? "overview").Trim().ToLowerInvariant();
            return v is "overview" or "health" or "vaccination" or "medicine" ? v : "overview";
        }

        private static int? ParseClassId(string? classIdRaw)
        {
            var raw = string.IsNullOrWhiteSpace(classIdRaw) ? "all" : classIdRaw.Trim();
            if (string.Equals(raw, "all", StringComparison.OrdinalIgnoreCase)) return null;
            if (int.TryParse(raw, out var parsed) && parsed > 0) return parsed;
            throw new ArgumentException("classId phải là số nguyên dương hoặc 'all'.");
        }

        private static (DateTime From, DateTime ToExclusive) ResolveDateRange(string timeRange, DateTime? fromDate, DateTime? toDate)
        {
            var now = VietnamTimeHelper.Now;

            if (timeRange == "custom-range")
            {
                if (!fromDate.HasValue || !toDate.HasValue)
                    throw new ArgumentException("fromDate và toDate là bắt buộc khi timeRange=custom-range.");

                var from = fromDate.Value.Date;
                var to = toDate.Value.Date;
                if (to < from) throw new ArgumentException("toDate phải >= fromDate.");
                return (from, to.AddDays(1));
            }

            if (timeRange == "this-week")
            {
                var diff = ((int)now.DayOfWeek + 6) % 7;
                var start = now.Date.AddDays(-diff);
                return (start, start.AddDays(7));
            }

            if (timeRange == "this-quarter")
            {
                var q = (now.Month - 1) / 3;
                var start = new DateTime(now.Year, q * 3 + 1, 1);
                return (start, start.AddMonths(3));
            }

            var monthStart = new DateTime(now.Year, now.Month, 1);
            return (monthStart, monthStart.AddMonths(1));
        }

        private static List<NurseTrendDto> BuildTrend(
            string timeRange,
            string reportType,
            DateTime from,
            DateTime toExclusive,
            List<(int VisitId, int StudentUserId, DateTime VisitDate, int? DiseaseId, string? Diagnosis)> visits,
            List<(int VisitId, int MedicineId, int Quantity)> prescriptions,
            List<(int UserId, string Status, DateTime UpdatedAt)> vaccinations)
        {
            IEnumerable<(DateTime Date, int Value)> source;

            if (reportType is "overview" or "health")
            {
                source = visits.Select(v => (v.VisitDate, 1));
            }
            else if (reportType == "medicine")
            {
                var visitDateMap = visits.ToDictionary(v => v.VisitId, v => v.VisitDate);
                source = prescriptions
                    .Where(p => visitDateMap.ContainsKey(p.VisitId))
                    .Select(p => (visitDateMap[p.VisitId], p.Quantity));
            }
            else
            {
                source = vaccinations
                    .Where(v => v.UpdatedAt >= from && v.UpdatedAt < toExclusive)
                    .Select(v => (v.UpdatedAt, 1));
            }

            return source
                .GroupBy(x => GetBucketStart(x.Date, timeRange))
                .OrderBy(g => g.Key)
                .Select(g => new NurseTrendDto
                {
                    Label = GetBucketLabel(g.Key, timeRange),
                    Value = g.Sum(x => x.Value)
                })
                .ToList();
        }

        private static DateTime GetBucketStart(DateTime date, string timeRange)
        {
            var d = date.Date;
            return timeRange switch
            {
                "this-month" => new DateTime(d.Year, d.Month, 1).AddDays(((d.Day - 1) / 7) * 7),
                "this-quarter" => new DateTime(d.Year, d.Month, 1),
                _ => d
            };
        }

        private static string GetBucketLabel(DateTime bucket, string timeRange)
        {
            return timeRange switch
            {
                "this-month" => $"Tuần {(((bucket.Day - 1) / 7) + 1):00}",
                "this-quarter" => $"Tháng {bucket.Month:00}",
                _ => bucket.ToString("dd/MM")
            };
        }

        private async Task<List<NurseRiskAlertDto>> BuildRiskAlertsAsync(
            List<(int VisitId, int StudentUserId, DateTime VisitDate, int? DiseaseId, string? Diagnosis)> visits,
            List<(int UserId, string Status, DateTime UpdatedAt)> vaccinations,
            HashSet<int> contagiousIds,
            Dictionary<int, string> diseaseNameMap,
            CancellationToken cancellationToken)
        {
            var alerts = new List<NurseRiskAlertDto>();

            alerts.AddRange(
                visits.Where(v => v.DiseaseId.HasValue && contagiousIds.Contains(v.DiseaseId.Value))
                    .GroupBy(v => v.DiseaseId!.Value)
                    .Select(g => new NurseRiskAlertDto
                    {
                        Id = $"contagious-{g.Key}",
                        Tone = "danger",
                        Title = $"Bệnh truyền nhiễm: {(diseaseNameMap.TryGetValue(g.Key, out var n) ? n : $"Disease #{g.Key}")}",
                        Message = $"Ghi nhận {g.Count()} lượt khám.",
                        TimeLabel = g.Max(x => x.VisitDate).ToString("dd/MM/yyyy HH:mm")
                    }));

            alerts.AddRange(await _context.Medicines.AsNoTracking()
                .Where(m => m.StockQuantity <= m.WarningThreshold)
                .OrderBy(m => m.StockQuantity)
                .Take(10)
                .Select(m => new NurseRiskAlertDto
                {
                    Id = $"low-stock-{m.MedicineId}",
                    Tone = "warning",
                    Title = $"Thuốc tồn thấp: {m.Name}",
                    Message = $"Tồn kho {m.StockQuantity}, ngưỡng {m.WarningThreshold}.",
                    TimeLabel = VietnamTimeHelper.Now.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync(cancellationToken));

            var today = VietnamTimeHelper.TodayDateOnly;
            var near = today.AddDays(30);

            var expiring = await _context.MedicineStockLogs.AsNoTracking()
                .Where(l => l.ExpiryDate.HasValue && l.ExpiryDate.Value >= today && l.ExpiryDate.Value <= near)
                .GroupBy(l => l.MedicineId)
                .Select(g => new { MedicineId = g.Key, ExpiryDate = g.Min(x => x.ExpiryDate) })
                .ToListAsync(cancellationToken);

            if (expiring.Count > 0)
            {
                var ids = expiring.Select(x => x.MedicineId).ToList();
                var names = await _context.Medicines.AsNoTracking()
                    .Where(m => ids.Contains(m.MedicineId))
                    .ToDictionaryAsync(m => m.MedicineId, m => m.Name, cancellationToken);

                alerts.AddRange(expiring.Select(x => new NurseRiskAlertDto
                {
                    Id = $"expiry-{x.MedicineId}",
                    Tone = "warning",
                    Title = $"Lô thuốc sắp hết hạn: {(names.TryGetValue(x.MedicineId, out var n) ? n : $"Medicine #{x.MedicineId}")}",
                    Message = $"Ngày hết hạn gần nhất: {x.ExpiryDate:dd/MM/yyyy}.",
                    TimeLabel = VietnamTimeHelper.Now.ToString("dd/MM/yyyy HH:mm")
                }));
            }

            var pending = vaccinations.Count(v => !string.Equals(v.Status, "DONE", StringComparison.OrdinalIgnoreCase));
            if (pending > 0)
            {
                alerts.Add(new NurseRiskAlertDto
                {
                    Id = "vaccination-pending",
                    Tone = "info",
                    Title = "Tiêm chủng chưa hoàn tất",
                    Message = $"Có {pending} bản ghi chưa đạt DONE.",
                    TimeLabel = VietnamTimeHelper.Now.ToString("dd/MM/yyyy HH:mm")
                });
            }

            return alerts;
        }

        private sealed class ReportAggregate
        {
            public List<ClassMetric> ClassRows { get; set; } = new();
            public List<LowSupplyDto> LowSupplies { get; set; } = new();
        }

        private sealed class StudentRow
        {
            public int UserId { get; set; }
            public int ClassId { get; set; }
            public string ClassName { get; set; } = null!;
            public string TeacherName { get; set; } = string.Empty;
        }

        private sealed class VisitRow
        {
            public int StudentUserId { get; set; }
            public DateTime VisitDate { get; set; }
            public int? DiseaseId { get; set; }
        }

        private sealed class VaccinationStatus
        {
            public int Total { get; set; }
            public int Completed { get; set; }
        }

        private sealed class ClassMetric
        {
            public int ClassId { get; set; }
            public string ClassName { get; set; } = null!;
            public string? TeacherName { get; set; }
            public int ClassSize { get; set; }
            public int Stable { get; set; }
            public int FollowUp { get; set; }
            public int HighRisk { get; set; }
            public int VaccinationTotal { get; set; }
            public int VaccinationCompleted { get; set; }
            public DateTime? LastVisitAt { get; set; }
            public Dictionary<string, int> ContagiousDiseases { get; } = new();
        }

        private sealed class NurseClassMetricInternal
        {
            public NurseClassMetricInternal(int classId, string className, string? grade)
            {
                ClassId = classId;
                ClassName = className;
                Grade = grade;
            }

            public int ClassId { get; }
            public string ClassName { get; }
            public string? Grade { get; }
            public int StudentCount { get; set; }
            public int ExaminationCount { get; set; }
            public int MedicineDispenseCount { get; set; }
            public int VaccinationTotal { get; set; }
            public int VaccinationDone { get; set; }
            public bool HasContagious { get; set; }
            public HashSet<int> TrackingStudents { get; } = new();
        }
    }
}
