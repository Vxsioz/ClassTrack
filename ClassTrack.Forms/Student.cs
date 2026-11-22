using System.ComponentModel.DataAnnotations;

namespace ClassTrack
{
    // Maps to the Students table. The profile fields are optional (nullable) so
    // records created before they existed stay valid.
    public class Student
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        public string Surname { get; set; } = "";

        [StringLength(254)]
        public string? Email { get; set; }

        [StringLength(30)]
        public string? ContactNumber { get; set; }

        [StringLength(100)]
        public string? Course { get; set; }

        [Range(1, 6)]
        public byte? YearLevel { get; set; }

        /// <summary>Relative path inside the app's picture folder, e.g. "students/3f2a....jpg".</summary>
        [StringLength(260)]
        public string? PhotoPath { get; set; }

        public string FullName => $"{FirstName} {Surname}";
    }
}
