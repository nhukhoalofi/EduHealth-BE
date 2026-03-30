using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class StudentVaccination
{
    [Key]
    [Column("RecordID")]
    public int RecordId { get; set; }

    [Column("StudentID")]
    [StringLength(20)]
    public string StudentId { get; set; } = null!;

    [Column("VaccineID")]
    public int VaccineId { get; set; }

    public bool Status { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("StudentVaccinations")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("VaccineId")]
    [InverseProperty("StudentVaccinations")]
    public virtual Vaccination Vaccine { get; set; } = null!;
}
