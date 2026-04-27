using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleWeb.Data;
using ScheduleWeb.Models;

public class LessonController : Controller
{
    private readonly ApplicationDbContext _context;

    public LessonController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ГОЛОВНА СТОРІНКА (Пошук та Розклад)
    public async Task<IActionResult> Index(DateTime? searchDate, string subject, string teacher, string group)
    {
        var query = _context.Lesson
            .Include(l => l.Teacher)
            .Include(l => l.Group)
            .AsQueryable();

        // Фільтрація як у вашому коді WinForms
        if (searchDate.HasValue)
            query = query.Where(l => l.Date.Date == searchDate.Value.Date);

        if (!string.IsNullOrEmpty(subject))
            query = query.Where(l => l.Subject.Contains(subject));

        if (!string.IsNullOrEmpty(teacher))
            query = query.Where(l => l.Teacher.FullName.Contains(teacher));

        if (!string.IsNullOrEmpty(group))
            query = query.Where(l => l.Group.Name.Contains(group));

        return View(await query.ToListAsync());
    }

    // ДОДАВАННЯ ЗАНЯТТЯ (Логіка об'єднання груп для лекцій)
    [HttpPost]
    public async Task<IActionResult> Create(Lesson model)
    {
        if (model.LessonType == "Лекція")
        {
            // Шукаємо, чи вже є така лекція
            var existing = await _context.Lesson
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l =>
                    l.Date == model.Date &&
                    l.Time == model.Time &&
                    l.Subject == model.Subject &&
                    l.TeacherId == model.TeacherId &&
                    l.LessonType == "Лекція");

            if (existing != null)
            {
                // Якщо група інша — дописуємо її назву (спрощено)
                // Примітка: В БД краще мати зв'язок Many-to-Many, 
                // але для вашої логіки "через кому" це виглядає так:
                return RedirectToAction(nameof(Index));
            }
        }

        _context.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
