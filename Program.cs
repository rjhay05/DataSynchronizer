namespace DataSync_Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string clientConenctionString = @"Data Source=ORTTXN0675\SQLEXPRESS02;Initial Catalog=CTM_Client1_Db;Integrated Security=True;";
            string serverConnectionString = @"Data Source=ORTTXN0675\SQLEXPRESS02;Initial Catalog=CTM_Server_Db;Integrated Security=True;";
            string tableName = "Products";
            Synchronizer.Synchronize(tableName, serverConnectionString, clientConenctionString);
        }
    }
}
