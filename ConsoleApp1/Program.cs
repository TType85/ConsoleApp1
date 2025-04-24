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
        var filter3 = new MultiValueFilter("Fields.States", ["CA", "TX", "NY"], false);
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

        var fields = new string[] { "Fields.LoanNumber", "Fields.Status" };
        var request = new LoanPipelineRequest(combinedFilter, fields, includeArchived: true);

        // Get JSON
        string json = request.ToJson(true);
        Console.WriteLine(json);
        Console.ReadLine();
    }
}


/*
 
using System.Text.Json;
using System.Text.Json.Serialization;

public class LoanResultConverter : JsonConverter<LoanResult>
{
    public override LoanResult Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var loanId = root.GetProperty("loanId").GetString() ?? throw new JsonException("Missing loanId");
        var fields = root.GetProperty("fields").Deserialize<Dictionary<string, string>>(options) ?? new();

        return new LoanResult(
            LoanId: loanId,
            LoanFolder: fields.GetValueOrDefault("Loan.LoanFolder"),
            LoanNumber: fields.GetValueOrDefault("Loan.LoanNumber"),
            LoanRate: fields.GetValueOrDefault("Loan.LoanRate"),
            LoanAmount: fields.GetValueOrDefault("Loan.LoanAmount"),
            Fields_4002: fields.GetValueOrDefault("Fields.4002"),
            LastModified: fields.GetValueOrDefault("Loan.LastModified"),
            BorrowerName: fields.GetValueOrDefault("Loan.BorrowerName"),
            LoanFolders: fields.GetValueOrDefault("Loan.LoanFolders")
        );
    }

    public override void Write(Utf8JsonWriter writer, LoanResult value, JsonSerializerOptions options)
    {
        // Implement serialization if needed
        throw new NotImplementedException();
    }
}

var options = new JsonSerializerOptions
{
    Converters = { new LoanResultConverter() },
    PropertyNameCaseInsensitive = true
};
var loans = JsonSerializer.Deserialize<List<LoanResult>>(json, options);



public record LoanResult(
    string LoanId,
    string? LoanFolder,
    string? LoanNumber,
    string? LoanRate,
    string? LoanAmount,
    string? Fields_4002,
    string? LastModified,
    string? BorrowerName,
    string? LoanFolders,
    Dictionary<string, string>? AdditionalFields = null
);

AdditionalFields: fields.Where(kvp => !new[] {
    "Loan.LoanFolder", "Loan.LoanNumber", "Loan.LoanRate", "Loan.LoanAmount",
    "Fields.4002", "Loan.LastModified", "Loan.BorrowerName", "Loan.LoanFolders"
}.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)




 */