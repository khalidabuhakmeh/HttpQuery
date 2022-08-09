# Experimental HTTP Methods With Minimal API Endpoints

This is a sample repository showing Minimal API endpoints handling the
experimental HTTP Method `Query` (https://nordicapis.com/your-guide-to-the-new-http-query-method/).

The Query method will behave similarly to `GET` but will also have the
potential for clients to sent a request body, which isn't normally supported by
the `GET` method.

## Getting Started

You'll need .NET 7, but I guess you could also target .NET 6 too.

## The Handling Endpoint

I've added a `MapQuery` call, similar to `MapGet` and other endpoints. The input type is of
`Query` which will read the body and give you the query. What the query is, is up to
you and your endpoint implementation.

I chose Dynamic LINQ **(bad idea for production environments)**, just to illustrate the point.

```c#
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
```

## Calling The Endpoint

Since the HTTP Method is experimental, no tooling out there will likely support calling it. 
You'll need C# code to do that.

```c#
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
```

**Again, DYNAMIC LINQ IS A BAD IDEA!**. It let's folks build dynamic C# code and execute code
on your server. Don't do this outside of demos and then be careful where those demos go.