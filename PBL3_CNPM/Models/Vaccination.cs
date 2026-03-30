using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class Vaccination
{
    [Key]
    [Column("VaccineID")]
    public int VaccineId { get; set; }

    [StringLength(100)]
    public string Name { get; set; } = null!;

    [InverseProperty("Vaccine")]
    public virtual ICollection<StudentVaccination> StudentVaccinations { get; set; } = new List<StudentVaccination>();
}
