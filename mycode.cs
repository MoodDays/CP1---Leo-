using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class PortScanFunction
{
    private static readonly Dictionary<int, string> serviceMap = new Dictionary<int, string>()
    {
        { 22, "SSH" },
        { 80, "HTTP" },
        { 443, "HTTPS" },
        { 8080, "HTTP Alt" }
        // Adicione mais portas e serviços conforme necessário
    };

    [Function("PortScanFunction")]
    public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
    {
        var response = req.CreateResponse();

        var result = new List<PortScanResult>();

        foreach (var port in serviceMap.Keys)
        {
            var portScanResult = new PortScanResult
            {
                Port = port,
                IsOpen = await IsPortOpenAsync("cp.leosantos.seg.br", port),
                Service = serviceMap[port]
            };

            result.Add(portScanResult);
        }

        var jsonResult = JsonSerializer.Serialize(result);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(jsonResult);
        return response;
    }

    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        try
        {
            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(host, port);
                if (await Task.WhenAny(connectTask, Task.Delay(500)) == connectTask)
                {
                    await connectTask; // Ensure any exceptions are thrown
                    return true;
                }
                return false;
            }
        }
        catch (Exception)
        {
            return false;
        }
    }
}

public class PortScanResult
{
    public int Port { get; set; }
    public bool IsOpen { get; set; }
    public string Service { get; set; }
}
