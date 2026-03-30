using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduHealthSystem.Data;   // sửa đúng namespace
using EduHealthSystem.Models; // nếu models có namespace này

public class TestDbController : Controller
{
    private readonly EduHealthDbContext _db;

    public TestDbController(EduHealthDbContext db)
    {
        _db = db;
    }

    // /TestDb/Students
    public async Task<IActionResult> Students()
    {
        var list = await _db.Students
            .OrderBy(x => x.StudentId)
            .Take(20)
            .ToListAsync();

        return View(list);
    }
}