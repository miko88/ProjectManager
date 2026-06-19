using FluentValidation.Results;

namespace ProjectManager.Application.Common;

public static class ValidationResultExtensions
{
    public static IReadOnlyDictionary<string, string[]> ToErrorDictionary(this ValidationResult result) =>
        result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
