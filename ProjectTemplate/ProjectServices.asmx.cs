using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Web.Script.Services;

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

        public class SkipsResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public int SkipsLeft { get; set; }
        }
        [WebMethod(EnableSession = true)]
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

		[WebMethod(EnableSession = true)]
		public int CountComments()
		{
			string connectionString = getConString();
			string query = "SELECT COUNT(*) AS total_rows FROM comments WHERE searchable=1";

			using (MySqlConnection sqlConnection = new MySqlConnection(connectionString))
			{
				MySqlCommand sqlCommand = new MySqlCommand(query, sqlConnection);
				sqlConnection.Open();
				int totalRows = Convert.ToInt32(sqlCommand.ExecuteScalar());
				return totalRows;
			}
		}

		[WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
		public LogonResponse LogOn(string uid, string pass)
		{
			//we return this flag to tell them if they logged in or not
			bool success = false;
			bool isAdmin = false;

			//our connection string comes from our web.config file like we talked about earlier
			string sqlConnectString = getConString();

			//here's our query.  A basic select with nothing fancy.  Note the parameters that begin with @
			//NOTICE: we added admin to what we pull, so that we can store it along with the id in the session
			string sqlSelect = "SELECT accountID, admin FROM accounts WHERE username=@idValue and pass=@passValue and active=1";

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
				isAdmin = Convert.ToBoolean(sqlDt.Rows[0]["admin"]);
				//isAdmin = sqlDt.Rows[0]["admin"] != DBNull.Value && Convert.ToBoolean(sqlDt.Rows[0]["admin"]);

			}
			else
			{
				success = false;
				isAdmin = false;
			}
			//return the result!
			return new LogonResponse { success = success, isAdmin = isAdmin };
		}

		public class LogonResponse
		{
			public bool success { get; set; }
			public bool isAdmin { get; set; }
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

        [WebMethod(EnableSession = true)]
        public string SubmitSuggestion(string suggestion)
        {
            if (Session["accountID"] == null)
            {
                return "{ \"success\": false, \"message\": \"User must be logged in.\"}";
            }
            try
            {
                // Input Validation
                if (string.IsNullOrEmpty(suggestion))
                {
                    return "{ \"success\": false, \"message\": \"Suggestion cannot be empty.\"}";
                }
                if (suggestion.Length > 500) // Example length limit
                {
                    return "{ \"success\": false, \"message\": \"Suggestion is too long.\"}";
                }

                // Insert into database
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    connection.Open();
                    string query = "INSERT INTO comments (accountID, content) VALUES (@accountID, @suggestion)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@accountID", Session["accountID"]);
                        command.Parameters.AddWithValue("@suggestion", suggestion);
                        command.ExecuteNonQuery();
                    }
                }
                return "{ \"success\": true }"; // Consistent JSON response
            }
            catch (Exception e)
            {
                return "{ \"success\": false, \"message\": \"An unexpected error occurred. Please try again later.\" }";
            }
        }

        [WebMethod(EnableSession = true)]
        public string SubmitReply(string commentContent, string replyContent)
        {
            try
            {
                if (string.IsNullOrEmpty(replyContent))
                {
                    return "{ \"success\": false, \"message\": \"Reply cannot be empty.\"}";
                }

                string connectionString = getConString();

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();

                    // Get the commentID based on the comment content
                    string queryGetCommentID = "SELECT commentID FROM comments WHERE content = @commentContent LIMIT 1";
                    int commentID = 0;

                    using (MySqlCommand command = new MySqlCommand(queryGetCommentID, connection))
                    {
                        command.Parameters.AddWithValue("@commentContent", commentContent);
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            commentID = Convert.ToInt32(result);
                        }
                        else
                        {
                            return "{ \"success\": false, \"message\": \"Parent comment not found.\"}";
                        }
                    }

                    // Insert the reply anonymously
                    string queryInsertReply = "INSERT INTO replies (commentID, accountID, content) VALUES (@commentID, @accountID, @replyContent)";

                    using (MySqlCommand command = new MySqlCommand(queryInsertReply, connection))
                    {
                        command.Parameters.AddWithValue("@commentID", commentID);
                        command.Parameters.AddWithValue("@accountID", Session["accountID"]);
                        command.Parameters.AddWithValue("@replyContent", replyContent);
                        command.ExecuteNonQuery();
                    }
                }

                return "{ \"success\": true }";
            }
            catch (Exception e)
            {
                return "{ \"success\": false, \"message\": \"An unexpected error occurred. Please try again later.\" }";
            }
        }

        [WebMethod(EnableSession = true)]
		public Comment[] GetActiveComments()
		{
			//check out the return type.  It's an array of Comment objects.  You can look at our custom Comment class to see that it's 
			//just a container for public class-level variables.  It's a simple container that asp.net will have no trouble converting into json.  When we return
			//sets of information, it's a good idea to create a custom container class to represent instances (or rows) of that information, and then return an array of those objects.  
			//Keeps everything simple.

			//WE ONLY SHARE COMMENTS WITH LOGGED IN USERS AND ADMINS!
			if (Session["accountID"] != null && Convert.ToInt32(Session["admin"]) == 1)
			{
				DataTable sqlDt = new DataTable("comments");

				string sqlConnectString = getConString();
				string sqlSelect =
					"SELECT " +
						"c.commentID, " +
						"c.content AS comment_content, " +
						"a1.firstname AS comment_firstname, " +
						"a1.lastname AS comment_lastname, " +
						"r.replyID, " +
						"r.content AS reply_content, " +
						"a2.firstname AS reply_firstname, " +
						"a2.lastname AS reply_lastname, " +
						"priority " +
					"FROM " +
						"comments c " +
					"JOIN " +
					"	accounts a1 ON c.accountID = a1.accountID " +
					"LEFT JOIN " +
					"	replies r ON c.commentID = r.commentID " +
					"LEFT JOIN " +
					"	accounts a2 ON r.accountID = a2.accountID " +
					"WHERE " +
						"c.searchable = 1 " + // important part: distinquishes between active and archived
					"ORDER BY " +
						"priority DESC, " +
						"c.commentID, " +
						"r.replyID";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				//gonna use this to fill a data table
				MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

				//filling the data table
				sqlDa.Fill(sqlDt);

				//loop through each row in the datasets, creating instances
				//of our container classes: Accounts, Comments, Replies.  Fill each with
				//data from the rows, then dump them in a list.
				List<Comment> comments = new List<Comment>();
				for (int i = 0; i < sqlDt.Rows.Count; i++)
				{
					List<Reply> replies = new List<Reply>();
					comments.Add(new Comment
					{
						commentID = sqlDt.Rows[i]["commentID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["commentID"]) : 0,
						comment_content = sqlDt.Rows[i]["comment_content"] != DBNull.Value ? sqlDt.Rows[i]["comment_content"].ToString() : string.Empty,
						comment_firstname = sqlDt.Rows[i]["comment_firstname"] != DBNull.Value ? sqlDt.Rows[i]["comment_firstname"].ToString() : string.Empty,
						comment_lastname = sqlDt.Rows[i]["comment_lastname"] != DBNull.Value ? sqlDt.Rows[i]["comment_lastname"].ToString() : string.Empty,
						replyID = sqlDt.Rows[i]["replyID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["replyID"]) : 0,
						reply_content = sqlDt.Rows[i]["reply_content"] != DBNull.Value ? sqlDt.Rows[i]["reply_content"].ToString() : string.Empty,
						reply_firstname = sqlDt.Rows[i]["reply_firstname"] != DBNull.Value ? sqlDt.Rows[i]["reply_firstname"].ToString() : string.Empty,
						reply_lastname = sqlDt.Rows[i]["reply_lastname"] != DBNull.Value ? sqlDt.Rows[i]["reply_lastname"].ToString() : string.Empty,
						priority = sqlDt.Rows[i]["priority"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["priority"]) : 0
					});
				}
				//convert the list of comments to an array and return!
				return comments.ToArray();
			}
			else
			{
				//if they're not logged in or not an admin, return an empty array
				return new Comment[0];
			}
		}

		[WebMethod(EnableSession = true)]
		public Comment[] GetArchivedComments()
		{
			//check out the return type.  It's an array of Comment objects.  You can look at our custom Comment class to see that it's 
			//just a container for public class-level variables.  It's a simple container that asp.net will have no trouble converting into json.  When we return
			//sets of information, it's a good idea to create a custom container class to represent instances (or rows) of that information, and then return an array of those objects.  
			//Keeps everything simple.

			//WE ONLY SHARE COMMENTS WITH LOGGED IN USERS AND ADMINS!
			if (Session["accountID"] != null && Convert.ToInt32(Session["admin"]) == 1)
			{
				DataTable sqlDt = new DataTable("comments");

				string sqlConnectString = getConString();
				string sqlSelect =
					"SELECT " +
						"c.commentID, " +
						"c.content AS comment_content, " +
						"a1.firstname AS comment_firstname, " +
						"a1.lastname AS comment_lastname, " +
						"r.replyID, " +
						"r.content AS reply_content, " +
						"a2.firstname AS reply_firstname, " +
						"a2.lastname AS reply_lastname, " +
						"priority " +
					"FROM " +
						"comments c " +
					"JOIN " +
					"	accounts a1 ON c.accountID = a1.accountID " +
					"LEFT JOIN " +
					"	replies r ON c.commentID = r.commentID " +
					"LEFT JOIN " +
					"	accounts a2 ON r.accountID = a2.accountID " +
					"WHERE " +
						"c.searchable = 0 " + // important part: distinquishes between active and archived
					"ORDER BY " +
						"priority DESC, " +
						"c.commentID, " +
						"r.replyID";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				//gonna use this to fill a data table
				MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

				//filling the data table
				sqlDa.Fill(sqlDt);

				//loop through each row in the datasets, creating instances
				//of our container classes: Accounts, Comments, Replies.  Fill each with
				//data from the rows, then dump them in a list.
				List<Comment> comments = new List<Comment>();
				for (int i = 0; i < sqlDt.Rows.Count; i++)
				{
					List<Reply> replies = new List<Reply>();
					comments.Add(new Comment
					{
						commentID = sqlDt.Rows[i]["commentID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["commentID"]) : 0,
						comment_content = sqlDt.Rows[i]["comment_content"] != DBNull.Value ? sqlDt.Rows[i]["comment_content"].ToString() : string.Empty,
						comment_firstname = sqlDt.Rows[i]["comment_firstname"] != DBNull.Value ? sqlDt.Rows[i]["comment_firstname"].ToString() : string.Empty,
						comment_lastname = sqlDt.Rows[i]["comment_lastname"] != DBNull.Value ? sqlDt.Rows[i]["comment_lastname"].ToString() : string.Empty,
						replyID = sqlDt.Rows[i]["replyID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["replyID"]) : 0,
						reply_content = sqlDt.Rows[i]["reply_content"] != DBNull.Value ? sqlDt.Rows[i]["reply_content"].ToString() : string.Empty,
						reply_firstname = sqlDt.Rows[i]["reply_firstname"] != DBNull.Value ? sqlDt.Rows[i]["reply_firstname"].ToString() : string.Empty,
						reply_lastname = sqlDt.Rows[i]["reply_lastname"] != DBNull.Value ? sqlDt.Rows[i]["reply_lastname"].ToString() : string.Empty,
						priority = sqlDt.Rows[i]["priority"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["priority"]) : 0
					});
				}
				//convert the list of comments to an array and return!
				return comments.ToArray();
			}
			else
			{
				//if they're not logged in or not an admin, return an empty array
				return new Comment[0];
			}
		}

		[WebMethod(EnableSession = true)]
		public void ArchiveComment(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE comments SET searchable = 0 WHERE commentID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}

		}

		[WebMethod(EnableSession = true)]
		public void UnArchiveComment(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE comments SET searchable = 1 WHERE commentID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public void DeleteComment(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "DELETE from comments WHERE commentID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public Account[] GetUsers()
		{
			//WE ONLY SHARE USER INFO WITH LOGGED IN ADMINS!
			if (Session["accountID"] != null && Convert.ToInt32(Session["admin"]) == 1)
			{
				DataTable sqlDt = new DataTable("accounts");

				string sqlConnectString = getConString();
				string sqlSelect =
					"SELECT " +
						"accountID, " +
						"firstname, " +
						"lastname, " +
						"admin, " +
						"active " +
					"FROM " +
						"accounts " +
					"ORDER BY " +
						"admin DESC, " +
						"lastname, " +
						"firstname";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				//gonna use this to fill a data table
				MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

				//filling the data table
				sqlDa.Fill(sqlDt);

				//loop through each row in the datasets, creating instances
				//of our container classes: Accounts, Comments, Replies.  Fill each with
				//data from the rows, then dump them in a list.
				List<Account> accounts = new List<Account>();
				for (int i = 0; i < sqlDt.Rows.Count; i++)
				{
					accounts.Add(new Account
					{
						accountID = sqlDt.Rows[i]["accountID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["accountID"]) : 0,
						firstname = sqlDt.Rows[i]["firstname"] != DBNull.Value ? sqlDt.Rows[i]["firstname"].ToString() : string.Empty,
						lastname = sqlDt.Rows[i]["lastname"] != DBNull.Value ? sqlDt.Rows[i]["lastname"].ToString() : string.Empty,
						admin = sqlDt.Rows[i]["admin"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["admin"]) : 0,
						active = sqlDt.Rows[i]["active"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["active"]) : 0
					});
				}
				//convert the list of accounts to an array and return!
				return accounts.ToArray();
			}
			else
			{
				//if they're not logged in or not an admin, return an empty array
				return new Account[0];
			}
		}

		[WebMethod(EnableSession = true)]
		public void DeactivateAccount(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE accounts SET active = 0 WHERE accountID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public void ActivateAccount(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE accounts SET active = 1 WHERE accountID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public void DemoteAdmin(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE accounts SET admin = 0 WHERE accountID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public void PromoteAdmin(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE accounts SET admin = 1 WHERE accountID=@idValue";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(id));

				sqlConnection.Open();
				try
				{
					sqlCommand.ExecuteNonQuery();
				}
				catch (Exception e)
				{
				}
				sqlConnection.Close();
			}
		}

		[WebMethod(EnableSession = true)]
		public void SetHighPriorityComment(string id)
        {

        }

		[WebMethod(EnableSession = true)]
		public void SetLowPriorityComment(string id)
        {

        }
	}
}
