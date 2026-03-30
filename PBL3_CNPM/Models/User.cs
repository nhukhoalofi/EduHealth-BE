using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("Username", Name = "UQ_Users_Username", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("UserID")]
    public int UserId { get; set; }

    [StringLength(50)]
    public string Username { get; set; } = null!;

    [StringLength(20)]
    public string Role { get; set; } = null!;

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    public bool IsActive { get; set; }

    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(255)]
    public string Password { get; set; } = null!;

    [StringLength(255)]
    public string? Avatar { get; set; }

    [InverseProperty("Nurse")]
    public virtual ICollection<HealthVisit> HealthVisits { get; set; } = new List<HealthVisit>();

    [InverseProperty("User")]
    public virtual ICollection<MedicineStockLog> MedicineStockLogs { get; set; } = new List<MedicineStockLog>();

    [InverseProperty("ParentUser")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
