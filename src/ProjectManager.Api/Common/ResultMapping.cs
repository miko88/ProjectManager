using ProjectManager.Application.Common;

namespace ProjectManager.Api.Common;

public static class ResultMapping
{
    /// <summary>Maps a non-success Result to a ProblemDetails IResult. Caller handles success.</summary>
    public static IResult ToProblem(this Result result) => result.Status switch
    {
        ResultStatus.Invalid => Results.ValidationProblem(result.ValidationErrors),
        ResultStatus.NotFound => Results.Problem(result.Message, statusCode: StatusCodes.Status404NotFound),
        ResultStatus.Conflict => Results.Problem(result.Message, statusCode: StatusCodes.Status409Conflict),
        ResultStatus.Unauthorized => Results.Problem(result.Message, statusCode: StatusCodes.Status401Unauthorized),
        _ => Results.Problem("Unexpected result status.", statusCode: StatusCodes.Status500InternalServerError)
    };
}
