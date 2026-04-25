using ClosedXML.Excel;
using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Reports;
using EduHealth.DTOs.Dashboard;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace EduHealth.Services.Implementations
{
    public sealed class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
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

            if (format == "pdf")
            {
                var pdfBytes = BuildAdminReportPdfBytes(aggregate, summaryCards);

                return new ExportFileDto
                {
                    FileName = $"AdminReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf",
                    ContentType = "application/pdf",
                    FileBytes = pdfBytes
                };
            }

            using var workbook = new XLWorkbook();
            var summarySheet = workbook.Worksheets.Add("Summary");

            summarySheet.Cell("A1").Value = "EduHealth Admin Report";
            summarySheet.Cell("A1").Style.Font.Bold = true;
            summarySheet.Cell("A1").Style.Font.FontSize = 16;

            summarySheet.Cell("A3").Value = "GeneratedAt(UTC)";
            summarySheet.Cell("B3").Value = DateTime.UtcNow;

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
                FileName = $"AdminReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
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

            var recipientIds = new HashSet<int>();

            if (request.ClassId.HasValue)
            {
                var studentIds = await _context.Students.AsNoTracking()
                    .Where(s => s.ClassId == request.ClassId.Value)
                    .Select(s => s.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var id in studentIds) recipientIds.Add(id);
            }

            if (recipientIds.Count == 0)
            {
                var nurseIds = await _context.Users.AsNoTracking()
                    .Where(u => u.Role == "NURSE" && u.IsActive)
                    .Select(u => u.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var id in nurseIds) recipientIds.Add(id);
            }

            var now = DateTime.UtcNow;
            var notification = new Notification
            {
                Title = request.Title,
                Content = request.Content,
                Type = "DIRECTIVE",
                CreatedByUserId = createdBy,
                CreatedAt = now,
                ClassId = request.ClassId
            };

            foreach (var userId in recipientIds)
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

        // ===== NURSE REPORT METHODS =====

        public async Task<NurseReportDashboardDto> GetNurseDashboardAsync(
            string timeRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? grade,
            string? classId,
            string? reportType,
            CancellationToken cancellationToken = default)
        {
            // Resolve date range
            var (startDate, endDate) = ResolveDateRange(timeRange, fromDate, toDate);

            // Build class query
            var classesQuery = _context.SchoolClasses.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(grade) && grade != "all")
            {
                classesQuery = classesQuery.Where(c => c.Grade == grade);
            }

            int? filterClassId = null;
            if (!string.IsNullOrWhiteSpace(classId) && classId != "all" && int.TryParse(classId, out var parsedClassId))
            {
                filterClassId = parsedClassId;
                classesQuery = classesQuery.Where(c => c.ClassId == parsedClassId);
            }

            var classes = await classesQuery
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.ClassName)
                .ToListAsync(cancellationToken);

            var classIds = classes.Select(c => c.ClassId).ToList();

            // Get class options for filter
            var allClasses = await _context.SchoolClasses.AsNoTracking().OrderBy(c => c.Grade).ThenBy(c => c.ClassName).ToListAsync(cancellationToken);
            var classOptions = allClasses.Select(c => new FilterOptionItemDto { Value = c.ClassId.ToString(), Label = c.ClassName }).ToList();

            var gradeOptions = allClasses.Select(c => c.Grade).Distinct().OrderBy(g => g)
                .Select(g => new FilterOptionItemDto { Value = g, Label = $"Khối {g}" }).ToList();

            // Build nurse class rows
            var classRows = new List<NurseClassRowDto>();
            foreach (var cls in classes)
            {
                var studentCount = await _context.Students.CountAsync(s => s.ClassId == cls.ClassId && s.User.IsActive, cancellationToken);

                var examinationCount = await _context.HealthVisits.CountAsync(
                    v => v.Student.ClassId == cls.ClassId && v.VisitDate >= startDate && v.VisitDate <= endDate,
                    cancellationToken);

                var trackingStudents = await _context.HealthVisits
                    .AsNoTracking()
                    .Where(v => v.Student.ClassId == cls.ClassId && v.VisitDate >= startDate && v.VisitDate <= endDate)
                    .Select(v => v.StudentUserId)
                    .Distinct()
                    .CountAsync(cancellationToken);

                var medicineDispenseCount = await _context.VisitPrescriptions
                    .AsNoTracking()
                    .Where(vp => vp.HealthVisit.Student.ClassId == cls.ClassId && 
                                  vp.HealthVisit.VisitDate >= startDate && 
                                  vp.HealthVisit.VisitDate <= endDate)
                    .SumAsync(vp => vp.Quantity, cancellationToken);

                var totalVaccinations = await _context.StudentVaccinations
                    .AsNoTracking()
                    .Where(sv => sv.Student.ClassId == cls.ClassId)
                    .CountAsync(cancellationToken);

                var completedVaccinations = await _context.StudentVaccinations
                    .AsNoTracking()
                    .Where(sv => sv.Student.ClassId == cls.ClassId && sv.Status == "DONE")
                    .CountAsync(cancellationToken);

                var vaccinationRate = totalVaccinations == 0 ? 0 : (int)Math.Round(completedVaccinations * 100.0 / totalVaccinations);

                // Determine status
                var hasContagious = await _context.HealthVisits
                    .AsNoTracking()
                    .AnyAsync(v => v.Student.ClassId == cls.ClassId && 
                                    v.VisitDate >= startDate && 
                                    v.VisitDate <= endDate &&
                                    v.DiseaseType != null &&
                                    v.DiseaseType.IsContagious,
                    cancellationToken);

                string status = "safe";
                if (hasContagious)
                    status = "alert";
                else if (vaccinationRate < 70 || trackingStudents > 0)
                    status = "watch";

                classRows.Add(new NurseClassRowDto
                {
                    Id = cls.ClassId,
                    ClassName = cls.ClassName,
                    Grade = cls.Grade,
                    GradeLabel = $"Khối {cls.Grade}",
                    StudentCount = studentCount,
                    ExaminationCount = examinationCount,
                    TrackingCount = trackingStudents,
                    MedicineDispenseCount = medicineDispenseCount,
                    VaccinationRate = vaccinationRate,
                    Status = status
                });
            }

            // Disease breakdown
            var diseaseBreakdown = await GetDiseaseBreakdownAsync(classIds, startDate, endDate, cancellationToken);

            // Top medicines
            var topMedicines = await GetTopMedicinesAsync(classIds, startDate, endDate, cancellationToken);

            // Risk alerts
            var riskAlerts = await BuildRiskAlertsAsync(classIds, startDate, endDate, cancellationToken);

            // Trend data
            var trend = await BuildTrendAsync(classIds, startDate, endDate, reportType ?? "overview", cancellationToken);

            return new NurseReportDashboardDto
            {
                Header = new ReportHeaderDto
                {
                    Title = "Báo cáo y tế tổng hợp",
                    Description = "Báo cáo hoạt động y tế theo từng lớp học"
                },
                AppliedFilters = new NurseAppliedFiltersDto
                {
                    TimeRange = timeRange,
                    Grade = grade ?? "all",
                    ClassId = classId ?? "all",
                    ReportType = reportType ?? "overview"
                },
                FilterOptions = new NurseFilterOptionsDto
                {
                    ClassOptions = classOptions,
                    GradeOptions = gradeOptions
                },
                Trend = trend,
                DiseaseBreakdown = diseaseBreakdown,
                TopMedicines = topMedicines,
                RiskAlerts = riskAlerts,
                ClassRows = classRows,
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<NurseClassDetailReportDto?> GetNurseClassDetailAsync(
            int classId,
            string timeRange,
            DateTime? fromDate,
            DateTime? toDate,
            CancellationToken cancellationToken = default)
        {
            var cls = await _context.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);

            if (cls == null)
                return null;

            var (startDate, endDate) = ResolveDateRange(timeRange, fromDate, toDate);

            var studentCount = await _context.Students
                .CountAsync(s => s.ClassId == classId && s.User.IsActive, cancellationToken);

            var examinationCount = await _context.HealthVisits
                .CountAsync(v => v.Student.ClassId == classId && v.VisitDate >= startDate && v.VisitDate <= endDate, cancellationToken);

            var students = await _context.Students
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.ClassId == classId && s.User.IsActive)
                .ToListAsync(cancellationToken);

            var studentSummaries = new List<StudentHealthSummaryDto>();
            foreach (var student in students)
            {
                var visits = await _context.HealthVisits
                    .AsNoTracking()
                    .Where(v => v.StudentUserId == student.UserId && v.VisitDate >= startDate && v.VisitDate <= endDate)
                    .OrderByDescending(v => v.VisitDate)
                    .ToListAsync(cancellationToken);

                studentSummaries.Add(new StudentHealthSummaryDto
                {
                    UserId = student.UserId,
                    FullName = student.FullName,
                    ExaminationCount = visits.Count,
                    LastDiagnosis = visits.FirstOrDefault()?.Diagnosis,
                    LastVisitDate = visits.FirstOrDefault()?.VisitDate
                });
            }

            var diseaseBreakdown = await GetDiseaseBreakdownAsync(new List<int> { classId }, startDate, endDate, cancellationToken);
            var medicinesUsed = await GetTopMedicinesAsync(new List<int> { classId }, startDate, endDate, cancellationToken);

            var totalVaccinations = await _context.StudentVaccinations
                .AsNoTracking()
                .Where(sv => sv.Student.ClassId == classId)
                .CountAsync(cancellationToken);

            var completedVaccinations = await _context.StudentVaccinations
                .AsNoTracking()
                .Where(sv => sv.Student.ClassId == classId && sv.Status == "DONE")
                .CountAsync(cancellationToken);

            var vaccinationRate = totalVaccinations == 0 ? 0 : (int)Math.Round(completedVaccinations * 100.0 / totalVaccinations);

            return new NurseClassDetailReportDto
            {
                ClassId = classId,
                ClassName = cls.ClassName,
                Grade = cls.Grade,
                StudentCount = studentCount,
                ExaminationCount = examinationCount,
                Students = studentSummaries,
                DiseaseBreakdown = diseaseBreakdown,
                MedicinesUsed = medicinesUsed,
                Vaccination = new VaccinationSummaryDto
                {
                    TotalRecords = totalVaccinations,
                    CompletedCount = completedVaccinations,
                    PendingCount = totalVaccinations - completedVaccinations,
                    CompletionRate = vaccinationRate
                }
            };
        }

        public async Task<ExportFileDto> ExportNurseReportAsync(
            string format,
            string timeRange,
            DateTime? fromDate,
            DateTime? toDate,
            string? grade,
            string? classId,
            string? reportType,
            CancellationToken cancellationToken = default)
        {
            var dashboard = await GetNurseDashboardAsync(timeRange, fromDate, toDate, grade, classId, reportType, cancellationToken);

            if (format == "pdf")
            {
                // PDF export not implemented - return Excel instead
                format = "xlsx";
            }

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Báo cáo");

            var row = 1;
            sheet.Cell(row, 1).Value = dashboard.Header.Title;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 1).Style.Font.FontSize = 14;
            row += 2;

            sheet.Cell(row, 1).Value = "Lớp";
            sheet.Cell(row, 2).Value = "Khối";
            sheet.Cell(row, 3).Value = "Số HS";
            sheet.Cell(row, 4).Value = "Lần Khám";
            sheet.Cell(row, 5).Value = "HS Theo Dõi";
            sheet.Cell(row, 6).Value = "Cấp Thuốc";
            sheet.Cell(row, 7).Value = "Tiêm (%)";
            sheet.Cell(row, 8).Value = "Trạng Thái";
            sheet.Range(row, 1, row, 8).Style.Font.Bold = true;
            row++;

            foreach (var classRow in dashboard.ClassRows)
            {
                sheet.Cell(row, 1).Value = classRow.ClassName;
                sheet.Cell(row, 2).Value = classRow.Grade;
                sheet.Cell(row, 3).Value = classRow.StudentCount;
                sheet.Cell(row, 4).Value = classRow.ExaminationCount;
                sheet.Cell(row, 5).Value = classRow.TrackingCount;
                sheet.Cell(row, 6).Value = classRow.MedicineDispenseCount;
                sheet.Cell(row, 7).Value = $"{classRow.VaccinationRate}%";
                sheet.Cell(row, 8).Value = classRow.Status;
                row++;
            }

            sheet.Columns("A:H").AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ExportFileDto
            {
                FileName = $"NurseReport_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileBytes = stream.ToArray()
            };
        }

        private (DateTime, DateTime) ResolveDateRange(string timeRange, DateTime? fromDate, DateTime? toDate)
        {
            var now = DateTime.UtcNow.Date;

            if (timeRange == "this-week")
                return (now.AddDays(-(int)now.DayOfWeek), now);
            else if (timeRange == "this-month")
                return (new DateTime(now.Year, now.Month, 1), now);
            else if (timeRange == "this-quarter")
            {
                var quarter = (now.Month - 1) / 3;
                var qStart = new DateTime(now.Year, quarter * 3 + 1, 1);
                return (qStart, now);
            }
            else if (timeRange == "custom-range")
                return (fromDate?.Date ?? now, toDate?.Date ?? now);
            else
                return (now.AddMonths(-1), now);
        }

        private async Task<List<DiseaseBreakdownItemDto>> GetDiseaseBreakdownAsync(
            List<int> classIds,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken)
        {
            var result = new List<DiseaseBreakdownItemDto>();

            var diseases = await _context.HealthVisits
                .AsNoTracking()
                .Where(v => classIds.Contains(v.Student.ClassId) && v.VisitDate >= startDate && v.VisitDate <= endDate)
                .GroupBy(v => new { v.DiseaseId, v.DiseaseType!.DiseaseName })
                .Select(g => new { g.Key.DiseaseId, g.Key.DiseaseName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var d in diseases)
            {
                result.Add(new DiseaseBreakdownItemDto
                {
                    Id = d.DiseaseId?.ToString() ?? "unknown",
                    Label = d.DiseaseName ?? "Không xác định",
                    Count = d.Count
                });
            }

            return result;
        }

        private async Task<List<TopMedicineItemDto>> GetTopMedicinesAsync(
            List<int> classIds,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken)
        {
            var result = new List<TopMedicineItemDto>();

            var medicines = await _context.VisitPrescriptions
                .AsNoTracking()
                .Include(vp => vp.Medicine)
                .Where(vp => classIds.Contains(vp.HealthVisit.Student.ClassId) &&
                             vp.HealthVisit.VisitDate >= startDate &&
                             vp.HealthVisit.VisitDate <= endDate)
                .GroupBy(vp => new { vp.Medicine.MedicineId, vp.Medicine.Code, vp.Medicine.Name, vp.Medicine.ActiveIngredient, vp.Medicine.StockQuantity, vp.Medicine.WarningThreshold })
                .Select(g => new 
                { 
                    g.Key.MedicineId,
                    g.Key.Code,
                    g.Key.Name,
                    g.Key.ActiveIngredient,
                    g.Key.StockQuantity,
                    g.Key.WarningThreshold,
                    UsedQuantity = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.UsedQuantity)
                .Take(10)
                .ToListAsync(cancellationToken);

            foreach (var m in medicines)
            {
                result.Add(new TopMedicineItemDto
                {
                    Id = m.Code,
                    Name = m.Name,
                    Category = m.ActiveIngredient ?? m.Code,
                    UsedQuantity = m.UsedQuantity,
                    StockStatus = m.StockQuantity <= m.WarningThreshold ? "low" : "normal"
                });
            }

            return result;
        }

        private async Task<List<RiskAlertDto>> BuildRiskAlertsAsync(
            List<int> classIds,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken)
        {
            var result = new List<RiskAlertDto>();
            var alertId = 1;

            // Check for contagious diseases
            var contagiousCases = await _context.HealthVisits
                .AsNoTracking()
                .Where(v => classIds.Contains(v.Student.ClassId) &&
                            v.VisitDate >= startDate &&
                            v.VisitDate <= endDate &&
                            v.DiseaseType != null &&
                            v.DiseaseType.IsContagious)
                .CountAsync(cancellationToken);

            if (contagiousCases > 0)
            {
                result.Add(new RiskAlertDto
                {
                    Id = $"alert-{alertId++}",
                    Type = "contagious_disease",
                    Title = "Phát hiện bệnh truyền nhiễm",
                    Description = $"Có {contagiousCases} ca bệnh truyền nhiễm được ghi nhận trong kỳ",
                    Severity = "danger"
                });
            }

            // Check for low stock medicines
            var lowStockMedicines = await _context.Medicines
                .AsNoTracking()
                .Where(m => m.StockQuantity <= m.WarningThreshold)
                .CountAsync(cancellationToken);

            if (lowStockMedicines > 0)
            {
                result.Add(new RiskAlertDto
                {
                    Id = $"alert-{alertId++}",
                    Type = "low_medicine_stock",
                    Title = "Thuốc tồn kho thấp",
                    Description = $"{lowStockMedicines} loại thuốc có tồn kho dưới mức cảnh báo",
                    Severity = "warning"
                });
            }

            // Check for incomplete vaccinations
            var incompleteVaccinations = await _context.StudentVaccinations
                .AsNoTracking()
                .Where(sv => classIds.Contains(sv.Student.ClassId) && sv.Status != "DONE")
                .CountAsync(cancellationToken);

            if (incompleteVaccinations > 0)
            {
                result.Add(new RiskAlertDto
                {
                    Id = $"alert-{alertId++}",
                    Type = "incomplete_vaccination",
                    Title = "Tiêm chủng chưa hoàn tất",
                    Description = $"{incompleteVaccinations} bản ghi tiêm chủng chưa được hoàn thành",
                    Severity = "warning"
                });
            }

            return result;
        }

        private async Task<List<TrendItemDto>> BuildTrendAsync(
            List<int> classIds,
            DateTime startDate,
            DateTime endDate,
            string reportType,
            CancellationToken cancellationToken)
        {
            var result = new List<TrendItemDto>();

            var dayDiff = (endDate - startDate).Days;
            var interval = dayDiff <= 7 ? 1 : dayDiff <= 30 ? 7 : 30;

            var query = _context.HealthVisits
                .AsNoTracking()
                .Where(v => classIds.Contains(v.Student.ClassId) && 
                            v.VisitDate >= startDate && 
                            v.VisitDate <= endDate);

            var visits = await query.ToListAsync(cancellationToken);

            var groupedByPeriod = visits
                .GroupBy(v => new DateTime(v.VisitDate.Year, v.VisitDate.Month, Math.Max(1, v.VisitDate.Day - (v.VisitDate.Day % interval))))
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in groupedByPeriod)
            {
                var label = group.Key.ToString("dd/MM");
                result.Add(new TrendItemDto
                {
                    Label = label,
                    Value = group.Count()
                });
            }

            return result;
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
                        col.Item().Text($"Thời gian xuất: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
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
    }
}