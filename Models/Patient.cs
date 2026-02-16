using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocNotes.Models
{
    [Table("Patient", Schema = "Healthcare")]
    public class Patient
    {
        [Key]
        public int PatientId { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Phone { get; set; }
        public string Email { get; set; }

        public DateTime DateOfBirth { get; set; }
        public string BloodGroup { get; set; }

        public string Address { get; set; }

        public string GuardianName { get; set; }
        public string GuardianContact { get; set; }
        public int Age { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<Note> Notes { get; set; }
        public ICollection<DoctorPatient> DoctorPatients { get; set; }

    }
}