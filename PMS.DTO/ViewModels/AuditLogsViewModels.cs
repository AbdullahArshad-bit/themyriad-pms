using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.AuditLogsViewModels
{
    //public class AuditLogsViewModels
    //{
    //    public AuditLog AuditLog { get; set; }
    //    public string PersonCode { get; set; }
    //    public string PersonFullName { get; set; }
    //}

    public class AuditLogsViewModels
    {
        public int Id { get; set; }
        public string PersonCode { get; set; }
        public string PersonFullName { get; set; }
        public string ActionName { get; set; }
        public int AuditType { get; set; }
        public string Reference { get; set; }
        public string TableName { get; set; }
        public DateTime TimeStamp { get; set; }
        public string UserName { get; set; }
        public int UserId { get; set; }
        public int ActionId { get; set; }
        public string PK { get; set; }
        public string AuditTypeText
        {
            get
            {
                switch (AuditType)
                {
                    case 0: return "Read";
                    case 1: return "Create";
                    case 2: return "Update";
                    case 3: return "Delete";
                    default: return AuditType.ToString();
                }
            }
        }
        public string FormattedTimeStamp
        {
            get { return TimeStamp.ToString("dd/MM/yyyy HH:mm:ss"); }
        }
    }


    public class AuditLogsBinding
    {
        public string draw { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? Userid { get; set; }
        public int? ActionId { get; set; }
        public int? AuditTypeId { get; set; }
        public Search search { get; set; }
    }

    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }

    public class AuditLogsResponse
    {
        public List<AuditLogsViewModels> AuditLogs { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }

    public class AuditLogDetailVM
    {
        public string PropertyName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}

