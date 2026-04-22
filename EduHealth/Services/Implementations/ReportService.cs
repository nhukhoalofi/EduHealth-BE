using EduHealth.Data;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Reports;
using EduHealth.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

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
            return new AdminReportDashboardDto
            {
                TotalStudents = await _context.Students.CountAsync(cancellationToken),
                TotalClasses = await _context.SchoolClasses.CountAsync(cancellationToken),
                TotalHealthVisits = await _context.HealthVisits.CountAsync(cancellationToken),
                TotalVaccinationCampaigns = await _context.VaccinationCampaigns.CountAsync(cancellationToken),
                MedicineWarningsCount = await _context.Medicines.CountAsync(m => m.StockQuantity <= m.WarningThreshold, cancellationToken),
                NotificationsSentCount = await _context.Notifications.CountAsync(cancellationToken),
                SystemLogsCount = await _context.SystemLogs.CountAsync(cancellationToken),
                HealthSummary = new HealthSummaryDto { Healthy = 850, Sick = 45, Chronic = 15 } // Mock metric
            };
        }

        public async Task<ReportClassDto?> GetClassReportAsync(int classId, CancellationToken cancellationToken)
        {
            var classEntity = await _context.SchoolClasses.AsNoTracking().FirstOrDefaultAsync(c => c.ClassId == classId, cancellationToken);
            if (classEntity == null) return null;

            return new ReportClassDto
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                TeacherName = "Nguyễn Văn A (CN)", // Mock implementation for contract
                TotalStudents = await _context.Students.CountAsync(s => s.ClassId == classId, cancellationToken),
                HealthBreakdown = new HealthSummaryDto { Healthy = 35, Sick = 3, Chronic = 2 },
                RiskList = new List<RiskStudentDto>
                {
                    new RiskStudentDto { StudentId = 1001, FullName = "Trần B", RiskLevel = "HIGH", Description = "Chưa tiêm Sởi" }
                }
            };
        }

        public async Task<ExportResponseDto> ExportReportAsync(ExportRequestDto request, CancellationToken cancellationToken)
        {
            return new ExportResponseDto
            {
                FileName = $"Report_{request.Format}_{DateTime.UtcNow:yyyyMMdd}.{request.Format}",
                DownloadUrl = $"https://s3.eduhealth.com/reports/mock-download-link.{request.Format}",
                ExpiresAt = DateTime.UtcNow.AddHours(24)
            };
        }

        public async Task<DirectiveResponseDto> CreateDirectiveAsync(DirectiveRequestDto request, CancellationToken cancellationToken)
        {
            return new DirectiveResponseDto
            {
                DirectiveId = new Random().Next(1000, 9999),
                Status = "SENT",
                CreatedAt = DateTime.UtcNow
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

        public async Task<NurseReportDashboardDto> GetNurseDashboardAsync(int nurseId, CancellationToken cancellationToken)
        {
            return new NurseReportDashboardDto
            {
                TotalAssignedStudents = 320,
                TotalAssignedClasses = 8,
                TodayHealthVisits = 12,
                ExpiringMedicinesCount = await _context.Medicines.CountAsync(m => m.StockQuantity <= m.WarningThreshold, cancellationToken),
                PendingVaccinationsCount = 8,
                HealthSummary = new HealthSummaryDto { Healthy = 300, Sick = 15, Chronic = 5 }
            };
        }

        public async Task<AdminNotificationPreviewResponseDto> PreviewNotificationsAsync(AdminNotificationPreviewRequestDto request, CancellationToken cancellationToken)
        {
            var recipients = new List<NotificationRecipientPreviewDto>();

            if (request.ClassId.HasValue)
            {
                // Lấy UserIds của toàn bộ Học sinh trong Class
                var studentUserIds = await _context.Students
                    .Where(s => s.ClassId == request.ClassId.Value)
                    .Select(s => s.UserId)
                    .ToListAsync(cancellationToken);

                var users = await _context.Users
                    .Where(u => studentUserIds.Contains(u.UserId))
                    .Select(u => new NotificationRecipientPreviewDto { UserId = u.UserId, FullName = u.FullName, Role = u.Role })
                    .ToListAsync(cancellationToken);

                recipients.AddRange(users);
            }
            else if (request.RecipientUserIds != null && request.RecipientUserIds.Any())
            {
                var users = await _context.Users
                    .Where(u => request.RecipientUserIds.Contains(u.UserId))
                    .Select(u => new NotificationRecipientPreviewDto { UserId = u.UserId, FullName = u.FullName, Role = u.Role })
                    .ToListAsync(cancellationToken);

                recipients.AddRange(users);
            }

            return new AdminNotificationPreviewResponseDto
            {
                TotalRecipients = recipients.Count,
                Recipients = recipients
            };
        }

        public async Task<AdminNotificationResponseDto> SendNotificationsAsync(AdminNotificationRequestDto request, int adminId, CancellationToken cancellationToken)
        {
            var userIdsToNotify = new HashSet<int>();

            if (request.ClassId.HasValue)
            {
                var studentUserIds = await _context.Students
                    .Where(s => s.ClassId == request.ClassId.Value)
                    .Select(s => s.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var id in studentUserIds) userIdsToNotify.Add(id);
            }
            else if (request.RecipientUserIds != null && request.RecipientUserIds.Any())
            {
                foreach (var id in request.RecipientUserIds) userIdsToNotify.Add(id);
            }

            var notification = new Notification
            {
                Title = request.Title,
                Content = request.Content,
                Type = request.Type,
                CreatedByUserId = adminId,
                CreatedAt = DateTime.UtcNow,
                ClassId = request.ClassId
            };

            foreach (var userId in userIdsToNotify)
            {
                notification.Recipients.Add(new NotificationRecipient
                {
                    UserId = userId,
                    IsRead = false,
                    SentAt = DateTime.UtcNow,
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
    }
}