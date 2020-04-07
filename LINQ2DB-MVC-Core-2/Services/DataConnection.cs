using LINQ2DB_MVC_Core_2.Data;
using LinqToDB;
using LinqToDB.Identity;

namespace LINQ2DB_MVC_Core_2.Services
{
    public class DataConnection : IdentityDataConnection<AspNetUsers>
{
    public DataConnection() : base()
    {
    }

    public DataConnection(string configurationString) : base(configurationString)
    {
    }

    /// <summary>
    /// Allows for the creating of database tables if they don't exist.
    /// </summary>
    /// <typeparam name="T">The class that has the table's fields as public properties</typeparam>
    public void TryCreateTable<T>()
        where T : class
    {
        try
        {
            this.CreateTable<T>();
        }
        catch
        {
            //
        }
    }
}
}
