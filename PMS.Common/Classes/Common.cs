using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common.Classes
{
   public static class Common
    {

        public static List<EF.AuditLogDetail> DetailedCompare<T>(this T val1, T val2)
        {
            List<EF.AuditLogDetail> variances = new List<EF.AuditLogDetail>();
            PropertyInfo[] fi = val1.GetType().GetProperties();
            foreach (var f in fi.Where(x => x.PropertyType.Name != typeof(ICollection<>).Name))
            {
                var i = f.GetAccessors()[0].IsVirtual;

                    if (f.Name.ToLower().Equals("updateddate") || i == true)
                    continue;

                EF.AuditLogDetail v = new EF.AuditLogDetail();
                v.PropertyName = f.Name;
                v.OldValue = f.GetValue(val1) == null ? "" : f.GetValue(val1).ToString();
                v.NewValue = f.GetValue(val2) == null ? "" : f.GetValue(val2).ToString();
                if (!Equals(v.OldValue, v.NewValue))
                    variances.Add(v);

            }
            return variances;
        }





        public class DifferenceInObjects
        {
            public string Property { get; set; }
            public object OldValue { get; set; }
            public object NewValue { get; set; }
        }



        public static string GetChangedValues(object oldObject, object newObject)
        {
            var oType = oldObject.GetType();

            var sb = new StringBuilder();
            foreach (var oProperty in oType.GetProperties())
            {
                //if (oProperty.PropertyType.Name == "ICollection`1" && oProperty.PropertyType.IsGenericType)
                //    continue;

                if (oProperty.Name.ToLower().Equals("updateddate"))
                    continue;

                var oOldValue = oProperty.GetValue(oldObject, null);
                var oNewValue = oProperty.GetValue(newObject, null);
                // this will handle the scenario where either value is null

                if (Equals(oOldValue, oNewValue)) continue;
                // Handle the display values when the underlying value is null

                var sOldValue = oOldValue == null ? "null" : oOldValue.ToString();
                var sNewValue = oNewValue == null ? "null" : oNewValue.ToString();
                sb.Append($"{oProperty.Name}: {sOldValue} –> {sNewValue}");
                sb.AppendLine();
            }

            return sb.ToString();
        }



    }
}
