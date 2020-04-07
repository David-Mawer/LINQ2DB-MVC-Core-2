using LinqToDB.Configuration;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace LINQ2DB_MVC_Core_2.Data
{
    public class Linq2dbSettings : ILinqToDBSettings
    {
        private const string msDefaultProviderName = "SqlServer";
        public readonly IConnectionStringSettings mConnectionStringSettings;
        public Linq2dbSettings(IConfiguration configuration)
        {
            // Figure out the database name from the connection string.
            var sDBConnection = configuration.GetSection("ConnectionStrings").GetSection("DefaultConnection").Value;
            var arrDBConnName = sDBConnection.Split("Database=");
            var sDBName = arrDBConnName.Length > 1 ? arrDBConnName[1] : "MVCLinq2DBTemplate";
            var nEndIndx = sDBName.IndexOf(";");
            if (nEndIndx > 0)
            {
                sDBName = sDBName.Substring(0, nEndIndx);
            }
            var oLinq2dbSettings = configuration.GetSection("Authentication").GetSection("Linq2db");
            var sProviderName = oLinq2dbSettings.GetSection("ProviderName").Value ?? "";
            if (sProviderName.Length == 0)
            {
                sProviderName = msDefaultProviderName;
            }

            mConnectionStringSettings = new ConnectionStringSettings
            {
                Name = sDBName,
                ProviderName = sProviderName,
                ConnectionString = sDBConnection
            };
        }

        public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

        public string DefaultConfiguration => "SqlServer";

        public string DefaultDataProvider => "SqlServer";

        public IEnumerable<IConnectionStringSettings> ConnectionStrings
        {
            get
            {
                yield return mConnectionStringSettings;
            }
        }
    }
}
