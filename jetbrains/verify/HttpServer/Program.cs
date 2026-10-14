using System.ComponentModel;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateful;
        options.EnableLegacySse = true;
    })
    .WithTools<HttpTools>();
var app = builder.Build();
app.MapMcp();
app.Run();

[McpServerToolType]
public sealed class HttpTools
{
    [McpServerTool, Description("Echoes the text back.")]
    public static string Echo(string text) => "http-echo:" + text;
}
