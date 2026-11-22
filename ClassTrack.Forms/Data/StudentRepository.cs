using Microsoft.EntityFrameworkCore;

namespace ClassTrack.Data
{
    /// <summary>What the Students page asks for: search + filters + sort + page.</summary>
    public sealed record StudentQuery(
        string? Search = null,
        string? Course = null,
        byte? YearLevel = null,
        string SortBy = "Id",          // Id | FirstName | Surname | Course | YearLevel
        bool Descending = false,
        int Page = 1,
        int PageSize = 10);

    public sealed record StudentPage(List<Student> Items, int Total, int Page, int TotalPages);

    public sealed record CountBy(string Label, int Count);

    public sealed record DashboardStats(
        int Total,
        List<CountBy> ByCourse,
        List<CountBy> ByYear,
        List<Student> Recent);

    /// <summary>
    /// All student database work lives here, so the UI never touches EF directly.
    /// Each call uses its own short-lived context.
    /// </summary>
    public class StudentRepository
    {
        public async Task<StudentPage> GetPageAsync(StudentQuery q)
        {
            await using var db = new AppDbContext();
            IQueryable<Student> query = ApplyFilters(db.Students.AsNoTracking(), q);

            int total = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)q.PageSize));
            int page = Math.Clamp(q.Page, 1, totalPages);

            var items = await ApplySort(query, q.SortBy, q.Descending)
                .Skip((page - 1) * q.PageSize).Take(q.PageSize)
                .ToListAsync();

            return new StudentPage(items, total, page, totalPages);
        }

        /// <summary>Every student matching the filters (for CSV export), unpaged.</summary>
        public async Task<List<Student>> GetAllAsync(StudentQuery q)
        {
            await using var db = new AppDbContext();
            return await ApplySort(ApplyFilters(db.Students.AsNoTracking(), q), q.SortBy, q.Descending)
                .ToListAsync();
        }

        public async Task<Student?> GetAsync(int id)
        {
            await using var db = new AppDbContext();
            return await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<int> AddAsync(Student student)
        {
            await using var db = new AppDbContext();
            db.Students.Add(student);
            await db.SaveChangesAsync();
            return student.Id;
        }

        /// <summary>Saves the editable fields. Returns false if the student no longer exists.</summary>
        public async Task<bool> UpdateAsync(Student edited)
        {
            await using var db = new AppDbContext();
            var tracked = await db.Students.FindAsync(edited.Id);
            if (tracked == null) return false;

            tracked.FirstName = edited.FirstName;
            tracked.Surname = edited.Surname;
            tracked.Email = edited.Email;
            tracked.ContactNumber = edited.ContactNumber;
            tracked.Course = edited.Course;
            tracked.YearLevel = edited.YearLevel;
            tracked.PhotoPath = edited.PhotoPath;
            await db.SaveChangesAsync();
            return true;
        }

        /// <summary>Deletes the student. Returns their PhotoPath so the caller can remove the file.</summary>
        public async Task<(bool Deleted, string? PhotoPath)> DeleteAsync(int id)
        {
            await using var db = new AppDbContext();
            var tracked = await db.Students.FindAsync(id);
            if (tracked == null) return (false, null);

            string? photo = tracked.PhotoPath;
            db.Students.Remove(tracked);
            await db.SaveChangesAsync();
            return (true, photo);
        }

        /// <summary>Distinct course names already in use (for the editor's suggestions and the filter).</summary>
        public async Task<List<string>> GetCoursesAsync()
        {
            await using var db = new AppDbContext();
            return await db.Students.AsNoTracking()
                .Where(s => s.Course != null && s.Course != "")
                .Select(s => s.Course!).Distinct().OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<DashboardStats> GetDashboardAsync(int recentCount = 6)
        {
            await using var db = new AppDbContext();

            int total = await db.Students.CountAsync();

            var byCourse = await db.Students.AsNoTracking()
                .GroupBy(s => s.Course ?? "")
                .Select(g => new { Course = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ThenBy(x => x.Course)
                .ToListAsync();

            var byYear = await db.Students.AsNoTracking()
                .GroupBy(s => s.YearLevel)
                .Select(g => new { Year = g.Key, Count = g.Count() })
                .OrderBy(x => x.Year == null).ThenBy(x => x.Year)
                .ToListAsync();

            var recent = await db.Students.AsNoTracking()
                .OrderByDescending(s => s.Id).Take(recentCount).ToListAsync();

            return new DashboardStats(
                total,
                byCourse.Select(x => new CountBy(x.Course == "" ? "Unassigned" : x.Course, x.Count)).ToList(),
                byYear.Select(x => new CountBy(x.Year == null ? "Unassigned" : $"Year {x.Year}", x.Count)).ToList(),
                recent);
        }

        // ---- query building -------------------------------------------------

        private static IQueryable<Student> ApplyFilters(IQueryable<Student> query, StudentQuery q)
        {
            if (!string.IsNullOrWhiteSpace(q.Search))
            {
                string t = q.Search.Trim();
                bool isId = int.TryParse(t, out int id);
                query = query.Where(s =>
                    (isId && s.Id == id) ||
                    s.FirstName.Contains(t) ||
                    s.Surname.Contains(t) ||
                    (s.Email != null && s.Email.Contains(t)) ||
                    (s.Course != null && s.Course.Contains(t)));
            }
            if (!string.IsNullOrEmpty(q.Course))
                query = query.Where(s => s.Course == q.Course);
            if (q.YearLevel != null)
                query = query.Where(s => s.YearLevel == q.YearLevel);
            return query;
        }

        /// <summary>Only whitelisted columns are sorted; Id is the tie-breaker so paging is stable.</summary>
        private static IOrderedQueryable<Student> ApplySort(IQueryable<Student> query, string sortBy, bool desc) =>
            (sortBy, desc) switch
            {
                ("FirstName", false) => query.OrderBy(s => s.FirstName).ThenBy(s => s.Id),
                ("FirstName", true) => query.OrderByDescending(s => s.FirstName).ThenBy(s => s.Id),
                ("Surname", false) => query.OrderBy(s => s.Surname).ThenBy(s => s.Id),
                ("Surname", true) => query.OrderByDescending(s => s.Surname).ThenBy(s => s.Id),
                ("Course", false) => query.OrderBy(s => s.Course).ThenBy(s => s.Id),
                ("Course", true) => query.OrderByDescending(s => s.Course).ThenBy(s => s.Id),
                ("YearLevel", false) => query.OrderBy(s => s.YearLevel).ThenBy(s => s.Id),
                ("YearLevel", true) => query.OrderByDescending(s => s.YearLevel).ThenBy(s => s.Id),
                (_, true) => query.OrderByDescending(s => s.Id),
                _ => query.OrderBy(s => s.Id)
            };
    }
}
