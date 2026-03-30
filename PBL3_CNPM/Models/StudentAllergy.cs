using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class StudentAllergy
{
    [Key]
    [Column("RecordID")]
    public int RecordId { get; set; }

    [Column("StudentID")]
    [StringLength(20)]
    public string StudentId { get; set; } = null!;

    [Column("AllergyID")]
    public int AllergyId { get; set; }

    [StringLength(300)]
    public string? Note { get; set; }

    [ForeignKey("AllergyId")]
    [InverseProperty("StudentAllergies")]
    public virtual AllergyType Allergy { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("StudentAllergies")]
    public virtual Student Student { get; set; } = null!;
}
