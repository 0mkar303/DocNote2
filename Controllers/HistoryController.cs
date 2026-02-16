using DocNote2.Data;
using DocNotes.Data;
using DocNotes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocNotes.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class HistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HistoryController(ApplicationDbContext context)
        {
            _context = context;
        }

        //public async Task<IActionResult> Index(int? patientId)
        //{
        //    // Load patients for dropdown
        //    ViewBag.Patients = await _context.Patients
        //        .OrderBy(p => p.FullName)
        //        .ToListAsync();

        //    var historyQuery = _context.Histories
        //        .Include(h => h.Patient)
        //        .Include(h => h.Doctor)
        //        .AsQueryable();

        //    // Apply filter if patient selected
        //    if (patientId.HasValue)
        //    {
        //        historyQuery = historyQuery
        //            .Where(h => h.PatientId == patientId.Value);
        //    }

        //    var history = await historyQuery
        //        .OrderByDescending(h => h.ActionDate)
        //        .ToListAsync();

        //    ViewBag.SelectedPatientId = patientId;

        //    return View(history);
        //}

        //public async Task<IActionResult> Index(string search)
        //{
        //    var historyQuery = _context.Histories
        //        .Include(h => h.Patient)
        //        .Include(h => h.Doctor)
        //        .AsQueryable();

        //    if (!string.IsNullOrWhiteSpace(search))
        //    {
        //        historyQuery = historyQuery
        //            .Where(h => h.Patient.FullName.Contains(search));
        //    }

        //    var history = await historyQuery
        //        .OrderByDescending(h => h.ActionDate)
        //        .ToListAsync();

        //    ViewBag.Search = search;

        //    return View(history);
        //}

        public async Task<IActionResult> Index(string search)
        {
            // 1. Get logged-in user's Identity Id
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 2. Find Doctor record linked to this user
            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return Unauthorized("Doctor profile not found");

            // 3. Load ONLY history of this doctor
            var historyQuery = _context.Histories
                .Include(h => h.Patient)
                .Include(h => h.Doctor)
                .Where(h => h.DoctorId == doctor.DoctorId)   // 🔥 IMPORTANT LINE
                .AsQueryable();

            // 4. Apply search (by patient name)
            if (!string.IsNullOrWhiteSpace(search))
            {
                historyQuery = historyQuery
                    .Where(h => h.Patient.FullName.Contains(search));
            }

            var history = await historyQuery
                .OrderByDescending(h => h.ActionDate)
                .ToListAsync();

            ViewBag.Search = search;
            return View(history);
        }

    }
}