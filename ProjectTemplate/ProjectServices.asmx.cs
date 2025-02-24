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
using System.Xml.Linq;


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

		[WebMethod(EnableSession = true)]
		public bool IsAdmin()
		{
			if (Convert.ToInt32(Session["admin"]) == 1) return true;
			else return false; 
		}

		[WebMethod(EnableSession = true)] //NOTICE: gotta enable session on each individual method
		public bool LogOn(string uid, string pass)
		{
			bool success = false;

			string sqlConnectString = getConString();

			string sqlSelect = "SELECT accountID, admin FROM accounts WHERE username=@idValue and pass=@passValue and active=1";

			MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
			MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

			sqlCommand.Parameters.AddWithValue("@idValue", HttpUtility.UrlDecode(uid));
			sqlCommand.Parameters.AddWithValue("@passValue", HttpUtility.UrlDecode(pass));

			MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);
			DataTable sqlDt = new DataTable();
			sqlDa.Fill(sqlDt);

			if (sqlDt.Rows.Count > 0)
			{
				Session["accountID"] = sqlDt.Rows[0]["accountID"];
				Session["admin"] = sqlDt.Rows[0]["admin"];
				success = true;
			}
			return success;
		}

		[WebMethod(EnableSession = true)]
		public bool LogOff()
		{
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
        public string SubmitImportance(string commentContent, object important)
        {
            Console.WriteLine("Raw received important value in C#: " + important);
            int importantValue = Convert.ToInt32(important);
            Console.WriteLine("Converted important value: " + importantValue);

            try
            {
                Console.WriteLine("Received important value in C#: " + important);
                Console.WriteLine("Received comment content: " + commentContent);

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
                            Console.WriteLine("Parent comment not found.");
                            return "{ \"success\": false, \"message\": \"Parent comment not found.\"}";
                        }
                    }

                    // Log the retrieved commentID
                    Console.WriteLine("Retrieved commentID: " + commentID);

                    // Get the latest reply ID for this comment
                    string queryGetLatestReply = "SELECT replyID FROM replies WHERE commentID = @commentID ORDER BY replyID DESC LIMIT 1";
                    int latestReplyID = 0;

                    using (MySqlCommand command = new MySqlCommand(queryGetLatestReply, connection))
                    {
                        command.Parameters.AddWithValue("@commentID", commentID);
                        object result = command.ExecuteScalar();
                        if (result != null)
                        {
                            latestReplyID = Convert.ToInt32(result);
                        }
                    }

                    Console.WriteLine("Updating latest replyID: " + latestReplyID);

                    // Update the important column in the latest reply
                    string queryUpdateImportance = "UPDATE replies SET important = @important WHERE replyID = @replyID LIMIT 1";

                    using (MySqlCommand command = new MySqlCommand(queryUpdateImportance, connection))
                    {
                        command.Parameters.AddWithValue("@important", importantValue);
                        command.Parameters.AddWithValue("@replyID", latestReplyID);
                        int rowsAffected = command.ExecuteNonQuery();

                        Console.WriteLine("Rows affected: " + rowsAffected);

                        if (rowsAffected == 0)
                        {
                            Console.WriteLine("No rows were updated. Possible missing replyID in replies.");
                            return "{ \"success\": false, \"message\": \"Failed to update importance.\"}";
                        }
                    }
                }

                return "{ \"success\": true }";
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                return "{ \"success\": false, \"message\": \"An unexpected error occurred. Please try again later.\" }";
            }
        }


        [WebMethod(EnableSession = true)]
        public Comment[] GetActiveComments()
        {

            // WE ONLY SHARE COMMENTS WITH LOGGED IN USERS AND ADMINS!
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
                        "priority, " +
                        "totalImportance " + // Ensure this line is not commented out
                    "FROM " +
                        "comments c " +
                    "JOIN " +
                        "accounts a1 ON c.accountID = a1.accountID " +
                    "LEFT JOIN " +
                        "replies r ON c.commentID = r.commentID " +
                    "LEFT JOIN " +
                        "accounts a2 ON r.accountID = a2.accountID " +
                    "WHERE " +
                        "c.searchable = 1 " + // Important part: distinguishes between active and archived
					"ORDER BY " +
                        "priority DESC, " +
                        "totalImportance DESC, " +
                        "c.commentID, " +
                        "r.replyID";

                MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
                MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

                MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

                sqlDa.Fill(sqlDt);

                // Loop through each row in the dataset, creating instances
                // of our container classes: Accounts, Comments, Replies. Fill each with
                // data from the rows, then dump them in a list.
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
						priority = sqlDt.Rows[i]["priority"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["priority"]) : 0,
						totalImportance = sqlDt.Rows[i]["totalImportance"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["totalImportance"]) : 0
                    });
                }
                // Convert the list of comments to an array and return!
                return comments.ToArray();
            }
            else
            {
                // If they're not logged in or not an admin, return an empty array
                return new Comment[0];
            }
        }

        [WebMethod(EnableSession = true)]
		public Comment[] GetArchivedComments()
		{
			// Check out the return type. It's an array of Comment objects. You can look at our custom Comment class to see that it's 
			// just a container for public class-level variables. It's a simple container that asp.net will have no trouble converting into json. When we return
			// sets of information, it's a good idea to create a custom container class to represent instances (or rows) of that information, and then return an array of those objects.  
			// Keeps everything simple.

			// WE ONLY SHARE COMMENTS WITH LOGGED IN USERS AND ADMINS!
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
						"priority, " +
						"totalImportance " + // Ensure this line is included
					"FROM " +
						"comments c " +
					"JOIN " +
						"accounts a1 ON c.accountID = a1.accountID " +
					"LEFT JOIN " +
						"replies r ON c.commentID = r.commentID " +
					"LEFT JOIN " +
						"accounts a2 ON r.accountID = a2.accountID " +
					"WHERE " +
						"c.searchable = 0 " + // Important part: distinguishes between active and archived
					"ORDER BY " +
						"priority DESC, " +
						"totalImportance DESC, " +
						"c.commentID, " +
						"r.replyID";

				MySqlConnection sqlConnection = new MySqlConnection(sqlConnectString);
				MySqlCommand sqlCommand = new MySqlCommand(sqlSelect, sqlConnection);

				// Use this to fill a data table
				MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

				// Filling the data table
				sqlDa.Fill(sqlDt);

				// Loop through each row in the dataset, creating instances
				// of our container classes: Accounts, Comments, Replies. Fill each with
				// data from the rows, then dump them in a list.
				List<Comment> comments = new List<Comment>();
				for (int i = 0; i < sqlDt.Rows.Count; i++)
				{
					List<Reply> replies = new List<Reply>();
					comments.Add(new Comment
					{
						commentID = sqlDt.Rows[i]["commentID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["commentID"]) : 0,
                        comment_content = $"<span class='header'>{(sqlDt.Rows[i]["comment_content"] != DBNull.Value ? sqlDt.Rows[i]["comment_content"].ToString() : string.Empty)}</span>",
                        comment_firstname = sqlDt.Rows[i]["comment_firstname"] != DBNull.Value ? sqlDt.Rows[i]["comment_firstname"].ToString() : string.Empty,
						comment_lastname = sqlDt.Rows[i]["comment_lastname"] != DBNull.Value ? sqlDt.Rows[i]["comment_lastname"].ToString() : string.Empty,
						replyID = sqlDt.Rows[i]["replyID"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["replyID"]) : 0,
						reply_content = sqlDt.Rows[i]["reply_content"] != DBNull.Value ? sqlDt.Rows[i]["reply_content"].ToString() : string.Empty,
						reply_firstname = sqlDt.Rows[i]["reply_firstname"] != DBNull.Value ? sqlDt.Rows[i]["reply_firstname"].ToString() : string.Empty,
						reply_lastname = sqlDt.Rows[i]["reply_lastname"] != DBNull.Value ? sqlDt.Rows[i]["reply_lastname"].ToString() : string.Empty,
						priority = sqlDt.Rows[i]["priority"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["priority"]) : 0,
						totalImportance = sqlDt.Rows[i]["totalImportance"] != DBNull.Value ? Convert.ToInt32(sqlDt.Rows[i]["totalImportance"]) : 0 // Add this line
					});
				}
				// Convert the list of comments to an array and return!
				return comments.ToArray();
			}
			else
			{
				// If they're not logged in or not an admin, return an empty array
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

				MySqlDataAdapter sqlDa = new MySqlDataAdapter(sqlCommand);

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

        //add improvements message
        [WebMethod(EnableSession = true)]
        public string AddImprovement(string content)
        {
            if (Session["accountID"] == null || Convert.ToInt32(Session["admin"]) != 1)
            {
                return "{ \"success\": false, \"message\": \"Only admins can add improvements.\" }";
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    connection.Open();

                    // Check if an improvement already exists
                    string checkQuery = "SELECT COUNT(*) FROM improvements";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            //If it exists, update the existing row
                            string updateQuery = "UPDATE improvements SET content = @content, isDisplayed = TRUE " +
								"WHERE improvementID = (SELECT improvementID FROM (SELECT improvementID FROM improvements ORDER BY improvementID DESC LIMIT 1) AS subquery)";
                            using (MySqlCommand updateCmd = new MySqlCommand(updateQuery, connection))
                            {
                                updateCmd.Parameters.AddWithValue("@content", content);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            //If no row exists, insert a new one
                            string insertQuery = "INSERT INTO improvements (content, isDisplayed) VALUES (@content, TRUE)";
                            using (MySqlCommand insertCmd = new MySqlCommand(insertQuery, connection))
                            {
                                insertCmd.Parameters.AddWithValue("@content", content);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }

                return "{ \"success\": true }";
            }
            catch (Exception e)
            {
                return "{ \"success\": false, \"message\": \"Database error occurred.\" }";
            }
        }


        //toggle improvements display status
        [WebMethod(EnableSession = true)]
        public string ToggleImprovementDisplay(int improvementID, bool isDisplayed)
        {
            if (Session["accountID"] == null || Convert.ToInt32(Session["admin"]) != 1)
            {
                return "{ \"success\": false, \"message\": \"Only admins can update improvements.\" }";
            }

            try
            {
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    connection.Open();
                    string query = "UPDATE improvements SET isDisplayed = @isDisplayed WHERE improvementID = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", improvementID);
                        command.Parameters.AddWithValue("@isDisplayed", isDisplayed);
                        command.ExecuteNonQuery();
                    }
                }
                return "{ \"success\": true }";
            }
            catch (Exception e)
            {
                return "{ \"success\": false, \"message\": \"Error updating display status.\" }";
            }
        }


        // Retrieve all active improvements
        [WebMethod(EnableSession = true)]
        public string GetActiveImprovements()
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(getConString()))
                {
                    connection.Open();
                    string query = "SELECT content FROM improvements WHERE isDisplayed = 1 ORDER BY improvementID DESC LIMIT 1";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        object result = command.ExecuteScalar();

                        if (result != null)
                        {
                            string improvement = result.ToString();
                            return "{ \"success\": true, \"improvements\": [\"" + improvement + "\"] }";
                        }
                        else
                        {
                            return "{ \"success\": false, \"improvements\": [] }";
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "{ \"success\": false, \"message\": \"Error retrieving improvements.\" }";
            }
        }


        [WebMethod(EnableSession = true)]
		public void SetHighPriorityComment(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE comments SET priority = 1 WHERE commentID=@idValue";

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
		public void SetLowPriorityComment(string id)
		{
			if (Convert.ToInt32(Session["admin"]) == 1)
			{
				string sqlConnectString = getConString();
				string sqlSelect = "UPDATE comments SET priority = 0 WHERE commentID=@idValue";

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
	}
}