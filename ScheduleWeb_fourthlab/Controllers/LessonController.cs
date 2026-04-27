using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleWeb.Data;
using ScheduleWeb.Models;
using ClosedXML.Excel;
using System.IO;

public class LessonController : Controller
{
    private readonly ApplicationDbContext _context;

    public LessonController(ApplicationDbContext context)
    {
        _context = context;
    }

    // ГОЛОВНА СТОРІНКА (Пошук та Розклад)
    public async Task<IActionResult> Index(DateTime? searchDate, string searchSubject, string searchTeacher, string searchGroup)
    {
        var query = _context.Lessons
            .Include(l => l.Teacher)
            .Include(l => l.Group)
            .AsQueryable();

        if (searchDate.HasValue)
            query = query.Where(l => l.Date.Date == searchDate.Value.Date);

        if (!string.IsNullOrEmpty(searchSubject))
            query = query.Where(l => l.Subject.Contains(searchSubject));

        if (!string.IsNullOrEmpty(searchTeacher))
            query = query.Where(l => l.Teacher.FullName.Contains(searchTeacher));

        if (!string.IsNullOrEmpty(searchGroup))
            query = query.Where(l => l.Group.Name.Contains(searchGroup));

        ViewBag.SearchDate = searchDate?.ToString("yyyy-MM-dd");
        ViewBag.SearchSubject = searchSubject;
        ViewBag.SearchTeacher = searchTeacher;
        ViewBag.SearchGroup = searchGroup;

        return View(await query.ToListAsync());
    }

    // СТОРІНКА ДОДАВАННЯ (GET)
    public async Task<IActionResult> Create()
    {
        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View();
    }

    // ДОДАВАННЯ ЗАНЯТТЯ
    [HttpPost]
    public async Task<IActionResult> Create(Lesson model)
    {
        if (model.LessonType == "Лекція")
        {
            var existing = await _context.Lessons
                .Include(l => l.Group)
                .FirstOrDefaultAsync(l =>
                    l.Date.Date == model.Date.Date &&
                    l.Time == model.Time &&
                    l.Subject == model.Subject &&
                    l.TeacherId == model.TeacherId &&
                    l.LessonType == "Лекція" &&
                    l.Room == model.Room &&
                    l.Week == model.Week);

            if (existing != null)
            {
                var newGroup = await _context.StudyGroups.FindAsync(model.StudyGroupId);
                if (newGroup != null && !existing.Group.Name.Contains(newGroup.Name))
                {
                    existing.Group.Name += ", " + newGroup.Name;
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
        }

        _context.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // ЕКСПОРТ В EXCEL
    public async Task<IActionResult> Export(DateTime? searchDate, string searchSubject, string searchTeacher, string searchGroup)
    {
        var query = _context.Lessons
            .Include(l => l.Teacher)
            .Include(l => l.Group)
            .AsQueryable();

        if (searchDate.HasValue)
            query = query.Where(l => l.Date.Date == searchDate.Value.Date);
        if (!string.IsNullOrEmpty(searchSubject))
            query = query.Where(l => l.Subject.Contains(searchSubject));
        if (!string.IsNullOrEmpty(searchTeacher))
            query = query.Where(l => l.Teacher.FullName.Contains(searchTeacher));
        if (!string.IsNullOrEmpty(searchGroup))
            query = query.Where(l => l.Group.Name.Contains(searchGroup));

        var lessons = await query.ToListAsync();

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Розклад");
            worksheet.Cell(1, 1).Value = "Дата";
            worksheet.Cell(1, 2).Value = "Предмет";
            worksheet.Cell(1, 3).Value = "Час";
            worksheet.Cell(1, 4).Value = "Аудиторія";
            worksheet.Cell(1, 5).Value = "Група";
            worksheet.Cell(1, 6).Value = "Тиждень";
            worksheet.Cell(1, 7).Value = "Викладач";
            worksheet.Cell(1, 8).Value = "Тип заняття";

            worksheet.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < lessons.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = lessons[i].Date.ToString("dd.MM.yyyy");
                worksheet.Cell(i + 2, 2).Value = lessons[i].Subject;
                worksheet.Cell(i + 2, 3).Value = lessons[i].Time;
                worksheet.Cell(i + 2, 4).Value = lessons[i].Room;
                worksheet.Cell(i + 2, 5).Value = lessons[i].Group?.Name;
                worksheet.Cell(i + 2, 6).Value = lessons[i].Week;
                worksheet.Cell(i + 2, 7).Value = lessons[i].Teacher?.FullName;
                worksheet.Cell(i + 2, 8).Value = lessons[i].LessonType;
            }

            worksheet.Columns().AdjustToContents();

            using (var stream = new MemoryStream())
            {
                workbook.SaveAs(stream);
                var content = stream.ToArray();
                return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Schedule.xlsx");
            }
        }
    }

    // ІМПОРТ З EXCEL
    [HttpPost]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

        using (var stream = new MemoryStream())
        {
            await file.CopyToAsync(stream);
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

                foreach (var row in rows)
                {
                    string dateStr = row.Cell(1).GetString();
                    string subject = row.Cell(2).GetString();
                    string time = row.Cell(3).GetString();
                    string room = row.Cell(4).GetString();
                    string groupName = row.Cell(5).GetString();
                    int week = row.Cell(6).GetValue<int>();
                    string teacherName = row.Cell(7).GetString();
                    string lessonType = row.Cell(8).GetString();

                    DateTime.TryParseExact(dateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date);

                    var group = await _context.StudyGroups.FirstOrDefaultAsync(g => g.Name == groupName);
                    if (group == null && !string.IsNullOrWhiteSpace(groupName))
                    {
                        group = new StudyGroup { Name = groupName };
                        _context.StudyGroups.Add(group);
                        await _context.SaveChangesAsync();
                    }

                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.FullName == teacherName);
                    if (teacher == null && !string.IsNullOrWhiteSpace(teacherName))
                    {
                        teacher = new Teacher { FullName = teacherName };
                        _context.Teachers.Add(teacher);
                        await _context.SaveChangesAsync();
                    }

                    if (lessonType == "Лекція")
                    {
                        var existing = await _context.Lessons.FirstOrDefaultAsync(l =>
                            l.Date == date && l.Subject == subject && l.Time == time && 
                            l.Room == room && l.Week == week && 
                            l.TeacherId == teacher.Id && l.LessonType == "Лекція");

                        if (existing != null)
                        {
                            if (!existing.Group.Name.Contains(group.Name))
                            {
                                existing.Group.Name += ", " + group.Name;
                            }
                            continue;
                        }
                    }

                    _context.Lessons.Add(new Lesson
                    {
                        Date = date,
                        Subject = subject,
                        Time = time,
                        Room = room,
                        StudyGroupId = group.Id,
                        Week = week,
                        TeacherId = teacher.Id,
                        LessonType = lessonType
                    });
                }
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index));
    }

    // СЛОВНИКИ: ГРУПИ ТА ВИКЛАДАЧІ
    public async Task<IActionResult> Dictionaries()
    {
        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> AddTeacher(string fullName)
    {
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            _context.Teachers.Add(new Teacher { FullName = fullName });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Dictionaries));
    }

    [HttpPost]
    public async Task<IActionResult> AddGroup(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
        {
            _context.StudyGroups.Add(new StudyGroup { Name = name });
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Dictionaries));
    }
}
