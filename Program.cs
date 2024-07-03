namespace DataSync_Demo
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string clientConenctionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Client;Integrated Security=True;";
            string serverConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=Server;Integrated Security=True;";
            string tableName = "Products";
            Synchronizer.Synchronize(tableName, serverConnectionString, clientConenctionString);
        }
    }
}
