﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;

namespace ProjectTemplate
{
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	[System.Web.Script.Services.ScriptService]

	public class ProjectServices : System.Web.Services.WebService
	{
		////////////////////////////////////////////////////////////////////////
		///replace the values of these variables with your database credentials
		////////////////////////////////////////////////////////////////////////
		private string dbid = "cis440springA2025team11";
		private string dbpass = "cis440springA2025team11";
		private string dbname = "cis440springA2025team11";
		////////////////////////////////////////////////////////////////////////

		////////////////////////////////////////////////////////////////////////
		///call this method anywhere that you need the connection string!
		////////////////////////////////////////////////////////////////////////
		private string getConString()
		{
			return "SERVER=107.180.1.16; PORT=3306; DATABASE=" + dbname + "; UID=" + dbid + "; PASSWORD=" + dbpass;
		}
		////////////////////////////////////////////////////////////////////////



		/////////////////////////////////////////////////////////////////////////
		//don't forget to include this decoration above each method that you want
		//to be exposed as a web service!
		//[WebMethod(EnableSession = true)]
		/////////////////////////////////////////////////////////////////////////
		//public string TestConnection()
		//{
		//	try
		//	{
		//		string testQuery = "select * from accounts";

		//		////////////////////////////////////////////////////////////////////////
		//		///here's an example of using the getConString method!
		//		////////////////////////////////////////////////////////////////////////
		//		MySqlConnection con = new MySqlConnection(getConString());
		//		////////////////////////////////////////////////////////////////////////

		//		MySqlCommand cmd = new MySqlCommand(testQuery, con);
		//		MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
		//		DataTable table = new DataTable();
		//		adapter.Fill(table);
		//		return "Success!";
		//	}
		//	catch (Exception e)
		//	{
		//		return "Something went wrong, please check your credentials and db name and try again.  Error: "+e.Message;
		//	}
		//}

		[WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
		public bool LogOn(string uid, string pass)
		{
			//we return this flag to tell them if they logged in or not
			bool success = false;

			//our connection string comes from our web.config file like we talked about earlier
			string sqlConnectString = getConString();

			//here's our query.  A basic select with nothing fancy.  Note the parameters that begin with @
			//NOTICE: we added admin to what we pull, so that we can store it along with the id in the session
			string sqlSelect = "SELECT accountID, admin FROM accounts WHERE username=@idValue and pass=@passValue";

			//set up our connection object to be ready to use our connection string
			MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
			//set up our command object to use our connection, and our query
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			//tell our command to replace the @parameters with real values
			//we decode them because they came to us via the web so they were encoded
			//for transmission (funky characters escaped, mostly)
			sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

			//a data adapter acts like a bridge between our command object and 
			//the data we are trying to get back and put in a table object
			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			//here's the table we want to fill with the results from our query
			DataTable sqlDt = new DataTable();
			//here we go filling it!
			sqlDa.Fill(sqlDt);
			//check to see if any rows were returned.  If they were, it means it's 
			//a legit account
			if (sqlDt.Rows.Count > 0)
			{
				//if we found an account, store the id and admin status in the session
				//so we can check those values later on other method calls to see if they 
				//are 1) logged in at all, and 2) and admin or not
				Session["accountID"] = sqlDt.Rows[0]["accountID"];
				Session["admin"] = sqlDt.Rows[0]["admin"];
				success = true;
			}
			//return the result!
			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool LogOff()
		{
			//if they log off, then we remove the session.  That way, if they access
			//again later they have to log back on in order for their ID to be back
			//in the session!
			Session.Abandon();
			return true;
		}

        [WebMethod]
        public string GetRandomComment()
        {
            string connectionString = getConString();
            string query = "SELECT content FROM comments WHERE searchable = 1 ORDER BY RAND() LIMIT 1"; // MySQL syntax

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                MySqlCommand command = new MySqlCommand(query, connection);
                connection.Open();

                object result = command.ExecuteScalar(); // Use ExecuteScalar for single values
                return result != null ? result.ToString() : "No comments found.";
            }
        }

    }
}
