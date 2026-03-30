using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class Medicine
{
    [Key]
    [Column("MedicineID")]
    public int MedicineId { get; set; }

    [StringLength(150)]
    public string MedicineName { get; set; } = null!;

    public int StockQuantity { get; set; }

    public DateOnly? ExpiryDate { get; set; }

    [InverseProperty("Medicine")]
    public virtual ICollection<MedicineStockLog> MedicineStockLogs { get; set; } = new List<MedicineStockLog>();

    [InverseProperty("Medicine")]
    public virtual ICollection<VisitPrescription> VisitPrescriptions { get; set; } = new List<VisitPrescription>();
}
