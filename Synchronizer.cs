using Microsoft.Synchronization;
using Microsoft.Synchronization.Data;
using Microsoft.Synchronization.Data.SqlServer;
using System;
using System.Data.SqlClient;

namespace DataSync_Demo
{
    public static class Synchronizer
    {
        private static void Initialize(string table, string serverConnectionString,
            string clientConnectionString)
        {
            //Server provisioning
            var serverConnection = new SqlConnection(serverConnectionString);
            DbSyncScopeDescription serverScope = new DbSyncScopeDescription(table);
            DbSyncTableDescription serverTableDescription = SqlSyncDescriptionBuilder.GetDescriptionForTable(table, serverConnection);
            serverScope.Tables.Add(serverTableDescription);
            SqlSyncScopeProvisioning serverProvsioning = new SqlSyncScopeProvisioning(serverConnection, serverScope);

            serverScope.Tables[table].Columns.Remove(serverScope.Tables[table].Columns["SyncID"]);

            serverScope.Tables[table].Columns["Code"].IsPrimaryKey = true;


            if (!serverProvsioning.ScopeExists(table))
            {
                serverProvsioning.SetCreateTableDefault(DbSyncCreationOption.Skip);
                serverProvsioning.Apply();
            }


            //Client provisioning
            var clientConnection = new SqlConnection(clientConnectionString);
            DbSyncScopeDescription clientScope = new DbSyncScopeDescription(table);
            DbSyncTableDescription clientTableDescription = SqlSyncDescriptionBuilder.GetDescriptionForTable(table, clientConnection);
            clientScope.Tables.Add(clientTableDescription);
            SqlSyncScopeProvisioning clientProvsioning = new SqlSyncScopeProvisioning(clientConnection, clientScope);
            if (!clientProvsioning.ScopeExists(table))
            {
                clientProvsioning.SetCreateTableDefault(DbSyncCreationOption.Skip);
                clientProvsioning.Apply();
            }

        }

        public static void Synchronize(string table, string serverConnectionString,
            string clientConnectionString)
        {
            Initialize(table, serverConnectionString, clientConnectionString);
            Synchronize(table, serverConnectionString, clientConnectionString, SyncDirectionOrder.Upload);
            CleanUp(table, serverConnectionString, clientConnectionString);
        }

        private static void Synchronize(string scopeName, string serverConnectionString,
            string clientConnectionString, SyncDirectionOrder syncDirectionOrder)
        {
            var serverConnection = new SqlConnection(serverConnectionString);

            var clientConnection = new SqlConnection(clientConnectionString);

            var agent = new SyncOrchestrator()
            {
                LocalProvider = new SqlSyncProvider(scopeName, clientConnection),
                RemoteProvider = new SqlSyncProvider(scopeName, serverConnection),
                Direction = syncDirectionOrder
            };

            (agent.RemoteProvider as RelationalSyncProvider).SyncProgress
                += new EventHandler<DbSyncProgressEventArgs>(dbProvider_SyncProgress);
            (agent.LocalProvider as RelationalSyncProvider).ApplyChangeFailed
                += new EventHandler<DbApplyChangeFailedEventArgs>(dbProvider_SyncProcessFailed);
            (agent.RemoteProvider as RelationalSyncProvider).ApplyChangeFailed
                += new EventHandler<DbApplyChangeFailedEventArgs>(dbProvider_SyncProcessFailed);

            agent.Synchronize();
        }

        private static void CleanUp(string scopeName, string serverConnectionString,
            string clientConnectionString)
        {
            var serverConnection = new SqlConnection(serverConnectionString);

            var clientConnection = new SqlConnection(clientConnectionString);

            SqlSyncScopeDeprovisioning serverDeprovisioning =
                new SqlSyncScopeDeprovisioning(serverConnection);
            SqlSyncScopeDeprovisioning clientDeprovisioning =
                new SqlSyncScopeDeprovisioning(clientConnection);

            serverDeprovisioning.DeprovisionScope(scopeName);
            serverDeprovisioning.DeprovisionStore();
            clientDeprovisioning.DeprovisionScope(scopeName);
            clientDeprovisioning.DeprovisionStore();
        }

        private static void dbProvider_SyncProgress(object sender, DbSyncProgressEventArgs e)
        {
            Console.WriteLine(e.ScopeProgress.ToString());
            
        }

        private static void dbProvider_SyncProcessFailed(object sneder, DbApplyChangeFailedEventArgs e)
        {
            Console.WriteLine(e.Conflict.ErrorMessage);
        }


    }
}
