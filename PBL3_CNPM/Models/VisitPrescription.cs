using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("MedicineId", Name = "IX_VisitPrescriptions_MedicineID")]
[Index("VisitId", Name = "IX_VisitPrescriptions_VisitID")]
public partial class VisitPrescription
{
    [Key]
    [Column("PrescriptionID")]
    public int PrescriptionId { get; set; }

    [Column("VisitID")]
    public int VisitId { get; set; }

    [Column("MedicineID")]
    public int MedicineId { get; set; }

    public int Quantity { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    [ForeignKey("MedicineId")]
    [InverseProperty("VisitPrescriptions")]
    public virtual Medicine Medicine { get; set; } = null!;

    [ForeignKey("VisitId")]
    [InverseProperty("VisitPrescriptions")]
    public virtual HealthVisit Visit { get; set; } = null!;
}
