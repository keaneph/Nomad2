using MySql.Data.MySqlClient;
using System;

namespace Nomad2.Services
{
    //serves as db and table initalization.
    //not sure what to add here yet aside from sql initalization

    //FIXME: data integrity.
    public class DatabaseHelper
    {
        private readonly string _serverConnectionString;
        private readonly string _databaseConnectionString;
        private const string DATABASE_NAME = "nomad";

        public DatabaseHelper()
        {
            //FIXME: This should be moved to a config file. learn more about env hahahha.
            //maybe use in SSIS too; to be notified later on.
            _serverConnectionString = "Server=localhost;Uid=root;Pwd=root;";
            _databaseConnectionString = $"{_serverConnectionString}Database={DATABASE_NAME};";
        }

        public MySqlConnection GetConnection()
        {
            try
            {
                var connection = new MySqlConnection(_databaseConnectionString);
                return connection;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to connect to database.", ex);
            }
        }

        public void InitializeDatabase()
        {
            //called from App.xaml.cs. will start everytime.
            CreateDatabase();
            CreateTables();
        }

        private void CreateDatabase()
        {
            using (var connection = new MySqlConnection(_serverConnectionString))
            {
                try
                {
                    connection.Open();

                    // check if database exists
                    string checkDatabase = $"SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA WHERE SCHEMA_NAME = '{DATABASE_NAME}'";
                    using (var checkCommand = new MySqlCommand(checkDatabase, connection))
                    {
                        var result = checkCommand.ExecuteScalar();
                        if (result != null) return; // database already exists
                    }

                    // create database if it doesnt exist
                    string createDatabase = $"CREATE DATABASE {DATABASE_NAME}";
                    using (var command = new MySqlCommand(createDatabase, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create database: {ex.Message}", ex);
                }
            }
        }

        private void CreateTables()
        {
            using (var connection = GetConnection())
            {
                try
                {
                    connection.Open();

                    // create Customer table
                    //prim key is customer_id
                    string createCustomerTable = @"
                        CREATE TABLE IF NOT EXISTS customer (
                            customer_id VARCHAR(9) PRIMARY KEY NOT NULL,
                            name VARCHAR(100) NOT NULL,
                            phone_number VARCHAR(30) NOT NULL,
                            address VARCHAR(200) NOT NULL,
                            government_id_picture VARCHAR(255) NOT NULL,
                            customer_status VARCHAR(30) NOT NULL,
                            registration_date DATE NOT NULL
                        )";

                    // create Bike table
                    //prim key is bike_id
                    //debating whether to use int or decimal for the daily rate; int is more simple.
                    string createBikeTable = @"
                        CREATE TABLE IF NOT EXISTS bike (
                            bike_id VARCHAR(9) PRIMARY KEY NOT NULL,
                            bike_model VARCHAR(100) NOT NULL,
                            bike_type VARCHAR(50) NOT NULL,
                            daily_rate INTEGER(9) NOT NULL,
                            bike_status VARCHAR(30) NOT NULL
                        )";

                    // create Rental table
                    //prim key is rental_id. wanted to use this for the payment and return tables too.
                    //sir said to not use it and just use bike and customer id
                    string createRentalTable = @"
                        CREATE TABLE IF NOT EXISTS rentals (
                            rental_id VARCHAR(9) PRIMARY KEY NOT NULL,
                            customer_id VARCHAR(9) NOT NULL,
                            bike_id VARCHAR(9) NOT NULL,
                            rental_date DATE NOT NULL,
                            rental_status VARCHAR(30) NOT NULL,
                            FOREIGN KEY (customer_id) REFERENCES customer(customer_id),
                            FOREIGN KEY (bike_id) REFERENCES bike(bike_id)
                        )";

                    // create Payment table
                    string createPaymentTable = @"
                        CREATE TABLE IF NOT EXISTS payments (
                            payment_id VARCHAR(9) PRIMARY KEY NOT NULL,
                            customer_id VARCHAR(9) NOT NULL,
                            bike_id VARCHAR(9) NOT NULL,
                            amount_to_pay INT(9) NOT NULL,
                            amount_paid INT(9) NOT NULL,
                            payment_date DATE NOT NULL,
                            payment_status VARCHAR(30) NOT NULL,
                            FOREIGN KEY (customer_id) REFERENCES customer(customer_id),
                            FOREIGN KEY (bike_id) REFERENCES bike(bike_id)
                        )";

                    // create Return table
                    string createReturnTable = @"
                        CREATE TABLE IF NOT EXISTS returns (
                            return_id VARCHAR(9) PRIMARY KEY NOT NULL,
                            customer_id VARCHAR(9) NOT NULL,
                            bike_id VARCHAR(9) NOT NULL,
                            return_date DATE NOT NULL,
                            FOREIGN KEY (customer_id) REFERENCES customer(customer_id),
                            FOREIGN KEY (bike_id) REFERENCES bike(bike_id)
                        )";

                    // execute all create table commands
                    using (var command = new MySqlCommand())
                    {
                        command.Connection = connection;

                        command.CommandText = createCustomerTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createBikeTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createRentalTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createPaymentTable;
                        command.ExecuteNonQuery();

                        command.CommandText = createReturnTable;
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to create tables: {ex.Message}", ex);
                }
            }
        }
    }
}