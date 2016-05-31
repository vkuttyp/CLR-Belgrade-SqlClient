﻿using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Belgrade.SqlClient.Async
{
    /// <summary>
    /// Query component that streams results of SQL query into an oputput stream.
    /// </summary>
    public class QueryPipe : IQueryPipe
    {
        /// <summary>
        /// Connection to Sql Database.
        /// </summary>
        private SqlConnection Connection;

        /// <summary>
        /// Query mapper used to stream results.
        /// </summary>
        private QueryMapper Mapper;

        /// <summary>
        /// Creates Query object.
        /// </summary>
        /// <param name="connection">Connection to Sql Database.</param>
        public QueryPipe(SqlConnection connection)
        {
            this.Connection = connection;
            this.Mapper = new QueryMapper(connection);
        }

        /// <summary>
        /// Executes SQL query and put results into stream.
        /// </summary>
        /// <param name="sql">SQL query that will be executed.</param>
        /// <param name="stream">Output stream wehre results will be written.</param>
        /// <param name="defaultOutput">Default content that will be written into stream if there are no results.</param>
        /// <returns>Task</returns>
        public async Task Stream(string sql, Stream stream, string defaultOutput = "")
        {
            using (var command = new SqlCommand(sql, this.Connection))
            {
                await this.SqlResultsToStream(command, stream, defaultOutput);
            }
        }

        /// <summary>
        /// Executes SQL command and put results into stream.
        /// </summary>
        /// <param name="command">SQL command that will be executed.</param>
        /// <param name="stream">Output stream wehre results will be written.</param>
        /// <param name="defaultOutput">Default content that will be written into stream if there are no results.</param>
        /// <returns>Task</returns>
        public async Task Stream(SqlCommand command, Stream stream, string defaultOutput = "")
        {
            command.Connection = this.Connection;
            await this.SqlResultsToStream(command, stream, defaultOutput);
        }

        private async Task SqlResultsToStream(SqlCommand command, Stream stream, string defaultOutput)
        {
            try
            {
                await this.Mapper.ExecuteReader(command,
                    async reader =>
                    {
                        if (reader.HasRows)
                        {
                            var text = reader.GetString(0);
                            var buffеr = Encoding.UTF8.GetBytes(text);
                            await stream.WriteAsync(buffеr, 0, buffеr.Length).ConfigureAwait(false);
                            await stream.FlushAsync();
                        }
                        else
                        {
                            if (defaultOutput != "")
                                stream.Write(Encoding.UTF8.GetBytes(defaultOutput), 0, defaultOutput.Length);
                        }
                    });
            }
            finally
            {
                command.Connection.Close();
            }
        }
    }
}