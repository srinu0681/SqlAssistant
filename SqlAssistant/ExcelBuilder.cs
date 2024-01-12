using System.Data;
using ClosedXML.Excel;
using Microsoft.Data.SqlClient;

namespace SqlAssistant;

public class ExcelBuilder
{
    private XLWorkbook workbook;
    private IXLWorksheet workSheet;
    private readonly SqlConnectionFactory _sqlConnectionFactory;

    public ExcelBuilder(SqlConnectionFactory sqlConnectionFactory)
    {
        workbook = new XLWorkbook();
        workSheet = workbook.Worksheets.Add("DatabaseResults");

        // Apply formatting to the title columns
        workSheet.Row(1).Style.Font.Bold = true;
        workSheet.Row(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public string ExportDatabaseResultsToExcelFile(string query)
    {
        var excelPath = Path.Combine(Environment.CurrentDirectory, @"Excel", $"databaseresults_{DateTime.Now.Month}{DateTime.Now.Day}{DateTime.Now.Year}-{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.xlsx");

        RunQueryAndExportToExcel(query);

        SaveDocument(excelPath);

        return excelPath;
    }

    public byte[] ExportDatabaseResultsToExcelBytes(string query)
    {
        var workbookBytes = new byte[0];
        var excelPath = Path.Combine(Environment.CurrentDirectory, @"Excel", $"databaseresults_{DateTime.Now.Month}{DateTime.Now.Day}{DateTime.Now.Year}-{DateTime.Now.Hour}{DateTime.Now.Minute}{DateTime.Now.Second}.xlsx");

        RunQueryAndExportToExcel(query);

        using (var ms = new MemoryStream())
        {
            workbook.SaveAs(ms);
            workbookBytes = ms.ToArray();
        }

        return workbookBytes;
    }

    public void RunQueryAndExportToExcel(string query)
    {
        using (SqlConnection connection = _sqlConnectionFactory.createConnection())
        {
            SqlCommand command = new SqlCommand(query, connection);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            try
            {
                var fields = GetColumns(reader);
                AddRow(fields.ToArray(), true);

                while (reader.Read())
                {
                    var values = GetValues(reader);
                    AddRow(values.ToArray());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                reader.Close();
            }
        }
    }

    private IEnumerable<string> GetColumns(IDataReader reader)
    {
        for (int index = 0; index < reader.FieldCount; ++index)
        {
            var label = reader.GetName(index);

            yield return string.IsNullOrWhiteSpace(label) ? $"#{index + 1}" : label;
        }
    }

    private IEnumerable<string> GetValues(IDataReader reader)
    {
        for (int index = 0; index < reader.FieldCount; ++index)
        {
            yield return reader.GetValue(index)?.ToString() ?? string.Empty;
        }
    }

    public void AddRow(string[] cellData, bool bHeader = false)
    {
        var row = bHeader ? workSheet.FirstRow() : workSheet.Row(workSheet.LastRowUsed().RowNumber() + 1);
        for (int i = 0; i < cellData.Count(); i++)
        {
            row.Cell(i + 1).Value = cellData[i];

            if (bHeader)
            {
                row.Cell(i + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(96, 73, 122);
                row.Cell(i + 1).Style.Font.FontColor = XLColor.FromArgb(255, 255, 255);
            }
        }
    }

    public void SaveDocument(string fileName)
    {
        workbook.SaveAs(fileName);
    }
}
