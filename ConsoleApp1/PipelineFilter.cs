using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipelineFilter;

public enum MatchType
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    Exact,
    GreaterThan,
    GreaterThanOrEquals,
    LessThan,
    LessThanOrEquals,
    IsEmpty,
    IsNotEmpty,
    MultiValue
}

public enum LogicalOperator
{
    And,
    Or
}

public enum DatePrecision
{
    Day,
    Month,
    Year,
    Recurring,
    Hour,
    Minute
}

public enum SortOrder
{
    Ascending,
    Descending
}

public static class JsonConverters
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters =
        {
            new EnumConverter<MatchType>(),
            new EnumConverter<LogicalOperator>(),
            new EnumConverter<DatePrecision>(),
            new EnumConverter<SortOrder>()
        }
    };

    private class EnumConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Enum.Parse<T>(reader.GetString()!, true);

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString().ToLowerInvariant());
    }
}

public interface IFilter
{
    CompositeFilter And(IFilter filter);
    CompositeFilter Or(IFilter filter);
}

public abstract record BaseFilter : IFilter
{
    [JsonPropertyName("matchType")]
    public MatchType MatchType { get; init; }

    [JsonPropertyName("canonicalName")]
    public string CanonicalName { get; init; }

    protected BaseFilter(string canonicalName, MatchType matchType)
    {
        CanonicalName = canonicalName;
        MatchType = matchType;
    }

    public CompositeFilter And(IFilter filter) => new(LogicalOperator.And, this, filter);
    public CompositeFilter Or(IFilter filter) => new(LogicalOperator.Or, this, filter);
}

public sealed record StringFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("include")]
    public bool? Include { get; init; }

    public StringFilter(string canonicalName, MatchType matchType, string value, bool? include = null)
        : base(canonicalName, matchType)
    {
        Value = value;
        Include = include;
    }
}

public sealed record DateFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; }

    [JsonPropertyName("precision")]
    public DatePrecision Precision { get; init; }

    public DateFilter(string canonicalName, MatchType matchType, string value, DatePrecision precision = DatePrecision.Day)
        : base(canonicalName, matchType)
    {
        Value = value;
        Precision = precision;
    }

    public DateFilter(string canonicalName, MatchType matchType, DateTime date, DatePrecision precision = DatePrecision.Day)
        : this(canonicalName, matchType, FormatDate(date, precision), precision) { }

    private static string FormatDate(DateTime date, DatePrecision precision) => precision switch
    {
        DatePrecision.Day => date.ToString("yyyy-MM-dd"),
        DatePrecision.Month => date.ToString("yyyy-MM"),
        DatePrecision.Year => date.ToString("yyyy"),
        _ => date.ToString("yyyy-MM-dd")
    };
}


public sealed record EmptyValueFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = "0001-01-01T00:00:00";

    public EmptyValueFilter(string canonicalName)
        : base(canonicalName, MatchType.IsEmpty) { }
}


public sealed record NotEmptyValueFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = "0001-01-01T00:00:00";

    public NotEmptyValueFilter(string canonicalName)
        : base(canonicalName, MatchType.IsNotEmpty) { }
}


public sealed record MultiValueFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public IReadOnlyList<string> Value { get; init; }

    [JsonPropertyName("include")]
    public bool Include { get; init; }

    public MultiValueFilter(string canonicalName, IEnumerable<string> values, bool include = true)
        : base(canonicalName, MatchType.MultiValue)
    {
        Value = values?.ToList() ?? [];
        Include = include;
    }
}

public sealed record CompositeFilter : IFilter
{
    [JsonPropertyName("operator")]
    public LogicalOperator Operator { get; init; }

    [JsonPropertyName("terms")]
    public IReadOnlyList<object> Terms { get; init; } = [];

    public CompositeFilter(LogicalOperator op, params object[] terms)
    {
        Operator = op;
        Terms = terms?.ToList() ?? [];
    }

    public CompositeFilter And(IFilter? filter)
    {
        if (filter == null) return this;
        return Operator == LogicalOperator.And
            ? this with { Terms = [.. Terms, filter] }
            : new CompositeFilter(LogicalOperator.And, this, filter);
    }

    public CompositeFilter Or(IFilter? filter)
    {
        if (filter == null) return this;
        return Operator == LogicalOperator.Or
            ? this with { Terms = [.. Terms, filter] }
            : new CompositeFilter(LogicalOperator.Or, this, filter);
    }
}


public sealed record SortCriterion(
    [property: JsonPropertyName("canonicalName")] string CanonicalName,
    [property: JsonPropertyName("order")] SortOrder Order);


public sealed record LoanPipelineRequest
{
    private readonly IReadOnlyList<string> _fields;
    private readonly IReadOnlyList<SortCriterion> _sort;

    [JsonPropertyName("filter")]
    public object? Filter { get; init; }

    [JsonPropertyName("fields")]
    public IReadOnlyList<string>? Fields => _fields.Any() ? _fields : null;

    [JsonPropertyName("sort")]
    public IReadOnlyList<SortCriterion>? Sort => _sort.Any() ? _sort : null;

    [JsonPropertyName("includeArchivedLoans")]
    public bool IncludeArchivedLoans { get; init; }

    public LoanPipelineRequest(IFilter filter, string[]? fields = null, SortCriterion? sort = null, bool includeArchived = false)
    {
        Filter = filter;
        _fields = fields?.ToList() ?? [];
        _sort = sort != null ? [sort] : [];
        IncludeArchivedLoans = includeArchived;
    }

    public string ToJson(bool indented = false)
    {
        var options = new JsonSerializerOptions(JsonConverters.DefaultOptions)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(this, options);
    }
}