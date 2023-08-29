using Polly;
using System.Net;
using System.Text;

namespace DesafioCodeBreaker
{
    public class Program
    {
        private static readonly int _minKeyNumber = 1;
        private static readonly int _maxKeyNumber = 10000;
        private static readonly string _apiUrl = "https://localhost:7038/fiap";

        private static int _counter = 0;

        static void Main(string[] args)
        {
            var keys = GenerateShuffledKeys();

            CancellationTokenSource cts = new();

            var options = new ParallelOptions()
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            Parallel.ForEach(keys, options, async key =>
            {
                var validKey = await ValidateKey(key);

                if (validKey)
                {
                    Console.WriteLine($"////////////////////////CHAVE ENCONTRADA: {key}");
                    cts.Cancel();
                }
                else
                {
                    Console.WriteLine($"[{_counter}/{keys.Count}]: {key}");
                }
            });

            Console.ReadKey();
        }

        static List<string> GenerateShuffledKeys()
        {
            char[] chars = { 'a', 'á', 'à', 'â', 'ã', 'b', 'c', 'ç', 'd', 
                             'e', 'é', 'è', 'ê', 'f', 'g', 'h', 'i', 'í', 
                             'ì', 'î', 'j', 'k', 'l', 'm', 'n', 'o', 'ó', 
                             'ò', 'ô', 'õ', 'p', 'q', 'r', 's', 't', 'u', 
                             'ú', 'ù', 'û', 'v', 'w', 'x', 'y', 'z' };

            List<string> keys = new List<string>();

            for(int number = _minKeyNumber; number <= _maxKeyNumber; number++)
            {
                foreach(var chr in chars)
                {
                    keys.Add($"{number}{chr}");
                    keys.Add($"{chr}{number}");
                    keys.Add($"{number}{chr.ToString().ToUpper()}");
                    keys.Add($"{chr.ToString().ToUpper()}{number}");
                }
            }

            keys = keys.OrderBy(a => Guid.NewGuid()).ToList();

            return keys;
        }

        static async Task<bool> ValidateKey(string key)
        {
            var httpClient = new HttpClient();

            var httpContent = new StringContent(
                                    $"{{\"key\": \"{key}\"}}",
                                    Encoding.UTF8, 
                                    "application/json");

            var response = await Policy
                                    .Handle<Exception>()
                                    .OrResult<HttpResponseMessage>(message => message.StatusCode != HttpStatusCode.OK && message.StatusCode != HttpStatusCode.NoContent)
                                    .WaitAndRetryAsync(new[]
                                    {
                                        TimeSpan.FromSeconds(1),
                                        TimeSpan.FromSeconds(3),
                                        TimeSpan.FromSeconds(9),
                                        TimeSpan.FromSeconds(15),
                                    }, (result, timeSpan, retryCount, context) => {
                                        Console.WriteLine($"key {key} failed, retrycount = {retryCount}. waiting {timeSpan} before next retry.");
                                    })
                                    .ExecuteAsync(() => httpClient.PostAsync(_apiUrl, httpContent));

            _counter++;
            return response.StatusCode == HttpStatusCode.OK;
        }
    }
}