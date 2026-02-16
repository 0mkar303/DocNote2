using DocNote2.Data;
using DocNotes.Data;
using DocNotes.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DocNotes.Controllers
{
    [Authorize(Roles = "Doctor")]
    public class NoteController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NoteController(ApplicationDbContext context)
        {
            _context = context;
        }

        // All Note-related actions will go here
        [HttpGet]
        public IActionResult Create(int patientId)
        {
            ViewBag.PatientId = patientId;
            return View("CreateNote"); // reuse same view
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int patientId, string noteText)
        {
            // 1. Create a temp Note object only for validation
            var tempNote = new Note
            {
                NoteText = noteText
            };

            // 2. Manually trigger model validation
            TryValidateModel(tempNote);
            // Remove navigation validation
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");
            if (!ModelState.IsValid)
            {
                ViewBag.PatientId = patientId;
                return View("CreateNote");
            }


            // 2. Get logged-in doctor
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var doctor = await _context.Doctors
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (doctor == null)
                return Unauthorized("Doctor profile not found.");

            // 3. Prepare file system paths
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "notes");
            Directory.CreateDirectory(folderPath);

            var fileName = $"Note_{patientId}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            var relativePath = "/notes/" + fileName;
            var fullPath = Path.Combine(folderPath, fileName);

            var fileContent =
        $@"Doctor : {doctor.FullName}
Patient ID : {patientId}
Date : {DateTime.Now}

-------------------------
{noteText}
-------------------------";

            // 4. Use transaction for DB safety
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 5. Write file
                await System.IO.File.WriteAllTextAsync(fullPath, fileContent);

                // 6. Call Stored Procedure
                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC Healthcare.sp_InsertNote @PatientId, @DoctorId, @NoteText, @FilePath",
                    new SqlParameter("@PatientId", patientId),
                    new SqlParameter("@DoctorId", doctor.DoctorId),
                    new SqlParameter("@NoteText", noteText),
                    new SqlParameter("@FilePath", relativePath)
                );

                // 7. Commit transaction
                await transaction.CommitAsync();

                return RedirectToAction("Details", "Patient", new { id = patientId });
            }
            catch (Exception ex)
            {
                // 8. Rollback transaction
                await transaction.RollbackAsync();

                // 9. Log error (if logger is available)
                // _logger.LogError(ex, "Error while creating clinical note");

                // 10. Clean up file if DB failed
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }

                // 11. Show user-friendly message
                ModelState.AddModelError("", "Unable to save note. Please try again.");
                ViewBag.PatientId = patientId;
                return View("CreateNote");
            }
        }

        [Authorize(Roles = "Doctor")]
        public IActionResult Download(int noteId)
        {
            var note = _context.Notes.FirstOrDefault(n => n.NoteId == noteId);

            if (note == null || string.IsNullOrEmpty(note.FilePath))
                return NotFound();

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                note.FilePath.TrimStart('/')
            );

            if (!System.IO.File.Exists(filePath))
                return NotFound();

            return PhysicalFile(filePath, "text/plain", Path.GetFileName(filePath));
        }
        [HttpGet]
        public async Task<IActionResult> Edit(int noteId)
        {
            var note = await _context.Notes
                .Include(n => n.Doctor)
                .FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (note.Doctor.UserId != userId)
                return Unauthorized("You can edit only your own notes");

            return View("EditNote", note);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Note model)
        {
            // Remove navigation property validation
            ModelState.Remove("Patient");
            ModelState.Remove("Doctor");

            if (!ModelState.IsValid)
            {
                return View("EditNote", model);
            }

            // 1. Load existing note with doctor
            var note = await _context.Notes
                .Include(n => n.Doctor)
                .FirstOrDefaultAsync(n => n.NoteId == model.NoteId);

            if (note == null)
                return NotFound();

            // 2. Authorization: only owner doctor can edit
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (note.Doctor.UserId != userId)
                return Unauthorized("You can edit only your own notes.");

            // 3. Update via Stored Procedure (Trigger will log history)
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Healthcare.sp_UpdateNote @NoteId, @NoteText",
                new SqlParameter("@NoteId", model.NoteId),
                new SqlParameter("@NoteText", model.NoteText)
            );

            //// 4. Update text file also
            //if (!string.IsNullOrEmpty(note.FilePath))
            //{
            //    var fullPath = Path.Combine(
            //        Directory.GetCurrentDirectory(),
            //        "wwwroot",
            //        note.FilePath.TrimStart('/')
            //    );

            //    if (System.IO.File.Exists(fullPath))
            //    {
            //        await System.IO.File.WriteAllTextAsync(fullPath, model.NoteText);
            //    }
            //}

            return RedirectToAction("Details", "Patient", new { id = note.PatientId });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int noteId)
        {
            var note = await _context.Notes
                .Include(n => n.Doctor)
                .Include(n => n.Patient)
                .FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
                return NotFound();

            // Only assigned doctor or authorized roles can view (optional rule)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (note.Doctor.UserId != userId && !User.IsInRole("Admin"))
                return Unauthorized("You are not allowed to view this note.");

            return View("NoteDetails", note);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int noteId)
        {
            var note = await _context.Notes
                .Include(n => n.Doctor)
                .FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (note.Doctor.UserId != userId)
                return Unauthorized("You can delete only your own notes.");

            return View("DeleteNote", note);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int noteId)
        {
            // 1. Load note with doctor
            var note = await _context.Notes
                .Include(n => n.Doctor)
                .FirstOrDefaultAsync(n => n.NoteId == noteId);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (note.Doctor.UserId != userId)
                return Unauthorized("You can delete only your own notes.");

            string? filePath = note.FilePath;

            // 2. Delete DB record via SP
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC Healthcare.sp_DeleteNote @NoteId",
                new SqlParameter("@NoteId", noteId)
            );

            // 3. Delete file from disk
            if (!string.IsNullOrEmpty(filePath))
            {
                var fullPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    filePath.TrimStart('/')
                );

                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            return RedirectToAction("Details", "Patient", new { id = note.PatientId });
        }



    }
}
