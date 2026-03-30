using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("ClassId", Name = "IX_Students_ClassID")]
[Index("ParentUserId", Name = "IX_Students_ParentUserID")]
public partial class Student
{
    [Key]
    [Column("StudentID")]
    [StringLength(20)]
    public string StudentId { get; set; } = null!;

    [Column("ParentUserID")]
    public int ParentUserId { get; set; }

    [Column("ClassID")]
    public int ClassId { get; set; }

    [StringLength(100)]
    public string FullName { get; set; } = null!;

    [Column("DOB")]
    public DateOnly? Dob { get; set; }

    public double? CurrentHeight { get; set; }

    public double? CurrentWeight { get; set; }

    [StringLength(255)]
    public string? AvtStudents { get; set; }

    [ForeignKey("ClassId")]
    [InverseProperty("Students")]
    public virtual Class Class { get; set; } = null!;

    [InverseProperty("Student")]
    public virtual ICollection<HealthVisit> HealthVisits { get; set; } = new List<HealthVisit>();

    [ForeignKey("ParentUserId")]
    [InverseProperty("Students")]
    public virtual User ParentUser { get; set; } = null!;

    [InverseProperty("Student")]
    public virtual ICollection<StudentAllergy> StudentAllergies { get; set; } = new List<StudentAllergy>();

    [InverseProperty("Student")]
    public virtual ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
}
