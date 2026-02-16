
using DocNote2.Models;
using DocNotes.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocNotes.Models
{
    [Table("Note", Schema = "Healthcare")]
    public class Note
    {
        [Key]
        public int NoteId { get; set; }


        [Required(ErrorMessage = "Clinical note is required")]
        [MinLength(5, ErrorMessage = "Note must be at least 5 characters")]
        public string NoteText { get; set; }

        public DateTime CreatedOn { get; set; }

        // Set only on update
        public DateTime? UpdatedOn { get; set; }


        // Foreign Keys
        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        public string? FilePath { get; set; }   // ✅ ADD THIS

        // Navigation
        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
    }
}