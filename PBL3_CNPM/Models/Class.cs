using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

[Index("GradeId", Name = "IX_Classes_GradeID")]
public partial class Class
{
    [Key]
    [Column("ClassID")]
    public int ClassId { get; set; }

    [StringLength(50)]
    public string ClassName { get; set; } = null!;

    [Column("GradeID")]
    public int GradeId { get; set; }

    [ForeignKey("GradeId")]
    [InverseProperty("Classes")]
    public virtual Grade Grade { get; set; } = null!;

    [InverseProperty("Class")]
    public virtual ICollection<Student> Students { get; set; } = new List<Student>();
}
