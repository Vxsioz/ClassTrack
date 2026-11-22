using System.ComponentModel.DataAnnotations;

namespace ClassTrack
{
    // Same shape as the web app's Student model, so both apps share the Students table.
    public class Student
    {
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        public string Surname { get; set; } = "";
    }
}
