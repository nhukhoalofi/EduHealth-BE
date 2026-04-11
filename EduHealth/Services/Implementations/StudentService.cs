using ClosedXML.Excel;
using EduHealth.Data.Entities;
using EduHealth.DTOs.Students;
using EduHealth.Helpers;
using EduHealth.Repositories.Interfaces;
using EduHealth.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace EduHealth.Services.Implementations
{
    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _studentRepository;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConfiguration _configuration;

        public StudentService(IStudentRepository studentRepository, ICloudinaryService cloudinaryService, IConfiguration configuration)
        {
            _studentRepository = studentRepository;
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message, string? Field, string? ImageUrl)> UpdateStudentImageAsync(
            int studentUserId,
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file is null || file.Length == 0)
            {
                return (false, "Vui lòng chọn file hình ảnh.", "file", null);
            }

            // basic validation (avoid non-image uploads)
            if (string.IsNullOrWhiteSpace(file.ContentType) || !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "File không đúng định dạng hình ảnh.", "file", null);
            }

            var student = await _studentRepository.GetByUserIdAsync(studentUserId, cancellationToken);
            if (student is null)
            {
                return (false, "Không tìm thấy học sinh.", "id", null);
            }

            var folderRoot = _configuration["Cloudinary:Folder"];
            var folder = string.IsNullOrWhiteSpace(folderRoot)
                ? "eduhealth/students"
                : $"{folderRoot.Trim().TrimEnd('/')}/students";

            try
            {
                var (url, publicId) = await _cloudinaryService.UploadImageAsync(file, folder, cancellationToken);

                student.User.Avatar = url;
                student.User.UpdatedAt = DateTime.UtcNow;
                _studentRepository.UpdateUser(student.User);
                await _studentRepository.SaveChangesAsync(cancellationToken);

                return (true, "Cập nhật hình ảnh học sinh thành công.", null, url);
            }
            catch
            {
                return (false, "Upload hình ảnh thất bại.", "file", null);
            }
        }

        public async Task<StudentListResultDto> GetStudentsAsync(StudentListQueryDto query, CancellationToken cancellationToken = default)
        {
            var page = query.Page <= 0 ? 1 : query.Page;
            var pageSize = query.PageSize <= 0 ? 20 : Math.Min(query.PageSize, 100);

            var (items, total) = await _studentRepository.GetPagedAsync(
                query.Search,
                query.ClassId,
                query.IsActive,
                page,
                pageSize,
                cancellationToken);

            return new StudentListResultDto
            {
                Items = items.Select(MapListItem).ToList(),
                Total = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = total == 0 ? 0 : (int)Math.Ceiling((double)total / pageSize)
            };
        }

        public async Task<StudentDetailDto?> GetStudentByIdAsync(int userId, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken);
            return student is null ? null : MapDetail(student);
        }

        public async Task<StudentCreateResultDto> CreateStudentAsync(StudentCreateRequestDto request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return new StudentCreateResultDto { Success = false, Field = "fullName", Message = "Họ tên không được rỗng." };
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return new StudentCreateResultDto { Success = false, Field = "email", Message = "Email không được rỗng." };
            }

            if (string.IsNullOrWhiteSpace(request.Phone))
            {
                return new StudentCreateResultDto { Success = false, Field = "phone", Message = "Số điện thoại không được rỗng." };
            }

            if (request.ClassId <= 0)
            {
                return new StudentCreateResultDto { Success = false, Field = "classId", Message = "Lớp học không hợp lệ." };
            }

            if (!await _studentRepository.ClassExistsAsync(request.ClassId, cancellationToken))
            {
                return new StudentCreateResultDto { Success = false, Field = "classId", Message = "Không tìm thấy lớp học." };
            }

            var email = request.Email.Trim();
            var phone = request.Phone.Trim();

            if (await _studentRepository.AnyEmailAsync(email, null, cancellationToken))
            {
                return new StudentCreateResultDto { Success = false, Field = "email", Message = "Email đã tồn tại." };
            }

            if (await _studentRepository.AnyPhoneAsync(phone, null, cancellationToken))
            {
                return new StudentCreateResultDto { Success = false, Field = "phone", Message = "Số điện thoại đã tồn tại." };
            }

            var password = string.IsNullOrWhiteSpace(request.Password) ? "123456Aa@" : request.Password.Trim();

            var now = DateTime.UtcNow;

            var user = new User
            {
                Code = "USR_TMP",
                Username = $"HS{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
                FullName = request.FullName.Trim(),
                Email = email,
                Phone = phone,
                Gender = request.Gender?.Trim(),
                Role = "STUDENT",
                IsActive = true,
                Status = "ACTIVE",
                Avatar = null,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordHash = PasswordHelper.HashPassword(password)
            };

            var student = new Student
            {
                User = user,
                Code = "STD_TMP",
                ClassId = request.ClassId,
                FullName = request.FullName.Trim(),
                DateOfBirth = request.DateOfBirth.Date,
                CurrentHeight = request.CurrentHeight,
                CurrentWeight = request.CurrentWeight,
                MedicalHistoryNotes = request.MedicalHistoryNotes?.Trim(),
                Guardian = request.Guardian?.Trim(),
                Phone = phone
            };

            await _studentRepository.AddAsync(user, student, cancellationToken);
            await _studentRepository.SaveChangesAsync(cancellationToken);

            // generate stable codes after UserId is available
            user.Code = $"USR{user.UserId:D3}";
            user.Username = $"HS{user.UserId:D3}";
            user.UpdatedAt = DateTime.UtcNow;
            _studentRepository.UpdateUser(user);

            student.UserId = user.UserId;
            student.Code = $"STD{user.UserId:D3}";
            _studentRepository.Update(student);
            await _studentRepository.SaveChangesAsync(cancellationToken);

            var saved = await _studentRepository.GetByUserIdAsync(user.UserId, cancellationToken);

            return new StudentCreateResultDto
            {
                Success = true,
                Message = "Tạo học sinh thành công.",
                Data = MapDetail(saved!)
            };
        }

        public async Task<StudentOperationResultDto> UpdateStudentAsync(int userId, StudentUpdateRequestDto request, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken);

            if (student is null)
            {
                return new StudentOperationResultDto { Success = false, Message = "Không tìm thấy học sinh." };
            }

            if (request.ClassId.HasValue)
            {
                if (request.ClassId.Value <= 0 || !await _studentRepository.ClassExistsAsync(request.ClassId.Value, cancellationToken))
                {
                    return new StudentOperationResultDto { Success = false, Field = "classId", Message = "Lớp học không hợp lệ." };
                }

                student.ClassId = request.ClassId.Value;
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var newEmail = request.Email.Trim();

                if (await _studentRepository.AnyEmailAsync(newEmail, userId, cancellationToken))
                {
                    return new StudentOperationResultDto { Success = false, Field = "email", Message = "Email đã tồn tại." };
                }

                student.User.Email = newEmail;
            }

            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                var newPhone = request.Phone.Trim();

                if (await _studentRepository.AnyPhoneAsync(newPhone, userId, cancellationToken))
                {
                    return new StudentOperationResultDto { Success = false, Field = "phone", Message = "Số điện thoại đã tồn tại." };
                }

                student.User.Phone = newPhone;
                student.Phone = newPhone;
            }

            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                var fullName = request.FullName.Trim();
                student.FullName = fullName;
                student.User.FullName = fullName;
            }

            if (request.DateOfBirth.HasValue)
            {
                student.DateOfBirth = request.DateOfBirth.Value.Date;
            }

            if (request.CurrentHeight.HasValue)
            {
                student.CurrentHeight = request.CurrentHeight.Value;
            }

            if (request.CurrentWeight.HasValue)
            {
                student.CurrentWeight = request.CurrentWeight.Value;
            }

            if (request.MedicalHistoryNotes is not null)
            {
                student.MedicalHistoryNotes = request.MedicalHistoryNotes.Trim();
            }

            if (request.Guardian is not null)
            {
                student.Guardian = request.Guardian.Trim();
            }

            if (request.Gender is not null)
            {
                student.User.Gender = request.Gender.Trim();
            }

            _studentRepository.Update(student);
            _studentRepository.UpdateUser(student.User);
            await _studentRepository.SaveChangesAsync(cancellationToken);

            return new StudentOperationResultDto
            {
                Success = true,
                Message = "Cập nhật học sinh thành công."
            };
        }

        public async Task<StudentOperationResultDto> DeleteStudentAsync(int userId, CancellationToken cancellationToken = default)
        {
            var student = await _studentRepository.GetByUserIdAsync(userId, cancellationToken);

            if (student is null)
            {
                return new StudentOperationResultDto { Success = false, Message = "Không tìm thấy học sinh." };
            }

            if (!student.User.IsActive)
            {
                return new StudentOperationResultDto { Success = true, Message = "Học sinh đã được vô hiệu hóa trước đó." };
            }

            student.User.IsActive = false;

            _studentRepository.UpdateUser(student.User);
            await _studentRepository.SaveChangesAsync(cancellationToken);

            return new StudentOperationResultDto
            {
                Success = true,
                Message = "Xóa mềm học sinh thành công."
            };
        }

        public async Task<StudentImportResultDto> ImportStudentsAsync(StudentImportRequestDto request, CancellationToken cancellationToken = default)
        {
            var result = new StudentImportResultDto();

            if (request.File is null || request.File.Length == 0)
            {
                result.Errors.Add(new StudentImportErrorDto { RowNumber = 0, Message = "File import không hợp lệ." });
                return result;
            }

            var rows = await ParseRowsAsync(request.File, cancellationToken);
            result.TotalRows = rows.Count;

            var pendingUsers = new List<User>();
            var pendingStudents = new List<Student>();

            var batchEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var batchPhones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var classCache = new Dictionary<int, bool>();

            foreach (var row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.FullName) ||
                    string.IsNullOrWhiteSpace(row.Email) ||
                    string.IsNullOrWhiteSpace(row.Phone) ||
                    row.ClassId <= 0 ||
                    row.DateOfBirth == default)
                {
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = "Thiếu dữ liệu bắt buộc (fullName, email, phone, classId, dateOfBirth)."
                    });
                    continue;
                }

                if (!classCache.TryGetValue(row.ClassId, out var classExists))
                {
                    classExists = await _studentRepository.ClassExistsAsync(row.ClassId, cancellationToken);
                    classCache[row.ClassId] = classExists;
                }

                if (!classExists)
                {
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = $"ClassId {row.ClassId} không tồn tại."
                    });
                    continue;
                }

                var normalizedEmail = row.Email.Trim();
                var normalizedPhone = row.Phone.Trim();

                if (batchEmails.Contains(normalizedEmail) || await _studentRepository.AnyEmailAsync(normalizedEmail, null, cancellationToken))
                {
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = "Email đã tồn tại."
                    });
                    continue;
                }

                if (batchPhones.Contains(normalizedPhone) || await _studentRepository.AnyPhoneAsync(normalizedPhone, null, cancellationToken))
                {
                    result.Errors.Add(new StudentImportErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = "Số điện thoại đã tồn tại."
                    });
                    continue;
                }

                batchEmails.Add(normalizedEmail);
                batchPhones.Add(normalizedPhone);

                var password = string.IsNullOrWhiteSpace(row.Password) ? "123456Aa@" : row.Password.Trim();

                var user = new User
                {
                    FullName = row.FullName.Trim(),
                    Email = normalizedEmail,
                    Phone = normalizedPhone,
                    Gender = row.Gender?.Trim(),
                    Role = "STUDENT",
                    IsActive = true,
                    Avatar = null,
                    PasswordHash = PasswordHelper.HashPassword(password)
                };

                var student = new Student
                {
                    User = user,
                    ClassId = row.ClassId,
                    FullName = row.FullName.Trim(),
                    DateOfBirth = row.DateOfBirth.Date,
                    CurrentHeight = row.CurrentHeight,
                    CurrentWeight = row.CurrentWeight,
                    MedicalHistoryNotes = row.MedicalHistoryNotes?.Trim(),
                    Guardian = row.Guardian?.Trim(),
                    Phone = normalizedPhone
                };

                pendingUsers.Add(user);
                pendingStudents.Add(student);
                result.SuccessCount++;
            }

            if (pendingUsers.Count > 0)
            {
                await _studentRepository.AddRangeAsync(pendingUsers, pendingStudents, cancellationToken);
                await _studentRepository.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        private static StudentListItemDto MapListItem(Student student)
        {
            return new StudentListItemDto
            {
                UserId = student.UserId,
                ImageUrl = student.User.Avatar,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                ClassId = student.ClassId,
                ClassName = student.Class.ClassName,
                Email = student.User.Email,
                Phone = student.User.Phone,
                Guardian = student.Guardian,
                CurrentHeight = student.CurrentHeight,
                CurrentWeight = student.CurrentWeight,
                IsActive = student.User.IsActive
            };
        }

        private static StudentDetailDto MapDetail(Student student)
        {
            return new StudentDetailDto
            {
                UserId = student.UserId,
                ImageUrl = student.User.Avatar,
                ClassId = student.ClassId,
                ClassName = student.Class.ClassName,
                FullName = student.FullName,
                DateOfBirth = student.DateOfBirth,
                CurrentHeight = student.CurrentHeight,
                CurrentWeight = student.CurrentWeight,
                MedicalHistoryNotes = student.MedicalHistoryNotes,
                Guardian = student.Guardian,
                Phone = student.User.Phone,
                Email = student.User.Email,
                Gender = student.User.Gender,
                IsActive = student.User.IsActive
            };
        }

        private static async Task<List<StudentImportRow>> ParseRowsAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            return ext switch
            {
                ".xlsx" => await ParseXlsxAsync(file, cancellationToken),
                ".csv" => await ParseCsvAsync(file, cancellationToken),
                _ => new List<StudentImportRow>()
            };
        }

        private static async Task<List<StudentImportRow>> ParseCsvAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var rows = new List<StudentImportRow>();

            await using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);

            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                return rows;
            }

            var headers = headerLine.Split(',').Select(x => x.Trim()).ToArray();
            var map = BuildHeaderMap(headers);

            var lineNumber = 1;
            while (!reader.EndOfStream)
            {
                lineNumber++;
                var line = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var cells = line.Split(',');
                rows.Add(ParseImportRow(cells, map, lineNumber));
            }

            return rows;
        }

        private static async Task<List<StudentImportRow>> ParseXlsxAsync(IFormFile file, CancellationToken cancellationToken)
        {
            var rows = new List<StudentImportRow>();

            await using var stream = file.OpenReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;

            using var workbook = new XLWorkbook(ms);
            var ws = workbook.Worksheets.FirstOrDefault();
            if (ws is null)
            {
                return rows;
            }

            var firstRow = ws.FirstRowUsed();
            if (firstRow is null)
            {
                return rows;
            }

            var headers = firstRow.CellsUsed().Select(c => c.GetString().Trim()).ToArray();
            var map = BuildHeaderMap(headers);

            var lastRow = ws.LastRowUsed()?.RowNumber() ?? firstRow.RowNumber();
            for (var r = firstRow.RowNumber() + 1; r <= lastRow; r++)
            {
                var row = ws.Row(r);
                if (row.CellsUsed().Any() == false)
                {
                    continue;
                }

                var maxCol = headers.Length;
                var cells = new string[maxCol];

                for (var c = 1; c <= maxCol; c++)
                {
                    cells[c - 1] = row.Cell(c).GetString().Trim();
                }

                rows.Add(ParseImportRow(cells, map, r));
            }

            return rows;
        }

        private static Dictionary<string, int> BuildHeaderMap(string[] headers)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < headers.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(headers[i]))
                {
                    map[headers[i].Trim()] = i;
                }
            }

            return map;
        }

        private static StudentImportRow ParseImportRow(string[] cells, Dictionary<string, int> map, int rowNumber)
        {
            string? Get(params string[] names)
            {
                foreach (var name in names)
                {
                    if (map.TryGetValue(name, out var idx) && idx >= 0 && idx < cells.Length)
                    {
                        return cells[idx]?.Trim();
                    }
                }

                return null;
            }

            var dobText = Get("dateOfBirth", "dob", "ngaySinh");
            var dateOfBirth = ParseDate(dobText);

            var classIdText = Get("classId", "lopId");
            _ = int.TryParse(classIdText, out var classId);

            _ = float.TryParse(Get("currentHeight", "chieuCao"), NumberStyles.Any, CultureInfo.InvariantCulture, out var height);
            _ = float.TryParse(Get("currentWeight", "canNang"), NumberStyles.Any, CultureInfo.InvariantCulture, out var weight);

            return new StudentImportRow
            {
                RowNumber = rowNumber,
                FullName = Get("fullName", "hoTen") ?? string.Empty,
                DateOfBirth = dateOfBirth,
                ClassId = classId,
                Phone = Get("phone", "soDienThoai") ?? string.Empty,
                Email = Get("email") ?? string.Empty,
                Guardian = Get("guardian", "nguoiGiamHo"),
                CurrentHeight = height,
                CurrentWeight = weight,
                MedicalHistoryNotes = Get("medicalHistoryNotes", "tienSuBenh"),
                Gender = Get("gender", "gioiTinh"),
                Password = Get("password", "matKhau")
            };
        }

        private static DateTime ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default;
            }

            var formats = new[]
            {
                "yyyy-MM-dd",
                "dd/MM/yyyy",
                "MM/dd/yyyy"
            };

            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt;
            }

            return DateTime.TryParse(value, out var normal) ? normal : default;
        }

        private sealed class StudentImportRow
        {
            public int RowNumber { get; set; }
            public string FullName { get; set; } = string.Empty;
            public DateTime DateOfBirth { get; set; }
            public int ClassId { get; set; }
            public string Phone { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Guardian { get; set; }
            public float CurrentHeight { get; set; }
            public float CurrentWeight { get; set; }
            public string? MedicalHistoryNotes { get; set; }
            public string? Gender { get; set; }
            public string? Password { get; set; }
        }
    }
}