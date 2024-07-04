﻿using Microsoft.Synchronization;
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
            using (SqlConnection serverConnection = new SqlConnection(serverConnectionString))
            {
                using (SqlConnection clientConnection = new SqlConnection(clientConnectionString))
                {
                    DbSyncScopeDescription scopeDescription = new DbSyncScopeDescription(table);
                    DbSyncTableDescription tableDescription = SqlSyncDescriptionBuilder.GetDescriptionForTable(table, serverConnection);
                    scopeDescription.Tables.Add(tableDescription);
                    scopeDescription.Tables["Products"].Columns.Remove(scopeDescription.Tables["Products"].Columns["SyncID"]);
                    scopeDescription.Tables["Products"].Columns["ID"].IsPrimaryKey = true;
                    SqlSyncScopeProvisioning serverProvsion = new SqlSyncScopeProvisioning(serverConnection, scopeDescription);
                    serverProvsion.Apply();
                    SqlSyncScopeProvisioning clientProvsion = new SqlSyncScopeProvisioning(clientConnection, scopeDescription);
                    clientProvsion.Apply();
                }
            }
        }

        public static void Synchronize(string table, string serverConnectionString,
            string clientConnectionString)
        {
            Initialize(table, serverConnectionString, clientConnectionString);
            Synchronize(table, serverConnectionString, clientConnectionString, SyncDirectionOrder.DownloadAndUpload);
            CleanUp(table, serverConnectionString, clientConnectionString);


        }

        private static void Synchronize(string scopeName, string serverConnectionString,
            string clientConnectionString, SyncDirectionOrder syncDirectionOrder)
        {
            using (SqlConnection serverConnection = new SqlConnection(serverConnectionString))
            {
                using (SqlConnection clientConnection = new SqlConnection(clientConnectionString))
                {
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
            }
        }

        private static void CleanUp(string scopeName, string serverConnectionString,
            string clientConnectionString)
        {
            using (SqlConnection serverConnection = new SqlConnection(serverConnectionString))
            {
                using (SqlConnection clientConnection = new SqlConnection(clientConnectionString))
                {
                    SqlSyncScopeDeprovisioning serverDeprovisioning =
                        new SqlSyncScopeDeprovisioning(serverConnection);
                    SqlSyncScopeDeprovisioning clientDeprovisioning =
                        new SqlSyncScopeDeprovisioning(clientConnection);

                    serverDeprovisioning.DeprovisionScope(scopeName);
                    serverDeprovisioning.DeprovisionStore();
                    clientDeprovisioning.DeprovisionScope(scopeName);
                    clientDeprovisioning.DeprovisionStore();

                }
            }

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
