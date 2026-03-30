using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EduHealthSystem.Models;

public partial class SystemAlert
{
    [Key]
    [Column("AlertID")]
    public int AlertId { get; set; }

    [StringLength(30)]
    public string AlertType { get; set; } = null!;

    [StringLength(500)]
    public string Message { get; set; } = null!;

    [Column(TypeName = "datetime")]
    public DateTime CreatedAt { get; set; }

    public bool IsRead { get; set; }
}
