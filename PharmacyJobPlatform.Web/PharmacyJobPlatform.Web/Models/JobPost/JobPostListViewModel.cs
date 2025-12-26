using PharmacyJobPlatform.Domain.Enums;

public class JobPostListViewModel
{
    public int Id { get; set; }

    public string Title { get; set; }

    public JobType JobType { get; set; }

    public string City { get; set; }
    public string Description { get; set; }


    public decimal? DailyWage { get; set; }

    public decimal? MonthlySalary { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
