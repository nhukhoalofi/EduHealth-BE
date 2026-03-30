using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Table("DiseaseType")]
public partial class DiseaseType
{
    [Key]
    [Column("DiseaseID")]
    public int DiseaseId { get; set; }

    [StringLength(150)]
    public string DiseaseName { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsContagious { get; set; }

    [StringLength(500)]
    public string? StandardTreatment { get; set; }

    [InverseProperty("Disease")]
    public virtual ICollection<HealthVisit> HealthVisits { get; set; } = new List<HealthVisit>();
}
