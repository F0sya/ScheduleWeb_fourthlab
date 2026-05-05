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

    public async Task<IActionResult> Index(string searchDate, string searchSubject, string searchTeacher, string searchGroup, string searchTime, string searchRoom, int? searchWeek, string searchLessonType)
    {
        var query = _context.Lessons
            .Include(l => l.Teacher)
            .Include(l => l.Groups)
            .AsQueryable();

        DateTime? parsedDate = null;
        if (!string.IsNullOrEmpty(searchDate))
        {
            if (searchDate.ToLower() == "сьогодні") parsedDate = DateTime.Today;
            else if (searchDate.ToLower() == "завтра") parsedDate = DateTime.Today.AddDays(1);
            else if (DateTime.TryParseExact(searchDate, new[] { "dd.MM.yyyy", "yyyy-MM-dd", "MM/dd/yyyy" }, null, System.Globalization.DateTimeStyles.None, out var d))
            {
                parsedDate = d;
            }
        }

        if (parsedDate.HasValue)
            query = query.Where(l => l.Date.Date == parsedDate.Value.Date);

        if (!string.IsNullOrEmpty(searchSubject))
            query = query.Where(l => l.Subject.Contains(searchSubject));

        if (!string.IsNullOrEmpty(searchTeacher))
            query = query.Where(l => l.Teacher.FullName.Contains(searchTeacher));

        if (!string.IsNullOrEmpty(searchGroup))
            query = query.Where(l => l.Groups.Any(g => g.Name.Contains(searchGroup)));

        if (!string.IsNullOrEmpty(searchTime))
            query = query.Where(l => l.Time.Contains(searchTime));

        if (!string.IsNullOrEmpty(searchRoom))
            query = query.Where(l => l.Room.Contains(searchRoom));

        if (searchWeek.HasValue)
            query = query.Where(l => l.Week == searchWeek.Value);

        if (!string.IsNullOrEmpty(searchLessonType))
            query = query.Where(l => l.LessonType.Contains(searchLessonType));

        ViewBag.SearchDate = searchDate;
        ViewBag.SearchSubject = searchSubject;
        ViewBag.SearchTeacher = searchTeacher;
        ViewBag.SearchGroup = searchGroup;
        ViewBag.SearchTime = searchTime;
        ViewBag.SearchRoom = searchRoom;
        ViewBag.SearchWeek = searchWeek;
        ViewBag.SearchLessonType = searchLessonType;

        return View(await query.ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Lesson model, int[] selectedGroups)
    {
        if (selectedGroups != null && selectedGroups.Length > 0)
        {
            var groupIds = selectedGroups.ToList();
            model.Groups = await _context.StudyGroups.Where(g => groupIds.Contains(g.Id)).ToListAsync();
        }

        _context.Add(model);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Export(string searchDate, string searchSubject, string searchTeacher, string searchGroup, string searchTime, string searchRoom, int? searchWeek, string searchLessonType)
    {
        var query = _context.Lessons
            .Include(l => l.Teacher)
            .Include(l => l.Groups)
            .AsQueryable();

        DateTime? parsedDate = null;
        if (!string.IsNullOrEmpty(searchDate))
        {
            if (searchDate.ToLower() == "сьогодні") parsedDate = DateTime.Today;
            else if (searchDate.ToLower() == "завтра") parsedDate = DateTime.Today.AddDays(1);
            else if (DateTime.TryParseExact(searchDate, new[] { "dd.MM.yyyy", "yyyy-MM-dd", "MM/dd/yyyy" }, null, System.Globalization.DateTimeStyles.None, out var d))
            {
                parsedDate = d;
            }
        }

        if (parsedDate.HasValue)
            query = query.Where(l => l.Date.Date == parsedDate.Value.Date);
        if (!string.IsNullOrEmpty(searchSubject))
            query = query.Where(l => l.Subject.Contains(searchSubject));
        if (!string.IsNullOrEmpty(searchTeacher))
            query = query.Where(l => l.Teacher.FullName.Contains(searchTeacher));
        if (!string.IsNullOrEmpty(searchGroup))
            query = query.Where(l => l.Groups.Any(g => g.Name.Contains(searchGroup)));
        if (!string.IsNullOrEmpty(searchTime))
            query = query.Where(l => l.Time.Contains(searchTime));
        if (!string.IsNullOrEmpty(searchRoom))
            query = query.Where(l => l.Room.Contains(searchRoom));
        if (searchWeek.HasValue)
            query = query.Where(l => l.Week == searchWeek.Value);
        if (!string.IsNullOrEmpty(searchLessonType))
            query = query.Where(l => l.LessonType.Contains(searchLessonType));

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
                worksheet.Cell(i + 2, 5).Value = string.Join(", ", lessons[i].Groups.Select(g => g.Name));
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
                    string groupNamesStr = row.Cell(5).GetString();
                    int week = row.Cell(6).GetValue<int>();
                    string teacherName = row.Cell(7).GetString();
                    string lessonType = row.Cell(8).GetString();

                    DateTime.TryParseExact(dateStr, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date);

                    var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.FullName == teacherName);
                    if (teacher == null && !string.IsNullOrWhiteSpace(teacherName))
                    {
                        teacher = new Teacher { FullName = teacherName };
                        _context.Teachers.Add(teacher);
                        await _context.SaveChangesAsync();
                    }

                    bool exists = await _context.Lessons.AnyAsync(l =>
                        l.Date == date &&
                        l.Subject == subject &&
                        l.Time == time &&
                        l.Room == room &&
                        l.Week == week &&
                        l.TeacherId == (teacher != null ? teacher.Id : 0) &&
                        l.LessonType == lessonType);

                    if (exists) continue;

                    var lesson = new Lesson
                    {
                        Date = date,
                        Subject = subject,
                        Time = time,
                        Room = room,
                        Week = week,
                        TeacherId = teacher?.Id ?? 0,
                        LessonType = lessonType,
                        Groups = new List<StudyGroup>()
                    };

                    if (!string.IsNullOrWhiteSpace(groupNamesStr))
                    {
                        var names = groupNamesStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        foreach (var name in names)
                        {
                            var g = await _context.StudyGroups.FirstOrDefaultAsync(sg => sg.Name == name);
                            if (g == null)
                            {
                                g = new StudyGroup { Name = name };
                                _context.StudyGroups.Add(g);
                                await _context.SaveChangesAsync();
                            }
                            lesson.Groups.Add(g);
                        }
                    }

                    _context.Lessons.Add(lesson);
                }
                await _context.SaveChangesAsync();
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteDuplicates()
    {
        var allLessons = await _context.Lessons
            .Include(l => l.Teacher)
            .Include(l => l.Groups)
            .ToListAsync();
        
        var duplicates = allLessons
            .GroupBy(l => new { 
                Date = l.Date.Date, 
                Subject = (l.Subject ?? "").Trim().ToLower(), 
                Time = (l.Time ?? "").Trim().ToLower(), 
                Room = (l.Room ?? "").Trim().ToLower(), 
                Week = l.Week, 
                TeacherName = (l.Teacher?.FullName ?? "").Trim().ToLower(), 
                LessonType = (l.LessonType ?? "").Trim().ToLower(),
                GroupNames = l.Groups != null && l.Groups.Any()
                    ? string.Join(",", l.Groups.OrderBy(g => g.Name).Select(g => (g.Name ?? "").Trim().ToLower()))
                    : "no-groups"
            })
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Skip(1))
            .ToList();

        if (duplicates.Any())
        {
            _context.Lessons.RemoveRange(duplicates);
            await _context.SaveChangesAsync();
            TempData["Message"] = $"Видалено {duplicates.Count} дублікатів";
        }
        else
        {
            TempData["Message"] = "Дублікатів не знайдено";
        }
        
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Dictionaries()
    {
        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View();
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

    [HttpPost]
    public async Task<IActionResult> DeleteGroup(int id)
    {
        var group = await _context.StudyGroups.FindAsync(id);
        if (group != null)
        {
            _context.StudyGroups.Remove(group);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Dictionaries));
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
    public async Task<IActionResult> DeleteTeacher(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);
        if (teacher != null)
        {
            _context.Teachers.Remove(teacher);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Dictionaries));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Groups)
            .FirstOrDefaultAsync(l => l.Id == id);
            
        if (lesson == null) return NotFound();

        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View(lesson);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Lesson model, int[] selectedGroups)
    {
        ModelState.Remove("Teacher");
        ModelState.Remove("Groups");

        if (ModelState.IsValid)
        {
            var existing = await _context.Lessons
                .Include(l => l.Groups)
                .FirstOrDefaultAsync(l => l.Id == model.Id);

            if (existing != null)
            {
                _context.Entry(existing).CurrentValues.SetValues(model);
                
                existing.Groups.Clear();
                if (selectedGroups != null && selectedGroups.Length > 0)
                {
                    var groupIds = selectedGroups.ToList();
                    existing.Groups = await _context.StudyGroups
                        .Where(g => groupIds.Contains(g.Id))
                        .ToListAsync();
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
        }

        ViewBag.Teachers = await _context.Teachers.ToListAsync();
        ViewBag.Groups = await _context.StudyGroups.ToListAsync();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson != null)
        {
            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    public IActionResult VoiceTest()
    {
        return View();
    }
}
