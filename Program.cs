using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;

namespace databases {
    class Program {
        private static void Query_Database (String db, String dbPassword) {
            using (SqlConnection connection = new SqlConnection (ConnectionString (dbPassword))) {
                connection.Open ();
                // Do work here; connection closed on following line.
                Console.WriteLine ($"Connected to {connection.Database}");
                String query = $"SELECT * FROM {db}";
                using (SqlCommand cmd = new SqlCommand (query, connection)) {
                    using (SqlDataReader reader = cmd.ExecuteReader ()) {
                        while (reader.Read ()) {
                            Console.WriteLine ($"Title: {reader.GetString(1)}, Year: {reader.GetInt32(2)}");
                        }
                    }
                }
            }
        }

        private static void InsertMovies (String table, string dbPassword, String csvPath) {
            // check if table does not exist
            String connString = ConnectionString (dbPassword);
            if (TableDoesNotExist (table, connString)) {
                Console.WriteLine ("Table does not exist");
            }
            // read in movies from csv
            Movie[] movies = ReadFieldsFromCSV (csvPath);

            // Create SQL Insert commands
            SqlCommand[] insertCmds = CreateInsertCommands (movies);

            // Run transactional Inserts all at once
            RunInsertCommands (insertCmds, connString);
        }

        private static SqlCommand[] CreateInsertCommands (Movie[] movies) {
            string commandText = "INSERT INTO MOVIE VALUES (@Title, @Year, @Director, @IMDB_Rating, @Genre);";
            SqlCommand[] commands = new SqlCommand[movies.Length];
            int index = 0;
            foreach (Movie m in movies) {
                if (m == null) {
                    Console.WriteLine ("null movie");
                }
                SqlCommand command = new SqlCommand (commandText);
                command.Parameters.Add ("@Title", SqlDbType.VarChar);
                command.Parameters["@Title"].Value = m.Title1;

                command.Parameters.Add ("@Year", SqlDbType.Int);
                command.Parameters["@Year"].Value = m.Year1;

                command.Parameters.Add ("@Director", SqlDbType.VarChar);
                command.Parameters["@Director"].Value = m.Director1;

                command.Parameters.Add ("@IMDB_Rating", SqlDbType.Decimal);
                command.Parameters["@IMDB_Rating"].Value = m.IMDB_rating1;

                command.Parameters.Add ("@Genre", SqlDbType.VarChar);
                command.Parameters["@Genre"].Value = m.Genre1;

                commands[index++] = command;
            }
            return commands;
        }
        private static void RunInsertCommands (SqlCommand[] commands, String connString) {
            using (SqlConnection conn = new SqlConnection (connString)) {
                conn.Open ();
                SqlTransaction transaction;
                transaction = conn.BeginTransaction ("insertTransaction");
                // run each command within the transaction
                foreach (SqlCommand cmd in commands) {
                    cmd.Transaction = transaction;
                    cmd.Connection = conn;
                    // this next part is where you might run into 
                    // problems so a try / catch is important
                    try {
                        cmd.ExecuteNonQuery ();
                    } catch (System.Exception ex) {
                        Console.WriteLine ($"Exception Type: {ex.GetType()}");
                        Console.WriteLine ($"Exception: {ex.Message}");
                        // attempt to rollback the transaction
                        try {
                            transaction.Rollback ();
                        } catch (System.Exception ex2) {
                            Console.WriteLine ($"Rollback exception: {ex2.GetType()}");
                            Console.WriteLine ($"Exception: {ex2.Message}");
                        }
                    }
                }
                transaction.Commit();
                Console.WriteLine($"Committed {commands.Length} new movies to the database.");
            }
        }

        private static int TotalLines (string filePath) {
            using (StreamReader r = new StreamReader (filePath)) {
                int i = 0;
                while (r.ReadLine () != null) { i++; }
                return i;
            }
        }

        private static Movie[] ReadFieldsFromCSV (string csvPath) {
            int totalLines = TotalLines (csvPath) - 1;
            Movie[] movies = new Movie[totalLines];
            using (StreamReader sr = new StreamReader (csvPath)) {
                // does row 0 have a header row?
                String line = sr.ReadLine();
                String[] words = line.Split(',');
                if(words[5] != "Title") {
                    throw new Exception ("Missing Header row in csv file.");                    
                }
                int iterator = 0;
                while (!sr.EndOfStream) {
                    line = sr.ReadLine();
                    words = line.Split(',');
                    try {
                        Movie newMovie = new Movie (words[5], Int32.Parse (words[10]), words[14], Decimal.Parse (words[8]), words[11]);
                        movies[iterator++] = newMovie;
                    } catch (System.IndexOutOfRangeException e) {
                        Console.WriteLine (e.ToString ());
                    } catch (System.FormatException e) {
                        Console.WriteLine ($"Title: {words[5]}");
                        Console.WriteLine ($"Year: {words[10]}");
                        Console.WriteLine (e.ToString ());
                    }
                }
            }
            // check if number of movies equals TotalLines
            if (totalLines != movies.Length) {
                throw new Exception ("You did not read the right number of lines from the csv file");
            }
            return movies;
        }

        private static bool TableDoesNotExist (String table, String connString) {
            using (SqlConnection conn = new SqlConnection (connString)) {
                conn.Open ();
                String query = $@"select case when exists
                ((select * from information_schema.tables where table_name = '{table}')) 
                then 1 else 0 end";
                using (SqlCommand cmd = new SqlCommand (query, conn)) {
                    return (int) cmd.ExecuteScalar () == 0;
                }
            }
        }

        private static String ConnectionString (String dbPassword) {
            // Build connection string
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder ();
            builder.DataSource = "localhost";
            builder.UserID = "sa";
            builder.Password = dbPassword;
            builder.InitialCatalog = "master";
            return builder.ConnectionString;
        }

        
        /// How to run: 
        /// dotnet run <Your-Database-Password> <Your-CSV-File>
        /// This program reads from a .csv file given 
        /// as the 2nd command line argument. It reads all the movies 
        /// from that file, extracts the relevant data, then inserts 
        /// it into my local MSSQL DB running on Docker.
        /// 
        static int Main (string[] args) {
            if (args.Length == 0) {
                Console.WriteLine ("Please enter a textual argument for your database password and a path to your csv file");
                return 1;
            }
            Console.WriteLine("Inserting Movies...");
            InsertMovies ("Movie", args[0], args[1]);
            return 0;
        }
    }
}