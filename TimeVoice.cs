using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.Metrics;
using System.Xml.Linq;

namespace supportBotGaga
{
    public class TimeVoice
    {
        private readonly IConfiguration _config;
        private static string span;

        public TimeVoice(SocketUser user)
        {
            // create the configuration
            var _builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path: "config.json");

            // build the configuration and assign to _config          
            _config = _builder.Build();
        }

        private static readonly Dictionary<ulong, DateTime> _voiceTime = new Dictionary<ulong, DateTime>();
        private static Dictionary<ulong, double> _userVoiceActivityPoints = new Dictionary<ulong, double>();


        public static async Task OnVoiceStateUpdatedAsync(SocketUser user, SocketVoiceState oldState, SocketVoiceState newState)
        {
            // Проверяем, является ли пользователь ботом
            if (user.IsBot)
            {
                await Task.CompletedTask;
            }

            // Получаем пользователя по DiscordId
            var reader = RequestHandler.ExecuteReader($"SELECT VoiceTime From Users WHERE DiscordId = '{user.Id}'");          
            while (reader.Read()) // построчно считываем данные
            {
                int a = 0;
                a++;
                if (a == 1) { span = reader.GetValue(0).ToString(); }
            }

            // Проверяем, найден ли пользователь
            if (!reader.HasRows)
            {
                Console.WriteLine("User not data base.");
                // Если пользователь не найден, создаем новую запись в таблице
                Console.WriteLine("Create user data base.");
                _ = RequestHandler.ExecuteWrite($"INSERT INTO Users (DiscordId, MessagesCount, Currency, LikesCount, LastActivity, VoiceTime) " +
                    $"VALUES('{user.Id}', 0, 0, 0, GETDATE(), '00:00')");
            }

            if (oldState.VoiceChannel == newState.VoiceChannel)
                await Task.CompletedTask;

            if (!_voiceTime.TryGetValue(user.Id, out var lastJoinTime))
                lastJoinTime = DateTime.UtcNow;

            var timeSpent = DateTime.UtcNow - lastJoinTime;

            TimeSpan timeSpan = TimeSpan.Parse(span); // получаем значение из SqlDataReader и преобразуем в TimeSpan
            timeSpent = timeSpent.Add(timeSpan); // выполняем операцию сложения с помощью метода Add()

            UpdateVoiceActivityPoints(user, timeSpent);

            // Сохраняем время проведенное в голосовом канале в базе данных или отправляем сообщение пользователю
            _ = RequestHandler.ExecuteWrite($"UPDATE Users SET VoiceTime = '{timeSpent.ToString(@"hh\:mm\:ss")}' WHERE DiscordId = '{user.Id}'");

            if (newState.VoiceChannel == null)
                _voiceTime.Remove(user.Id);
            else
                _voiceTime[user.Id] = DateTime.UtcNow;

            await Task.CompletedTask;
        }

        private static void UpdateVoiceActivityPoints(SocketUser user, TimeSpan timeSpent)
        {
            // Проверяем, если пользователь достиг максимального увеличения баллов, не обновляем баллы
            if (_userVoiceActivityPoints.ContainsKey(user.Id) && _userVoiceActivityPoints[user.Id] >= 2.0)
                return;

            // Вычисляем количество баллов, которое будет начислено за проведенное время в голосовом чате
            var pointsToAdd = timeSpent.TotalMinutes >= 10.0 ? 2.0 : Math.Floor(timeSpent.TotalMinutes) * 0.1;

            // Обновляем баллы активности пользователя
            if (_userVoiceActivityPoints.ContainsKey(user.Id))
                _userVoiceActivityPoints[user.Id] += pointsToAdd;
            else
                _userVoiceActivityPoints[user.Id] = pointsToAdd;

            Console.WriteLine($"Voice Channel {user.Id} , {timeSpent} , {(decimal)_userVoiceActivityPoints[user.Id]}");

            _ = RequestHandler.ExecuteWrite($"UPDATE Users SET Currency = Currency + CONVERT(decimal, {_userVoiceActivityPoints[user.Id]}) WHERE DiscordId = '{user.Id}'");

            _userVoiceActivityPoints.Remove(user.Id);
        }
    }
}
