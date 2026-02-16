using DocNote2.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocNotes.Models
{
    [Table("DoctorPatient", Schema = "Healthcare")]
    public class DoctorPatient
    {
        [Key]
        public int DoctorPatientId { get; set; }

        public int DoctorId { get; set; }
        public int PatientId { get; set; }

        public DateTime AssignedOn { get; set; } = DateTime.Now;

        public Doctor Doctor { get; set; }
        public Patient Patient { get; set; }
    }

}
