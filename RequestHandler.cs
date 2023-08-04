using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace supportBotGaga
{
    public  class RequestHandler
    {
        // create the configuration
        private static IConfigurationBuilder _builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile(path: "config.json");

        public static readonly IConfiguration _config = _builder.Build();

        public static int ExecuteWrite(string request)
        {
            // Создаем SQL-запрос для добавления пользователя в таблицу users
            var query = request;

            // Выполняем запрос
            using (var connection = new SqlConnection(_config["conectionStrings"]))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static SqlDataReader ExecuteReader(string query)
        {
            var connection = new SqlConnection(_config["conectionStrings"]);
            connection.Open();
            var command = new SqlCommand(query, connection);
            var reader = command.ExecuteReader(CommandBehavior.CloseConnection);
            return reader;
        }
    }
}
