
using DocNote2.Models;
using DocNotes.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocNotes.Models
{
    [Table("History", Schema = "Healthcare")]
    public class History
    {
        [Key]
        public int HistoryId { get; set; }

        public int PatientId { get; set; }
        public int DoctorId { get; set; }

        public string Action { get; set; }
        public DateTime ActionDate { get; set; } = DateTime.Now;

        public Patient Patient { get; set; }
        public Doctor Doctor { get; set; }
    }
}