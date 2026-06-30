using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class SerialNumberRepository : BaseRepository<SerialNumber>, ISerialNumberRepository
{
    public SerialNumberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SerialNumber?> GetWithRelatedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(sn => sn.Issues!)
                .ThenInclude(sni => sni.Issue!)
                    .ThenInclude(i => i.StockIn!)
                        .ThenInclude(si => si.Part!)
            .FirstOrDefaultAsync(sn => sn.Id == id, cancellationToken);
    }

    public async Task CreateWithIssuesAsync(
        IEnumerable<SerialNumber> serialNumbers,
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default)
    {
        var logPath = @"d:\Kerjaan\project-toho\PT Tokyo Radiator Selamat Sempurna (TRSS)\Traceability System\program\trss-traceability-backend\debug_issues.log";
        var logLines = new List<string>
        {
            $"--- Executing CreateWithIssuesAsync at {DateTime.Now} ---",
            $"Input Serial Numbers: {string.Join(", ", serialNumbers.Select(sn => sn.SerialNumberCode))}",
            $"Input Issue Numbers: {string.Join(", ", issueNumbers ?? new List<string>())}"
        };

        var connection = _context.Database.GetDbConnection();
        var wasClosed = connection.State == ConnectionState.Closed;
        if (wasClosed)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var hasActiveTransaction = _context.Database.CurrentTransaction != null;
        var transaction = hasActiveTransaction 
            ? null 
            : await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dbTransaction = hasActiveTransaction 
                ? _context.Database.CurrentTransaction!.GetDbTransaction() 
                : transaction!.GetDbTransaction();

            // 1. Insert Serial Numbers and retrieve their auto-incremented IDs
            foreach (var sn in serialNumbers)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = dbTransaction;
                cmd.CommandText = @"
                    INSERT INTO serial_numbers (serial_number_code, type, created_at, created_by)
                    VALUES (@serial_number_code, @type, @created_at, @created_by);
                    SELECT LAST_INSERT_ID();";

                var pCode = cmd.CreateParameter();
                pCode.ParameterName = "@serial_number_code";
                pCode.Value = sn.SerialNumberCode;
                cmd.Parameters.Add(pCode);

                var pType = cmd.CreateParameter();
                pType.ParameterName = "@type";
                pType.Value = sn.Type;
                cmd.Parameters.Add(pType);

                var pCreatedAt = cmd.CreateParameter();
                pCreatedAt.ParameterName = "@created_at";
                pCreatedAt.Value = DateTime.UtcNow;
                cmd.Parameters.Add(pCreatedAt);

                var pCreatedBy = cmd.CreateParameter();
                pCreatedBy.ParameterName = "@created_by";
                pCreatedBy.Value = (object?)sn.CreatedBy ?? DBNull.Value;
                cmd.Parameters.Add(pCreatedBy);

                var insertedIdObj = await cmd.ExecuteScalarAsync(cancellationToken);
                sn.Id = Convert.ToInt32(insertedIdObj);
                sn.CreatedAt = (DateTime)pCreatedAt.Value;
                
                logLines.Add($"Inserted Serial Number: {sn.SerialNumberCode} with generated ID: {sn.Id}");
            }

            // 2. Fetch Issue IDs by issue numbers
            var issueIds = new List<int>();
            var issueList = issueNumbers?.ToList() ?? new List<string>();
            logLines.Add($"Issue count to search: {issueList.Count}");
            if (issueList.Count > 0)
            {
                using var cmd = connection.CreateCommand();
                cmd.Transaction = dbTransaction;

                var paramNames = new List<string>();
                for (int i = 0; i < issueList.Count; i++)
                {
                    var paramName = $"@issueNo{i}";
                    paramNames.Add(paramName);

                    var param = cmd.CreateParameter();
                    param.ParameterName = paramName;
                    param.Value = issueList[i];
                    cmd.Parameters.Add(param);
                    
                    logLines.Add($"Added issue param: {paramName} = '{issueList[i]}'");
                }

                cmd.CommandText = $"SELECT id FROM issues WHERE number IN ({string.Join(", ", paramNames)})";
                logLines.Add($"Executing query: {cmd.CommandText}");

                using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var fetchedId = reader.GetInt32(0);
                    issueIds.Add(fetchedId);
                    logLines.Add($"Found Issue ID in DB: {fetchedId}");
                }
            }

            logLines.Add($"Resolved Issue IDs count: {issueIds.Count}");

            // 3. Insert Serial Number Issues
            if (issueIds.Count > 0)
            {
                foreach (var sn in serialNumbers)
                {
                    foreach (var issueId in issueIds)
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.Transaction = dbTransaction;
                        cmd.CommandText = @"
                            INSERT INTO serial_number_issues (serial_number_id, issue_id, created_at, created_by)
                            VALUES (@serial_number_id, @issue_id, @created_at, @created_by);";

                        var pSnId = cmd.CreateParameter();
                        pSnId.ParameterName = "@serial_number_id";
                        pSnId.Value = sn.Id;
                        cmd.Parameters.Add(pSnId);

                        var pIssueId = cmd.CreateParameter();
                        pIssueId.ParameterName = "@issue_id";
                        pIssueId.Value = issueId;
                        cmd.Parameters.Add(pIssueId);

                        var pCreatedAt = cmd.CreateParameter();
                        pCreatedAt.ParameterName = "@created_at";
                        pCreatedAt.Value = DateTime.UtcNow;
                        cmd.Parameters.Add(pCreatedAt);

                        var pCreatedBy = cmd.CreateParameter();
                        pCreatedBy.ParameterName = "@created_by";
                        pCreatedBy.Value = (object?)sn.CreatedBy ?? DBNull.Value;
                        cmd.Parameters.Add(pCreatedBy);

                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                        logLines.Add($"Linked Serial Number ID: {sn.Id} with Issue ID: {issueId}");
                    }
                }
            }

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
                logLines.Add("Transaction committed.");
            }
        }
        catch (Exception ex)
        {
            logLines.Add($"Error: {ex.Message}");
            logLines.Add($"Stack Trace: {ex.StackTrace}");
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
                logLines.Add("Transaction rolled back.");
            }
            throw;
        }
        finally
        {
            if (wasClosed)
            {
                await connection.CloseAsync();
                logLines.Add("Connection closed.");
            }
            try
            {
                System.IO.File.AppendAllLines(logPath, logLines);
            }
            catch
            {
                // Ignore log file write errors
            }
        }
    }
}
