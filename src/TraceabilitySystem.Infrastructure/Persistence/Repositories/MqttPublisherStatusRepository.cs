using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class MqttPublisherStatusRepository : BaseRepository<MqttPublisherStatus>, IMqttPublisherStatusRepository
{
    public MqttPublisherStatusRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<MqttPublisherStatus>> GetByReferenceCodeAsync(
        string referenceCode,
        string referenceType = MqttPublisherReferenceTypes.ProcessLog,
        CancellationToken cancellationToken = default)
    {
        return await _context.MqttPublisherStatuses
            .Where(s => s.ReferenceCode == referenceCode && s.ReferenceType == referenceType)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<MqttPublisherStatus>> GetByReferenceCodesAsync(
        IEnumerable<string> referenceCodes,
        string referenceType = MqttPublisherReferenceTypes.ProcessLog,
        CancellationToken cancellationToken = default)
    {
        var codes = referenceCodes.ToList();
        return await _context.MqttPublisherStatuses
            .Where(s => codes.Contains(s.ReferenceCode) && s.ReferenceType == referenceType)
            .ToListAsync(cancellationToken);
    }

    public async Task SyncDisabledProcessesAsync(
        string referenceCode,
        string referenceType,
        IEnumerable<Process> relevantProcesses,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(referenceCode)) return;

        var existingList = await _context.MqttPublisherStatuses
            .Where(s => s.ReferenceCode == referenceCode && s.ReferenceType == referenceType)
            .ToListAsync(cancellationToken);

        var existingMap = existingList
            .GroupBy(s => s.ProcessCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        bool hasChanges = false;

        foreach (var proc in relevantProcesses)
        {
            var procCode = proc.Code;
            bool isExisting = existingMap.TryGetValue(procCode, out var existingItem);

            if (!proc.IsMqttPublisher)
            {
                // Proses mati (IsMqttPublisher == false)
                if (!isExisting)
                {
                    // Belum ada -> Insert baru
                    _context.MqttPublisherStatuses.Add(new MqttPublisherStatus
                    {
                        ProcessCode = procCode,
                        IsPublisher = false,
                        ReferenceType = referenceType,
                        ReferenceCode = referenceCode,
                        Status = "Disabled",
                        Reason = $"Process '{procCode}' MQTT publisher is disabled by configuration.",
                        Date = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    });
                    hasChanges = true;
                }
                else if (existingItem!.IsPublisher != false || existingItem.Status != "Disabled")
                {
                    // Sudah ada tapi status beda -> Update/Replace
                    existingItem.IsPublisher = false;
                    existingItem.Status = "Disabled";
                    existingItem.Reason = $"Process '{procCode}' MQTT publisher is disabled by configuration.";
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    _context.MqttPublisherStatuses.Update(existingItem);
                    hasChanges = true;
                }
            }
            else
            {
                // Proses hidup (IsMqttPublisher == true)
                // Jika sebelumnya ada di tabel (pernah tercatat mati), maka hapus
                if (isExisting && existingItem != null)
                {
                    _context.MqttPublisherStatuses.Remove(existingItem);
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
