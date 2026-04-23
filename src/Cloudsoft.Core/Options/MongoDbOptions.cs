namespace Cloudsoft.Core.Options;

public class MongoDbOptions
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "Cloudsoft";

    public string JobPostingsCollectionName { get; set; } = "jobPostings";

    public string EmployersCollectionName { get; set; } = "employers";
}
