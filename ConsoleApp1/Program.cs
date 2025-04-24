using System.Text.Json.Serialization;
using System.Text.Json;

namespace PipelineFilter;

public class Program
{
    private static void Main(string[] args)
    {
        // Create individual filters
        var filter1 = new StringFilter("Fields.Status", MatchType.Equals, "Active");
        var filter2 = new DateFilter("Fields.CreatedDate", MatchType.Equals, DateTime.Today, DatePrecision.Day);
        var filter3 = new MultiValueFilter("Fields.States", new[] { "CA", "TX", "NY" }, false);
        var filter4 = new DateFilter("Fields.CreatedDate", MatchType.Equals, DateTime.Today.Subtract(TimeSpan.FromDays(-14)), DatePrecision.Day);
        var filter5 = new DateFilter("Fields.CreatedDate", MatchType.Equals, DateTime.Today.Subtract(TimeSpan.FromDays(-28)), DatePrecision.Day);
        var filter6 = new DateFilter("Fields.CreatedDate", MatchType.Equals, DateTime.Today.Subtract(TimeSpan.FromDays(-30)), DatePrecision.Day);
        var filter7 = new EmptyValueFilter("Fields.CreatedDate2");
        var filter8 = new NotEmptyValueFilter("Fields.CreatedDate2");
        // Combine filters
        var dateFilter = filter4.Or(filter6).Or(filter5);
        var combinedFilter = filter1.And(filter2).And(filter3).And(dateFilter).And(filter7).And(filter8);
        var sort = new SortCriterion("Fields.CreatedDate", SortOrder.Ascending);
        // Create filter request
        var request = new LoanPipelineRequest(combinedFilter)
            .WithFields("Fields.LoanNumber", "Fields.Status")
            .WithSort(sort)
            .IncludeArchived(true);

        // Get JSON
        string json = request.ToJson(true);
        Console.WriteLine(json);
        Console.ReadLine();
    }
}