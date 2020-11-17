using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace cping {
	class Program {
		static void Main(string[] args) {
			if (args.Length == 0) {
				Info();
				return;
			}

			bool name = "qwertyuiopasdfghjklzxcvbnm".Any(letter => args[0].Contains(letter));

			var argsDict = GetArgs(args);
			int requestsNumber = argsDict["n"];
			int dataLength = argsDict["l"] > 65500 ? 65500 : argsDict["l"];
			byte[] buffer = new byte[dataLength];
			PingOptions options = new PingOptions(argsDict["i"], true);

			int lost = 0;
			long min, max, sum;

			try {
				Ping ping = new Ping();
				PingReply firstReply = ping.Send(args[0], 1, buffer, options);
				if (firstReply.Status != IPStatus.Success)
					PrintResult(firstReply);
				else {
					sum = min = max = firstReply.RoundtripTime;
					string printThis = name ? $"{args[0]} [{firstReply.Address}]" : $"{args[0]}";
					Console.WriteLine($"Обмен пакетами с {printThis} с {dataLength} байтами данных");
					PrintResult(firstReply);

					for (int i = 0; i < requestsNumber - 1; i++) {
						PingReply reply = ping.Send(args[0], 1, buffer, options);
						sum += reply.RoundtripTime;
						min = reply.RoundtripTime < min ? reply.RoundtripTime : min;
						max = reply.RoundtripTime > max ? reply.RoundtripTime : max;
						PrintResult(reply);
						if (reply.Status != IPStatus.Success)
							lost++;
					}

					Console.WriteLine(
						$"\nСтатистика cping для {firstReply.Address}:" +
						$"\n    Пакетов: отправлено = {requestsNumber}, получено = {requestsNumber - lost}, потеряно = {lost}" +
						$"\n    ({lost / requestsNumber}% потерь)" +
						$"\nПриблизительное время приёма-передачи в мс:" +
						$"\n    Минимальное = {min}мс, Максимальное = {max}мс, Среднее = {sum / requestsNumber}мс");
				}
			}
			catch (PingException) {
				Console.WriteLine($"Не удалось обнаружить узел {args[0]}");
			}
		}

		static void PrintResult(PingReply reply) {
			Console.WriteLine(
			reply.Status switch {
				IPStatus.Success => $"Ответ от {reply.Address}: число байт={reply.Buffer.Length} время={reply.RoundtripTime}мс TTL={reply.Options.Ttl}",
				IPStatus.PacketTooBig => "Размер пакета слишком большой",
				IPStatus.TtlExpired => "Время жизни пакета истекло",
				IPStatus.TimedOut => "Время ожидания истекло",
				IPStatus.BadDestination => $"Не удалось обнаружить узел {reply.Address}",
				IPStatus.Unknown => "Ошибка в передаче пакета",
				_ => "Неизвестная ошибка в передаче пакета",
			});
		}

		static Dictionary<string, int> GetArgs(string[] args) {
			var result = new Dictionary<string, int> {
				["n"] = 3,
				["l"] = 32,
				["i"] = 64
			};

			if (args.Length != 1) {
				for (int i = 1; i < args.Length; i += 2) {
					if (!result.ContainsKey(args[i].TrimStart('-', '/'))) {
						Console.WriteLine("cping не принимает параметр -" + args[i]);
						continue;
					}
					if (!int.TryParse(args[i + 1], out int value)) {
						Console.WriteLine($"Параметр {args[i]} не принимает значение {args[i + 1]}");
						continue;
					}
					if (value < 1) {
						Console.WriteLine($"Неверно задано значение {args[i]} (Минимальная допустимая величина: 1)");
						continue;
					}
					if (args[i] == "-i" && value > 255) {
						Console.WriteLine($"Неверное задано значение {args[i]} (Максимальная допустимая величина: 255)");
						continue;
					}
					result[args[i].TrimStart('-', '/')] = value;
				}
			}

			return result;
		}

		static void Info() {
			Console.WriteLine(
				"Использование: cping <имя узла/адрес узла>" +
				"\nПараметры:" +
				"\n  -n <значение> - кол-во запросов" +
				"\n  -l <значение> - размер отправляемого пакета" +
				"\n  -i <значение> - время жизни пакета");
		}
	}
}
