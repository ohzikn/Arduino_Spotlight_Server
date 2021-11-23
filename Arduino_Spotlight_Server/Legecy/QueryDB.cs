using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Server
{
    class QueryDB
    {
        SqlConnectionStringBuilder sqlConnStrBuilder;
        SqlConnection sqlConn;
        SqlCommand cmd;
        public void Connect(string str)
        {
            /*sqlConnStrBuilder = new SqlConnectionStringBuilder();
            sqlConnStrBuilder.DataSource = "local";
            sqlConnStrBuilder.InitialCatalog = db;
            sqlConnStrBuilder.IntegratedSecurity = TF;*/
            sqlConn = new SqlConnection(str);
        }
        public void Command(string sqlCommand)
        {
            sqlConn.Open();
            cmd = new SqlCommand(sqlCommand, sqlConn);
        }
        public void AddParameter(string[] parameter, int[] data)
        {
            for (int i = 0; i < parameter.Length; i++)
            {
                cmd.Parameters.Add(parameter[i], SqlDbType.Int).Value = data[i];
            }
        }
        public List<int> Get_RGB()
        {
            List<int> rgb = new List<int>();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                rgb.Add((int)dr.GetValue(1));
                rgb.Add((int)dr.GetValue(2));
                rgb.Add((int)dr.GetValue(3));
            }
            sqlConn.Close();
            return rgb;
        }
        public void End()
        {
            sqlConn.Dispose();
        }
    }
}
