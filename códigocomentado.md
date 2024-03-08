// Usamos bibliotecas que nos ajudam a fazer a função funcionar, conectar na internet e trabalhar com formatos de texto específicos.
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

public class PortScanFunction
{
    // Aqui guardamos uma lista de portas conhecidas e o que cada uma geralmente faz.
    private static readonly Dictionary<int, string> serviceMap = new Dictionary<int, string>()
    {
        { 22, "SSH" },   // Usado para conectar seguramente a outro computador.
        { 80, "HTTP" },  // Usado para sites da web normais.
        { 443, "HTTPS" }, // Como o HTTP, mas seguro.
        { 8080, "HTTP Alt" } // Outra porta para sites da web, mas não padrão.
        // Podemos adicionar mais portas aqui conforme a necessidade.
    };

    // Esse pedaço diz que a função responde a pedidos do tipo GET na web.
    [Function("PortScanFunction")]
    public static async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
    {
        // Cria uma resposta que vamos preencher e enviar de volta para quem pediu.
        var response = req.CreateResponse();

        // Lista para manter os resultados do nosso teste.
        var result = new List<PortScanResult>();

        // Passa por cada porta que conhecemos para ver se está aberta.
        foreach (var port in serviceMap.Keys)
        {
            // Testa a porta e guarda o resultado.
            var portScanResult = new PortScanResult
            {
                Port = port,
                IsOpen = await IsPortOpenAsync("cp.leosantos.seg.br", port), // Testa a porta de forma assíncrona.
                Service = serviceMap[port] // Diz que serviço é esperado nesta porta.
            };

            // Adiciona o resultado à nossa lista.
            result.Add(portScanResult);
        }

        // Converte nossa lista de resultados em texto no formato JSON.
        var jsonResult = JsonSerializer.Serialize(result);
        response.Headers.Add("Content-Type", "application/json");
        response.WriteString(jsonResult);

        // Manda a resposta de volta.
        return response;
    }

    // Método para testar se uma porta está aberta usando internet. Usa "async" para não travar enquanto espera a resposta.
    private static async Task<bool> IsPortOpenAsync(string host, int port)
    {
        try
        {
            // Tenta conectar ao servidor na porta especificada.
            using (var tcpClient = new TcpClient())
            {
                var connectTask = tcpClient.ConnectAsync(host, port);
                // Espera um pouco para ver se conecta. Se demorar, considera que falhou.
                if (await Task.WhenAny(connectTask, Task.Delay(500)) == connectTask)
                {
                    // Se conectou, a porta está aberta.
                    return true;
                }
                // Se não conectou rápido, considera que a porta não está aberta.
                return false;
            }
        }
        catch (Exception)
        {
            // Se deu algum erro na tentativa, também considera que a porta não está aberta.
            return false;
        }
    }
}

// Uma classe simples para guardar informações sobre o resultado do teste de cada porta.
public class PortScanResult
{
    public int Port { get; set; } // O número da porta que testamos.
    public bool IsOpen { get; set; } // Se a porta está aberta ou não.
    public string Service { get; set; } // Que tipo de serviço geralmente usa essa porta.
}
