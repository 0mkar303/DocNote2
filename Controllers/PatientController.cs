using DocNote2.Data;
using DocNotes.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
namespace DocNotes.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class PatientController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PatientController(ApplicationDbContext context)
        {
            _context = context;
        }

        //public async Task<IActionResult> Index(string search)
        //{
        //    int pageSize = 5; // Show 5 patients per page

        //    // 1. Get logged-in user's Identity Id
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        //    // 2. Find Doctor record linked to this user
        //    var doctor = await _context.Doctors.AsNoTracking()
        //        .FirstOrDefaultAsync(d => d.UserId == userId);

        //    if (doctor == null)
        //        return Unauthorized("Doctor profile not found");

        //    // 3. Get only patients assigned to this doctor
        //    var patients = _context.DoctorPatients.AsNoTracking()
        //        .Where(dp => dp.DoctorId == doctor.DoctorId)
        //        .Select(dp => dp.Patient)
        //        .AsQueryable();

        //    // 4. Apply search filter
        //    if (!string.IsNullOrWhiteSpace(search))
        //    {
        //        search = search.Trim();
        //        patients = patients.Where(p => p.FullName.Contains(search));
        //    }

        //    return View(await patients.ToListAsync());
        //}

        public async Task<IActionResult> Index(string search, int page = 1)
        {
            int pageSize = 5; // Show 5 patients per page

            // 1. Get logged-in user's Identity Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Find Doctor record linked to this user
            var doctor = await _context.Doctors.AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return Unauthorized("Doctor profile not found");

            // 3. Get only patients assigned to this doctor
            var patients = _context.DoctorPatients.AsNoTracking()
                .Where(dp => dp.DoctorId == doctor.DoctorId)
                .Select(dp => dp.Patient)
                .AsQueryable();

            // 4. Apply search filter (UNCHANGED business logic)
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();
                patients = patients.Where(p => p.FullName.Contains(search));
            }

            // =========================
            // PAGINATION (NEW PART)
            // =========================

            // Total number of matching records
            int totalRecords = await patients.CountAsync();

            // Total number of pages
            int totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Fetch only one page of data
            var pagedPatients = await patients
                .OrderBy(p => p.FullName)              // Always order before Skip
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Send pagination info to View
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.Search = search;

            return View(pagedPatients);
        }


        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var doctor = await _context.Doctors
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return Unauthorized("Doctor profile not found");

            // Check if this patient is assigned to this doctor
            var isAssigned = await _context.DoctorPatients
                .AnyAsync(dp => dp.DoctorId == doctor.DoctorId && dp.PatientId == id);

            if (!isAssigned)
                return Unauthorized("You are not allowed to view this patient.");

            var patient = await _context.Patients
                .AsNoTracking()
                .Include(p => p.Notes)
                    .ThenInclude(n => n.Doctor)
                .FirstOrDefaultAsync(p => p.PatientId == id);
            // 🔥 ONLY CHANGE: Latest notes first
            patient.Notes = patient.Notes
                .OrderByDescending(n => n.CreatedOn)
                .ToList();


            if (patient == null)
                return NotFound();

            return View(patient);
        }





    }
}