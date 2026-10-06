using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace TechnicalTrainingPlanner.ViewModels.Sessions;

public class SessionFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Eğitim seçiniz.")]
    public int TrainingId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Eğitmen seçiniz.")]
    public int InstructorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Sınıf seçiniz.")]
    public int ClassroomId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Kontenjan pozitif olmalıdır.")]
    public int Capacity { get; set; }

    public DateTime StartsAtLocal { get; set; }
    public DateTime EndsAtLocal { get; set; }
    public string Status { get; set; } = "PLANNED";

    public List<SelectListItem> Trainings { get; set; } = new();
    public List<SelectListItem> Instructors { get; set; } = new();
    public List<SelectListItem> Classrooms { get; set; } = new();
}
