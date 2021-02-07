using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace WeiCloudStorageAPI.Data
{
    public class SqlDataUtil
    {
        //protected readonly MySqlConnection dbConn;
        protected readonly string connStr;
        public SqlDataUtil(string connStr)
        {
            this.connStr = connStr;
            //try
            //{
            //    dbConn = new MySqlConnection();
            //    dbConn.ConnectionString = connStr;
            //    if (dbConn.State != ConnectionState.Open)
            //        dbConn.Open();
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}
        }
        public async Task<T> QueryFirstAsync<T>(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return await dbConn.QueryFirstAsync<T>(sql, param);
            }
        }
        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return await dbConn.QueryAsync<T>(sql, param);
            }
        }
        public IEnumerable<T> Query<T>(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return dbConn.Query<T>(sql, param);
            }
        }
        public async Task<int> ExecuteAsync(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return await dbConn.ExecuteAsync(sql, param);
            }
        }
        public int Execute(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return dbConn.Execute(sql, param);
            }
        }
        public T QueryScalar<T>(string sql, object param = null)
        {
            using (var dbConn = new MySqlConnection())
            {
                dbConn.ConnectionString = connStr;
                if (dbConn.State != ConnectionState.Open)
                    dbConn.Open();
                return dbConn.ExecuteScalar<T>(sql, param);
            }
        }
    }
    public class DBContextAir : SqlDataUtil
    {
        public DBContextAir(IConfiguration _configuration):base(_configuration["ConnectionStrings:WeiCloudAirDBConn"])
        {

        }
    }
    public class DBContext : SqlDataUtil
    {
        public DBContext(IConfiguration _configuration) : base(_configuration["ConnectionStrings:WeiCloudDBConn"])
        {

        }
    }
}
