using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("NurseId", Name = "IX_HealthVisits_NurseID")]
[Index("StudentId", Name = "IX_HealthVisits_StudentID")]
public partial class HealthVisit
{
    [Key]
    [Column("VisitID")]
    public int VisitId { get; set; }

    [Column("StudentID")]
    [StringLength(20)]
    public string StudentId { get; set; } = null!;

    [Column("NurseID")]
    public int NurseId { get; set; }

    [Column(TypeName = "datetime")]
    public DateTime VisitDate { get; set; }

    [StringLength(500)]
    public string? Symptoms { get; set; }

    [StringLength(500)]
    public string? Diagnosis { get; set; }

    public double? MeasuredHeight { get; set; }

    public double? MeasuredWeight { get; set; }

    [Column("DiseaseID")]
    public int? DiseaseId { get; set; }

    [ForeignKey("DiseaseId")]
    [InverseProperty("HealthVisits")]
    public virtual DiseaseType? Disease { get; set; }

    [ForeignKey("NurseId")]
    [InverseProperty("HealthVisits")]
    public virtual User Nurse { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("HealthVisits")]
    public virtual Student Student { get; set; } = null!;

    [InverseProperty("Visit")]
    public virtual ICollection<VisitPrescription> VisitPrescriptions { get; set; } = new List<VisitPrescription>();
}
