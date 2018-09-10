using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            test e = new test();
            e.getData();
        }

    }
    class test
    {
        public void getData()
        {
            string query = @"select concat(p.first_name,p.last_name ) patient_name , s.name as sample_name,ps.item_code,ps.visit_type,ps.sample_id,i.subdepartment_code,(case when p.gender=1 then 'Male' else 'Female' end)as sex ,p.age,p.dob
from lab_patient_sample ps
left join patient p on p.id=ps.patient_id
left join service_item i on i.service_code=ps.item_code
left join sample_master s on s.id = i.sample_type_id
where ps.sample_id is not null and s.name is not null and s.name not like '.'
limit 150
";
            List<LabSampleDTO> list = new List<LabSampleDTO>();
            using (DBCommand cmd = new DBCommand(query))
            {
                DataTable dt = new DBHelper().FillDataTable(cmd);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    list.Add(new LabSampleDTO
                    {
                        PatientName = dt.Rows[i]["patient_name"].ToString(),
                    //    SampleTypeName = dt.Rows[i]["sample_name"].ToString(),
                        ItemCode = dt.Rows[i]["item_code"].ToString(),
                        VisitType = dt.Rows[i]["visit_type"].ToString(),
                        SampleNo = dt.Rows[i]["sample_id"].ToString(),
                        SubDeptCode = dt.Rows[i]["subdepartment_code"].ToString(),
                        SexName = dt.Rows[i]["sex"].ToString(),
                        Age = dt.Rows[i]["age"].ToString(),
                        AgeNow = dt.Rows[i]["dob"].ToString()
                    });
                }
            }
            SaveSampleDataToLabelingMachine(list);
        }

        public bool SaveSampleDataToLabelingMachine(List<LabSampleDTO> selectedSampToPrintBarcode)
        {

            using (SqliteHelper cmd = new SqliteHelper())
                selectedSampToPrintBarcode.ForEach(sample =>
                {
                    string query = @"insert into TestDetails ('PatientName','Bill,Sample,PatientID','SampleType','TestCode','PatientLocationID','Department','Gender','AgeinYears','AgeinText') values 
                    ('" + sample.PatientName + "','" + sample.SampleNo + "','" + sample.SampleTypeName + "','" + sample.ItemCode + "','" + sample.VisitType +
                    "','" + sample.SubDeptCode + "','" + sample.SexName + "','" + sample.AgeNow + "','" + sample.Age + "')";
                    cmd.Insert(query);
                });
            return true;
        }

    }
    class LabSampleDTO
    {
        public string SexName { get; set; }

        public string SampleNo { get;  set; }
        public string PatientName { get; internal set; }
        public string SubDeptCode { get; internal set; }
        public string SampleTypeName { get; internal set; }
        public string AgeNow { get; internal set; }
        public string ItemCode { get; internal set; }
        public string VisitType { get; internal set; }
        public string Age { get; internal set; }
    }

    public class SqliteHelper : IDisposable
    {
        private SQLiteConnection sqlite;

        public SqliteHelper()
        {
            sqlite = new SQLiteConnection(DBSettings.SqliteConnectionString);
        }

        public DataTable selectQuery(string query)
        {
            SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            try
            {
                SQLiteCommand cmd;
                sqlite.Open();  //Initiate connection to the db
                cmd = sqlite.CreateCommand();
                cmd.CommandText = query;  //set the passed query
                ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt); //fill the datasource
            }
            catch (SQLiteException ex)
            {
              //  LoggingUtility.Logger.Error(string.Format("Error from sqllite select query:{0}:{1}", ex.Message, ex.InnerException), ex);
            }
            sqlite.Close();
            return dt;
        }
        public bool Insert(string query)
        {
            bool flag = false;
            try
            {
                SQLiteCommand cmd;
                sqlite.Open();  //Initiate connection to the db
                cmd = sqlite.CreateCommand();
                cmd.CommandText = query;  //set the passed query
                cmd.ExecuteNonQuery();
                flag = true;
            }
            catch (SQLiteException ex)
            {
               // LoggingUtility.Logger.Error(string.Format("Error from sqllite insert query:{0}:{1}", ex.Message, ex.InnerException), ex);
            }
            sqlite.Close();
            return flag;
        }

        public void Dispose()
        {
            sqlite.Dispose();
        }
    }
    public class DBHelper
    {
        //NpgsqlConnection con = new NpgsqlConnection(ConfigurationManager.ConnectionStrings["numrConnection"].ConnectionString);
        //con.Open();
        //        NpgsqlCommand cmd = new NpgsqlCommand(query);
        //cmd.Connection = con;
        //        NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
        //DataTable tb = new DataTable();
        //da.Fill(tb);
        //        //var e = DataRowMapper(tb.Rows[0],typeof( LabSampleDTO),"");
        //        con.Close();
        DbConnection DbCon = new DbConnection();

        public DataTable FillDataTable(DBCommand dbCommand)
        {

            DataTable result = new DataTable();

            NpgsqlCommand cmd = dbCommand.GetInternalCommand();

            cmd.Connection = DbCon.Connection;

            try
            {
                DbCon.Open();
                NpgsqlDataAdapter da = new NpgsqlDataAdapter(cmd);
                da.Fill(result);
                DbCon.Close();

            }

            catch (NpgsqlException ex)
            {
               // LoggingUtility.Logger.Error(string.Format("Error from ExecuteScalar part of trans:{0}:{1}", ex.Message, ex.InnerException), ex);
            }

            finally
            {
                DbCon.Close();
            }

            return result;

        }

    }
    public class DBCommand : IDisposable
    {
        private NpgsqlCommand _InternalCommand;
        public NpgsqlCommand GetInternalCommand()
        {
            _InternalCommand.CommandType = CommandType.Text;
            _InternalCommand.CommandText = SQLQuery;
            return _InternalCommand;
        }

        public DBCommand()
        {
            _InternalCommand = new NpgsqlCommand();
            Parameters.Clear();
        }
        public DBCommand(string sqlQuery)
        {
            _InternalCommand = new NpgsqlCommand();
            this.SQLQuery = sqlQuery;
            Parameters.Clear();

        }
        public string SQLQuery { get; set; }
        public NpgsqlParameterCollection Parameters
        {
            get
            {
                return _InternalCommand.Parameters;
            }
        }

        public void Dispose()
        {
            SQLQuery = "";
            _InternalCommand.Dispose();
        }
    }

    class DbConnection
    {
        private NpgsqlConnection con { get; set; }
        public NpgsqlConnection Connection
        {
            get
            {
                if (con == null)
                {
                    //if (string.IsNullOrWhiteSpace(DBSettings.ConnectionString))
                    //{
                    //  //  MessageBox.Show("error:not establish connection");
                    //    throw new Exception("can't read connection");
                    //}
                    //else
                        con = new NpgsqlConnection(DBSettings.ConnectionString);

                }
                return con;
            }
        }
        public void Open()
        {
            Connection.Open();
        }
        public void Close()
        {
            if (con != null)
            {
                if (con.State == System.Data.ConnectionState.Open)
                {
                    con.Close();
                }
            }
        }
    }
    public static class DBSettings
    {
        public static string ConnectionString { get { return "Server='';User Id='';Password='';Database='';Pooling=true;MaxPoolSize=300;Integrated Security=true"; } }
        public static string SqliteConnectionString { get { return "DataSource = D://sample2.db";} }
    }
}
