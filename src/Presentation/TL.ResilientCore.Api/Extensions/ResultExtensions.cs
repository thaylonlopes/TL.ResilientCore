using Microsoft.AspNetCore.Http;
using TL.ResilientCore.Domain.Shared;

namespace TL.ResilientCore.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult(this Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return Results.BadRequest(new { Error = result.Error });
    }

    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return Results.BadRequest(new { Error = result.Error });
    }
}