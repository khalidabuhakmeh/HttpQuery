using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", async (HttpContext ctx, LinkGenerator generator, string? q) =>
{
    var client = new HttpClient();

    var request = new HttpRequestMessage(
        new HttpMethod("QUERY"),
        generator.GetUriByName(ctx, "people")
    );

    //language=C#
    q ??= "people.Take(10).OrderByDescending(p => p.Index)";

    request.Content = new StringContent(q, Encoding.UTF8, "text/plain");

    var response = client.Send(request);
    var result = await response.Content.ReadAsStringAsync();

    return Results.Content(result, "application/json");
});

app.MapQuery("/people", query =>
{
    try
    {
        // database
        var people = Enumerable.Range(1, 100)
            .Select(i => new Person { Index = i, Name = $"Minion #{i}" });

        // let's use the Query
        var parameter = Expression.Parameter(typeof(IEnumerable<Person>), nameof(people));
        var expression = DynamicExpressionParser.ParseLambda(new[] { parameter }, null, query.Text);
        var compiled = expression.Compile();

        // execute query
        var result = compiled.DynamicInvoke(people);

        return Results.Ok(new
        {
            query,
            results = result
        });
    }
    catch (Exception e)
    {
        return Results.BadRequest(e);
    }
})
.WithName("people");

app.Run();

public class Person
{
    public string Name { get; set; }
    public int Index { get; set; }
}
public class Query
{
    public string? Text { get; set; }

    public static async ValueTask<Query> BindAsync(
        HttpContext context,
        ParameterInfo parameter)
    {
        string? text = null;
        var request = context.Request;

        if (!request.Body.CanSeek)
        {
            // We only do this if the stream isn't *already* seekable,
            // as EnableBuffering will create a new stream instance
            // each time it's called
            request.EnableBuffering();
        }

        if (request.Body.CanRead)
        {
            request.Body.Position = 0;
            var reader = new StreamReader(request.Body, Encoding.UTF8);
            text = await reader.ReadToEndAsync().ConfigureAwait(false);
            request.Body.Position = 0;
        }

        return new Query { Text = text };
    }

    public static implicit operator string(Query query) // implicit digit to byte conversion operator
    {
        return query.Text ?? string.Empty; // implicit conversion
    }
}
public static class HttpQueryExtensions
{
    public static IEndpointConventionBuilder MapQuery(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<Query, IResult> requestDelegate)
    {
        return endpoints.MapMethods(pattern, new[] { "QUERY" }, requestDelegate);
    }

    public static IEndpointConventionBuilder MapQuery(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        RequestDelegate requestDelegate)
    {
        return endpoints.MapMethods(pattern, new[] { "QUERY" }, requestDelegate);
    }
}