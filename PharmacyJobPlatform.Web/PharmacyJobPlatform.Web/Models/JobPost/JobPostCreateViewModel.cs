using PharmacyJobPlatform.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class JobPostCreateViewModel
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public JobType JobType { get; set; }

    [Required]
    public string City { get; set; }

    public string Address { get; set; }

    // Günlük
    public decimal? DailyWage { get; set; }
    public DateTime? WorkDate { get; set; }

    // Kalıcı
    public decimal? MonthlySalary { get; set; }
}
