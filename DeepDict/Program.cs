namespace Cc.Moonlink.Deepdict;

using System;
using System.Net.Http;
using System.Net.Http.Json; // 需要 .NET 6+ 
using System.Text.Json;
using System.Threading.Tasks;

class DeepDict
{
    // 配置部分：建议将 API Key 存放在环境变量中
    private static readonly string ApiKey = "sk-bea5be4d181e4be8b1834124fcaea6e2"; 
    private static readonly string ApiUrl = "https://api.deepseek.com/chat/completions";

    static async Task Main(string[] args)
    {
        // 1. 获取命令行输入 (如: dd apple)
        if (args.Length == 0)
        {
            Console.WriteLine("用法: dd <单词或句子>");
            return;
        }

        string word = string.Join(" ", args);
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("DeepDict is querying: {word}...");
        Console.ResetColor();

        // 2. 发起请求
        await QueryDeepSeek(word);
    }

    static async Task QueryDeepSeek(string query)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

        var requestBody = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "system", content = "你是一个简洁的词典助手。用户输入单词或句子，你只需提供：1.音标(如果是单词) 2.核心中文释义，多义则以分号划分 3.一个地道的例句。不要废话。" },
                new { role = "user", content = query }
            },
            temperature = 0.3 // 调低随机性，让翻译更稳
        };

        try
        {
            var response = await client.PostAsJsonAsync(ApiUrl, requestBody);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ 错误: API 请求失败 (状态码: {response.StatusCode})");
                return;
            }

            var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
            string result = jsonResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // 3. 漂亮地输出结果
            Console.WriteLine("------------------------------------------");
            Console.WriteLine(result.Trim());
            Console.WriteLine("------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"caught exception: {ex.Message}");
        }
    }
}