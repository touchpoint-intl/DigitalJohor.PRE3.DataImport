using DigitalJohor.PRE3.DataImport.ImportServices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer.Sinks.MSSqlServer.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DigitalJohor.PRE3.DataImport
{
    public class Program
    {
        private readonly IKetuaKampungService _ketuaKampungService;

        public Program(IKetuaKampungService ketuaKampungService)
        {
            _ketuaKampungService = ketuaKampungService;
        }

        static void Main(string[] args)
        {

            Console.WriteLine("Getting Connection ...");

            //your connection string 
            string connString = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_MCP") ??
                                   "Server=tcp:digitaljohor.database.windows.net,1433;Initial Catalog=pre3_import;Persist Security Info=False;User ID=bananana;Password=gilamonster11#;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            //create instanace of database connection
            SqlConnection conn = new SqlConnection(connString);

            string query = @"SELECT *
                                     FROM KetuaKampung
                                     WHERE NamaMPKK = 'KG SG PADANG' ;
                                     ";

            //define the SqlCommand object
            SqlCommand cmd = new SqlCommand(query, conn);

            try
            {
                Console.WriteLine("Openning Connection ...");

                //open connection
                conn.Open();

                Console.WriteLine("Connection successful!");

                //execute the SQLCommand
                SqlDataReader dr = cmd.ExecuteReader();
                Console.WriteLine(Environment.NewLine + "Retrieving data from database..." + Environment.NewLine);

                var count = 0;
                if (dr.HasRows)
                {
                    while (dr.Read())
                    {
                        count++;
                        var kKampung = new KetuaKampungDTO();
                        kKampung.NamaMPKK = dr.GetString(0);
                        kKampung.Mukim = dr.GetString(1);
                        kKampung.NamaPengerusi = dr.GetString(2);
                        kKampung.BankId = dr.GetInt64(3);
                        kKampung.NoAcc = dr.GetString(4);
                        kKampung.NoIc = dr.GetString(5);
                        kKampung.NoTelefon = dr.GetString(6);
                        kKampung.Cacatan = dr.GetString(7);
                        kKampung.Daerah = dr.GetString(8);

                        //display retrieved record
                        CreateForm(kKampung);
                    }
                    Console.WriteLine("Retrieved records count: " + count);
                }
                else
                {
                    Console.WriteLine("No data found.");
                }

                //foreach (var kk in kKList)
                //{
                //    Console.WriteLine("-----");
                //    Console.WriteLine(kk.NamaMPKK);
                //    Console.WriteLine(kk.Mukim);
                //    Console.WriteLine(kk.NamaPengerusi);
                //    Console.WriteLine(kk.BankId);
                //    Console.WriteLine(kk.NoAcc);
                //    Console.WriteLine(kk.NoIc);
                //    Console.WriteLine(kk.NoTelefon);
                //    Console.WriteLine(kk.Cacatan);
                //    Console.WriteLine(kk.Daerah);
                //}

                //close data reader
                dr.Close();

                //close connection
                conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }

            Console.Read();
        }

        public async void CreateForm(KetuaKampungDTO dto)
        {
            await _ketuaKampungService.CreateNewForm(dto);
        }
    }
}
