#nullable enable

using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

// Smoke test of the JetBrains fork of ModelContextProtocol.Core under System.Text.Json 8.
//   json                  enum wire names through McpJsonUtilities.DefaultOptions
//   server                stdio MCP server built on the fork
//   stdio <host-command>  client: starts this assembly's server mode through <host-command> and talks to it
//   http <url> <mode>     client: talks to an MCP server over HTTP, mode StreamableHttp or Sse
int failures = 0;
void Check(bool isOk, string what)
{
    Console.Error.WriteLine($"{(isOk ? "PASS" : "FAIL")} {what}");
    if(!isOk) failures++;
}

string runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
string stjVersion = typeof(JsonSerializer).Assembly.GetName().Version!.ToString();
Console.Error.WriteLine($"runtime={runtime} System.Text.Json={stjVersion} mode={args[0]}");

switch(args[0])
{
    case "json":
        void Wire<T>(T value, string expected)
        {
            string json = JsonSerializer.Serialize(value, McpJsonUtilities.DefaultOptions);
            T back = JsonSerializer.Deserialize<T>($"\"{expected}\"", McpJsonUtilities.DefaultOptions)!;
            Check((json == $"\"{expected}\"") && EqualityComparer<T>.Default.Equals(back, value), $"{typeof(T).Name}.{value} <-> {json}");
        }
        Wire(Role.User, "user");
        Wire(Role.Assistant, "assistant");
        Wire(LoggingLevel.Warning, "warning");
        Wire(LoggingLevel.Emergency, "emergency");
        Wire(ContextInclusion.ThisServer, "thisServer");
        Wire(ContextInclusion.AllServers, "allServers");
        string message = JsonSerializer.Serialize(new LoggingMessageNotificationParams { Level = LoggingLevel.Critical, Data = JsonSerializer.SerializeToElement("x", McpJsonUtilities.DefaultOptions) }, McpJsonUtilities.DefaultOptions);
        Check(message.Contains("\"level\":\"critical\""), $"enum inside a message: {message}");
        break;

    case "server":
        var options = new McpServerOptions
        {
            ServerInfo = new Implementation { Name = "fork-smoke", Version = "1.0" },
            ToolCollection =
            [
                McpServerTool.Create((string text) => "echo:" + text, new McpServerToolCreateOptions { Name = "echo" }),
                McpServerTool.Create(async (McpServer server, Role role, CancellationToken cancellationToken) =>
                {
                    // A notification carrying an enum, to check the wire name on the way out of the server.
                    await server.SendNotificationAsync(NotificationMethods.LoggingMessageNotification, new LoggingMessageNotificationParams { Level = LoggingLevel.Warning, Data = JsonSerializer.SerializeToElement("from-tool", McpJsonUtilities.DefaultOptions) }, cancellationToken: cancellationToken);
                    return "role:" + role;
                }, new McpServerToolCreateOptions { Name = "classify" }),
            ],
        };
        await using(McpServer server = McpServer.Create(new StdioServerTransport("fork-smoke"), options))
            await server.RunAsync();
        return 0;

    case "stdio":
        string self = typeof(Program).Assembly.Location;
        var transport = new StdioClientTransport(new StdioClientTransportOptions { Name = "fork-smoke", Command = args[1], Arguments = [self, "server"] });
        await using(McpClient client = await McpClient.CreateAsync(transport))
        {
            Check(client.ServerInfo.Name == "fork-smoke", $"initialize over stdio, server {client.ServerInfo.Name}, protocol {client.NegotiatedProtocolVersion}");
            IList<McpClientTool> tools = await client.ListToolsAsync();
            Check(tools.Count == 2, $"tools: {string.Join(", ", tools.Select(tool => tool.Name))}");
            string schema = tools.Single(tool => tool.Name == "classify").ProtocolTool.InputSchema.GetRawText();
            Check(schema.Contains("\"user\"") && schema.Contains("\"assistant\""), $"enum schema: {schema}");

            // Notifications are dispatched apart from the call's response, so the level is awaited rather than read once the call returns.
            var level = new TaskCompletionSource<LoggingLevel>(TaskCreationOptions.RunContinuationsAsynchronously);
            await using(client.RegisterNotificationHandler(NotificationMethods.LoggingMessageNotification, (notification, _) =>
            {
                Console.Error.WriteLine($"     notification params: {notification.Params?.ToJsonString()}");
                level.TrySetResult(JsonSerializer.Deserialize<LoggingMessageNotificationParams>(notification.Params, McpJsonUtilities.DefaultOptions)!.Level);
                return default;
            }))
            {
                CallToolResult echo = await client.CallToolAsync("echo", new Dictionary<string, object?> { ["text"] = "hi" });
                Check(echo.Content.OfType<TextContentBlock>().Single().Text == "echo:hi", "call echo");
                CallToolResult classify = await client.CallToolAsync("classify", new Dictionary<string, object?> { ["role"] = "assistant" });
                Check(classify.Content.OfType<TextContentBlock>().Single().Text == "role:Assistant", $"call classify with an enum argument: {classify.Content.OfType<TextContentBlock>().Single().Text}");
                bool hasArrived = (await Task.WhenAny(level.Task, Task.Delay(TimeSpan.FromSeconds(10)))) == level.Task;
                Check(hasArrived && (level.Task.Result == LoggingLevel.Warning), $"logging notification level: {(hasArrived ? level.Task.Result.ToString() : "none within 10 s")}");
            }
        }
        break;

    case "http":
        var httpTransport = new HttpClientTransport(new HttpClientTransportOptions { Endpoint = new Uri(args[1]), TransportMode = (HttpTransportMode)Enum.Parse(typeof(HttpTransportMode), args[2]) });
        await using(McpClient client = await McpClient.CreateAsync(httpTransport))
        {
            Check(client.ServerInfo is not null, $"initialize over {args[2]}, server {client.ServerInfo?.Name}, protocol {client.NegotiatedProtocolVersion}");
            IList<McpClientTool> tools = await client.ListToolsAsync();
            Check(tools.Any(tool => tool.Name == "echo"), $"tools: {string.Join(", ", tools.Select(tool => tool.Name))}");
            CallToolResult echo = await client.CallToolAsync("echo", new Dictionary<string, object?> { ["text"] = "hi" });
            Check(echo.Content.OfType<TextContentBlock>().Single().Text == "http-echo:hi", $"call echo over {args[2]}");
        }
        break;
}

Console.Error.WriteLine(failures == 0 ? "ALL PASS" : $"{failures} FAILED");
return failures == 0 ? 0 : 1;
