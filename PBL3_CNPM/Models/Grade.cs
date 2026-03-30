using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class Grade
{
    [Key]
    [Column("GradeID")]
    public int GradeId { get; set; }

    [StringLength(20)]
    public string GradeName { get; set; } = null!;

    [InverseProperty("Grade")]
    public virtual ICollection<Class> Classes { get; set; } = new List<Class>();
}
