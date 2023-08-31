﻿using Polly;
using System.Net;
using System.Text;

namespace DesafioCodeBreaker
{
    public class Program
    {
        private static readonly int _minKeyNumber = 1;
        private static readonly int _maxKeyNumber = 10000;
        private static readonly string _apiUrl = "https://fiap-inaugural.azurewebsites.net/fiap";
        private static readonly string _txtTestedNumbersPath =
            @$"{AppDomain.CurrentDomain.BaseDirectory}\TESTED_NUMBERS.txt";

        private static readonly Random _random = new Random();
       
        static async Task Main(string[] args)
        {
            var numbersToProcess = GenerateShuffledIntList()
                                    .Except(GetTestedNumbers())
                                    .ToList();

            foreach (int i in numbersToProcess)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Numero testado: {i}");

                var keys = GenerateShuffledPossibleKeys(i);

                var httpClient = new HttpClient();

                var tasks = new List<Task<(string, bool)>>();
                foreach (var key in keys)
                    tasks.Add(ValidateKey(httpClient, key));

                (string, bool)[] responses = await Task.WhenAll(tasks);

                var successKey = responses.FirstOrDefault(r => r.Item2);

                if (successKey.Item1 != null)
                {
                    Console.WriteLine($"////////////////////////CHAVE ENCONTRADA: {successKey}");
                    break;
                }

                SaveTestedNumber(i);
            }

            Console.ReadKey();
        }

        static List<string> GenerateShuffledPossibleKeys(int number)
        {
            var chars = new List<char>
                { 'a', 'á', 'à', 'â', 'ã', 'b', 'c', 'ç', 'd',
                  'e', 'é', 'è', 'ê', 'f', 'g', 'h', 'i', 'í',
                  'ì', 'î', 'j', 'k', 'l', 'm', 'n', 'o', 'ó',
                  'ò', 'ô', 'õ', 'p', 'q', 'r', 's', 't', 'u',
                  'ú', 'ù', 'û', 'v', 'w', 'x', 'y', 'z' };

            //capital letters
            chars.AddRange(
                chars.Select(c => char.ToUpper(c)).ToList()
            );

            List<string> keys = new List<string>();


            foreach (var chrLeft in chars)
            {
                foreach (var chrRight in chars)
                {
                    keys.Add($"{chrLeft}{number}{chrRight}");
                }

                keys.Add($"{chrLeft}{number}");
                keys.Add($"{number}{chrLeft}");
            }

            return Shuffle(keys);
        }

        static List<int> GenerateShuffledIntList()
        {
            var list = Enumerable.Range(_minKeyNumber,
                                        _maxKeyNumber - _minKeyNumber + 1).ToList();

            return Shuffle(list);
        }

        static async Task<(string, bool)> ValidateKey(HttpClient httpClient, string key)
        {
            var httpContent = new StringContent(
                                    $"{{\"key\": \"{key}\"}}",
                                    Encoding.UTF8,
                                    "application/json");

            var response = await Policy
                                    .Handle<Exception>()
                                    .OrResult<HttpResponseMessage>(message => message.StatusCode != HttpStatusCode.OK && message.StatusCode != HttpStatusCode.NoContent)
                                    .WaitAndRetryAsync(new[]
                                    {
                                        TimeSpan.FromSeconds(3),
                                        TimeSpan.FromSeconds(9),
                                        TimeSpan.FromSeconds(15),
                                    }, (result, timeSpan, retryCount, context) =>
                                    {
                                        Console.WriteLine($"key {key} failed, retrycount = {retryCount}. waiting {timeSpan} before next retry.");
                                    })
                                    .ExecuteAsync(() => httpClient.PostAsync(_apiUrl, httpContent));

            return (key, response.StatusCode == HttpStatusCode.OK);
        }

        static List<T> Shuffle<T>(List<T> list)
        {
            int n = list.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }

            return list;
        }

        static List<int> GetTestedNumbers()
        {
            List<int> numbers = new List<int>();

            if (!File.Exists(_txtTestedNumbersPath))
            {
                Console.WriteLine("Arquivo txt não encontrado.");
                return numbers;
            }

            try
            {
                Console.WriteLine($"Consultando arquivo {_txtTestedNumbersPath}");

                using (StreamReader reader = new StreamReader(_txtTestedNumbersPath))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (line is null) break;

                        numbers.Add(int.Parse(line));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocorreu um erro ao consultar o arquivo txt: " + ex.Message);
            }

            Console.WriteLine($"{numbers.Count} numeros já foram testados anteriormente.");

            return numbers;
        }

        static void SaveTestedNumber(int number)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(_txtTestedNumbersPath, true))
                    writer.WriteLine(number);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ocorreu um erro ao gravar no arquivo txt: " + ex.Message);
            }
        }
    }
}