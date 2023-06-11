using DocumentManagementApi.Models;
using System.Data;
using System.Data.SqlClient;

namespace DocumentManagementApi.DAL
{
    public interface IDatabaseHelper
    {
        Task InsertDocumentAsync(DocumentDto document);
        int GetDownloadCountFromDatabase(string documentId);
        void UpdateDownloadCountInDatabase(string documentId);
        Task StoreTokenInDatabaseAsync(string token, string documentId);
        (bool, string) CheckTokenValidity(string token);
    }

    public class DatabaseHelper : IDatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
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
                // Handle the exception
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

                    int downloadCount = outputParameter.Value != DBNull.Value ? (int)outputParameter.Value : 0;
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
