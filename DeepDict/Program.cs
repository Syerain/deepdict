namespace Cc.Moonlink.Deepdict;

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

class DeepDict
{
    private static readonly string ConfigFileName = "api-key.dd.conf";
    private static readonly string ApiUrl = "https://api.deepseek.com/chat/completions";

    static async Task Main(string[] args)
    {
        // 1. 检查命令行参数
        // args[0] 就是 apple, 如果是 dd keep an eye on, args 就会有多个元素
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: dd <word_or_phrase>");
            Console.WriteLine("Example: dd apple");
            return;
        }

        // 2. 将所有参数拼接成一个完整的待查询字符串
        string queryText = string.Join(" ", args);

        // 3. 获取配置文件中的 API Key
        string apiKey = await GetApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey)) return;

        // 4. 发起查询
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"DeepDict quirying': \"{queryText}\"...");
        Console.ResetColor();

        await QueryDeepSeek(queryText, apiKey);
        
    }

    private static async Task<string> GetApiKeyAsync()
    {
        // 始终从 .exe 所在目录寻找配置文件
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);

        if (!File.Exists(configPath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Missing config file 'api-key.dd.conf' in : {configPath}");
            Console.ResetColor();
            return null;
        }

        string key = await File.ReadAllTextAsync(configPath);
        return key.Trim();
    }

    static async Task QueryDeepSeek(string query, string apiKey)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                // 这里我们稍微增强了 Prompt，让它根据输入类型自动调整输出
                new { role = "system", content = "你是一个简易的词典助手，你接收用户的目标词汇或句子，只需提供：1.国际音标（如果目标输入不是英文，则提供目标词的英文拼读+国际音标）2.词性及其中文释义，有多项的话，近义以逗号划分，多义以分号划分 3.简单地道的例句及其中文释义。此外不要有任何其他废话，不要使用 markdown 标记" }
            ,
                new { role = "user", content = query }
            },
            temperature = 0.2 // 降低随机性，结果更准确
        };

        try
        {
            var response = await client.PostAsJsonAsync(ApiUrl, requestBody);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"API Error: {(int)response.StatusCode} {response.ReasonPhrase}");
                return;
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            string result = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(" done.");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine(result.Trim());
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}