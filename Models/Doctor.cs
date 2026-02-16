using DocNote2.Models;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocNotes.Models
{
    [Table("Doctor", Schema = "Healthcare")]
    public class Doctor
    {
        [Key]
        public int DoctorId { get; set; }

        [Required]
        public string FullName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }
        public string Speciality { get; set; }

        public string? UserId { get; set; }
        public IdentityUser User { get; set; }

        public ICollection<Note> Notes { get; set; }
        public ICollection<DoctorPatient> DoctorPatients { get; set; }

    }
}

