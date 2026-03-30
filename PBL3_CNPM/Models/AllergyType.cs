using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class AllergyType
{
    [Key]
    [Column("AllergyID")]
    public int AllergyId { get; set; }

    [StringLength(100)]
    public string AllergyName { get; set; } = null!;

    [StringLength(20)]
    public string? Severity { get; set; }

    [InverseProperty("Allergy")]
    public virtual ICollection<StudentAllergy> StudentAllergies { get; set; } = new List<StudentAllergy>();
}
