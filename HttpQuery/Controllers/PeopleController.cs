using System.Diagnostics.CodeAnalysis;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace HttpQuery.Controllers;

[Route("api/people")]
public class PeopleController : Controller
{
    [HttpQuery, Route("", Name = "controller")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            var query = new Query { Text = body };

            // database
            var people = Enumerable.Range(1, 100)
                .Select(i => new Person { Index = i, Name = $"Minion #{i}" });

            // let's use the Query
            var parameter = Expression.Parameter(typeof(IEnumerable<Person>), nameof(people));
            var expression = DynamicExpressionParser.ParseLambda(new[] { parameter }, null, query.Text);
            var compiled = expression.Compile();

            // execute query
            var result = compiled.DynamicInvoke(people);

            return Ok(new
            {
                query,
                source = "controller",
                results = result
            });
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }
}

public class HttpQueryAttribute : HttpMethodAttribute
{
    private static readonly IEnumerable<string> SupportedMethods = new[] { "QUERY" };

    /// <summary>
    /// Creates a new <see cref="Microsoft.AspNetCore.Mvc.HttpGetAttribute"/>.
    /// </summary>
    public HttpQueryAttribute()
        : base(SupportedMethods)
    {
    }

    /// <summary>
    /// Creates a new <see cref="Microsoft.AspNetCore.Mvc.HttpGetAttribute"/> with the given route template.
    /// </summary>
    /// <param name="template">The route template. May not be null.</param>
    public HttpQueryAttribute([StringSyntax("Route")] string template)
        : base(SupportedMethods, template)
    {
        if (template == null)
        {
            throw new ArgumentNullException(nameof(template));
        }
    }
}