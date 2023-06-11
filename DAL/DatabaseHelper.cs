using DocumentManagementApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace DocumentManagementApi.DAL
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection"); ;

        }


        public async Task InsertDocumentAsync(DocumentDto document)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (SqlCommand command = new SqlCommand("InsertDocument", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@DocumentId", document.DocumentId);
                        command.Parameters.AddWithValue("@Name", document.Name);
                        command.Parameters.AddWithValue("@Icon", document.Icon);
                        command.Parameters.AddWithValue("@ContentPreviewImage", document.ContentPreviewImage);
                        command.Parameters.AddWithValue("@UploadDateTime", document.UploadDateTime);
                        command.Parameters.AddWithValue("@DownloadCount", document.DownloadCount);

                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {


            }
        }




        public int GetDownloadCountFromDatabase(string documentId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("GetDownloadCount", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@DocumentId", documentId);

                    SqlParameter outputParameter = new SqlParameter("@DownloadCount", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParameter);

                    command.ExecuteNonQuery();

                    int downloadCount;

                    if (outputParameter.Value != DBNull.Value)
                    {
                        downloadCount = (int)outputParameter.Value;
                    }
                    else
                    {
                        downloadCount = 0; // Or assign a default value as per your requirements
                    }
                    return downloadCount;
                }
            }
        }

        public void UpdateDownloadCountInDatabase(string documentId)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("UpdateDownloadCount", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@DocumentId", documentId);

                    command.ExecuteNonQuery();
                }
            }
        }


        public async Task StoreTokenInDatabaseAsync(string token, string documentId)
        {
            // TODO: Implement the database connection and call the stored procedure
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("InsertDocumentToken", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Token", token);
                    command.Parameters.AddWithValue("@DocumentId", documentId);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }


        public (bool, string) CheckTokenValidity(string token)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("CheckTokenValidity", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@Token", token);

                    SqlParameter documentIdParameter = command.Parameters.Add("@DocumentId", SqlDbType.NVarChar, 100);
                    documentIdParameter.Direction = ParameterDirection.Output;

                    command.ExecuteNonQuery();

                    bool isValid = documentIdParameter.Value != DBNull.Value;
                    string documentId = isValid ? documentIdParameter.Value.ToString() : null;

                    return (isValid, documentId);
                }
            }
        }

    }


}
