using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("MedicineId", Name = "IX_StockLogs_MedicineID")]
[Index("UserId", Name = "IX_StockLogs_UserID")]
public partial class MedicineStockLog
{
    [Key]
    [Column("LogID")]
    public int LogId { get; set; }

    [Column("MedicineID")]
    public int MedicineId { get; set; }

    [Column("UserID")]
    public int UserId { get; set; }

    public int ChangeQty { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("MedicineId")]
    [InverseProperty("MedicineStockLogs")]
    public virtual Medicine Medicine { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("MedicineStockLogs")]
    public virtual User User { get; set; } = null!;
}
