using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.AuditLogsViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.AuditLogs
{
    public interface IAuditLogsService
    {
        //List<DifferenceInObjects> DetailedCompare<T>(this T val1, T val2);

        void AddAuditLog(EF.AuditLog auditLog);
        void AddAuditLogList(List<EF.AuditLog> auditLog);

        List<EF.AuditLog> GetAuditLogs();

        List<EF.AuditLog> GetAuditHistoryByPersoId(int PersonId);

        AuditLogsResponse GetAuditLogsData(AuditLogsBinding request);

        AuditLogsResponse ExportAuditLogsData(AuditLogsBinding request);
    }
}
