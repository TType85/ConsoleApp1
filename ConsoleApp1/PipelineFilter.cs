using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PipelineFilter;

// Enums (unchanged)
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

// JSON Converters (unchanged)
public static class JsonConverters
{
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        WriteIndented = false, // Default to non-indented
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

// Filter Interface (unchanged)
public interface IFilter
{
    CompositeFilter And(IFilter filter);
    CompositeFilter Or(IFilter filter);
}

// BaseFilter (unchanged)
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

// StringFilter (unchanged)
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

// DateFilter (unchanged)
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

// EmptyValueFilter (unchanged)
public sealed record EmptyValueFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = "";

    public EmptyValueFilter(string canonicalName)
        : base(canonicalName, MatchType.IsEmpty) { }
}

// NotEmptyValueFilter (unchanged)
public sealed record NotEmptyValueFilter : BaseFilter
{
    [JsonPropertyName("value")]
    public string Value { get; init; } = "";

    public NotEmptyValueFilter(string canonicalName)
        : base(canonicalName, MatchType.IsNotEmpty) { }
}

// MultiValueFilter (unchanged)
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

// CompositeFilter (unchanged)
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

// SortCriterion (unchanged)
public sealed record SortCriterion(
    [property: JsonPropertyName("canonicalName")] string CanonicalName,
    [property: JsonPropertyName("order")] SortOrder Order);

// LoanPipelineRequest (updated)
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

    [JsonPropertyName("start")]
    public int? Start { get; init; }

    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    [JsonPropertyName("includeArchivedLoans")]
    public bool IncludeArchivedLoans { get; init; }

    public LoanPipelineRequest()
    {
        _fields = [];
        _sort = [];
    }

    public LoanPipelineRequest(IFilter filter) : this()
    {
        Filter = filter;
    }

    private LoanPipelineRequest(
        object? filter,
        IReadOnlyList<string> fields,
        IReadOnlyList<SortCriterion> sort,
        int? start,
        int? limit,
        bool includeArchivedLoans)
    {
        Filter = filter;
        _fields = fields;
        _sort = sort;
        Start = start;
        Limit = limit;
        IncludeArchivedLoans = includeArchivedLoans;
    }

    public LoanPipelineRequest WithFields(params string[] fields)
    {
        var newFields = fields?.Length > 0 ? [.. _fields, .. fields] : _fields;
        return new LoanPipelineRequest(Filter, newFields, _sort, Start, Limit, IncludeArchivedLoans);
    }

    public LoanPipelineRequest WithSort(params SortCriterion[] sortCriteria)
    {
        var newSort = sortCriteria?.Length > 0 ? [.. _sort, .. sortCriteria] : _sort;
        return new LoanPipelineRequest(Filter, _fields, newSort, Start, Limit, IncludeArchivedLoans);
    }

    public LoanPipelineRequest WithPagination(int start, int limit) =>
        new LoanPipelineRequest(Filter, _fields, _sort, start, limit, IncludeArchivedLoans);

    public LoanPipelineRequest IncludeArchived(bool include = true) =>
        new LoanPipelineRequest(Filter, _fields, _sort, Start, Limit, include);

    public string ToJson(bool indented = false)
    {
        var options = new JsonSerializerOptions(JsonConverters.DefaultOptions)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(this, options);
    }
}