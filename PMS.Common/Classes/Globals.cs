using PMS.Common.Classes;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;

namespace PMS.Common
{
    public class Globals
    {
        public static User User
        {
            get
            {
                if (HttpContext.Current.User.Identity.IsAuthenticated)
                {

                    var context = HttpContext.Current;
                    var authenticationTicket = ((FormsIdentity)context.User.Identity).Ticket;

                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();

                    var data = serializer.Deserialize<User>(authenticationTicket.UserData);
                    return data;

                    // The user is authenticated. Return the user from the forms auth ticket.
                    //return ((MyPrincipal)(HttpContext.Current.User)).User;
                }
                else if (HttpContext.Current.Items.Contains("User"))
                {
                    // The user is not authenticated, but has successfully logged in.
                    return (User)HttpContext.Current.Items["User"];
                }
                else
                {
                    return null;
                }
            }
        }


        private static string _uploadDirectory = "/Upload/Files";

        public static string UploadDirectory
        {
            get { return _uploadDirectory; }
        }



        public static List<AppUserRoles> AppUserRoles { get; set; }



        private List<string> _roomsGender = new List<string> { "Male", "Female", "Mix" };

        public List<string> RoomsGender
        {
            get { return _roomsGender; }
        }



        private static List<string> _genders = new List<string> { "Male", "Female", "Staff" };

        public static List<string> Genders
        {
            get { return _genders; }
        }


        private static List<string> _titles = new List<string> { "Mr.", "Ms.", "Other" };

        public static List<string> Titles
        {
            get { return _titles; }
        }


        //public static List<string> CountryList
        //{
        //    get
        //    {
        //        List<string> cultureList = new List<string>();
        //        CultureInfo[] getCultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

        //        foreach (CultureInfo getCulture in getCultureInfo)
        //        {
        //            RegionInfo getRegionInfo = new RegionInfo(getCulture.LCID);

        //            if (!cultureList.Contains(getRegionInfo.EnglishName))
        //            {
        //                cultureList.Add(getRegionInfo.EnglishName);
        //            }
        //        }

        //        cultureList.Sort();
        //        return cultureList;
        //    }
        //}
        public static List<string> CountryList
        {
            get
            {
                List<string> countryList = new List<string>
                {
                 "Afghanistan",
                 "Albania",
                 "Algeria",
                 "Andorra",
                 "Angola",
                 "Antigua and Barbuda",
                 "Argentina",
                 "Armenia",
                 "Australia",
                 "Austria",
                 "Azerbaijan",
                 "Bahamas",
                 "Bahrain",
                 "Bangladesh",
                 "Barbados",
                 "Belarus",
                 "Belgium",
                 "Belize",
                 "Benin",
                 "Bhutan",
                 "Bolivia",
                 "Bosnia and Herzegovina",
                 "Botswana",
                 "Brazil",
                 "Brunei",
                 "Bulgaria",
                 "Burkina Faso",
                 "Burundi",
                 "Cabo Verde",
                 "Cambodia",
                 "Cameroon",
                 "Canada",
                 "Central African Republic",
                 "Chad",
                 "Chile",
                 "China",
                 "Colombia",
                 "Comoros",
                 "Congo, Democratic Republic of the",
                 "Congo, Republic of the",
                 "Costa Rica",
                 "Cote d'Ivoire",
                 "Croatia",
                 "Cuba",
                 "Cyprus",
                 "Czechia",
                 "Denmark",
                 "Djibouti",
                 "Dominica",
                 "Dominican Republic",
                 "Ecuador",
                 "Egypt",
                 "El Salvador",
                 "Equatorial Guinea",
                 "Eritrea",
                 "Estonia",
                 "Eswatini (formerly Swaziland)",
                 "Ethiopia",
                 "Fiji",
                 "Finland",
                 "France",
                 "Gabon",
                 "Gambia",
                 "Georgia",
                 "Germany",
                 "Ghana",
                 "Greece",
                 "Grenada",
                 "Guatemala",
                 "Guinea",
                 "Guinea-Bissau",
                 "Guyana",
                 "Haiti",
                 "Honduras",
                 "Hungary",
                 "Iceland",
                 "India",
                 "Indonesia",
                 "Iran",
                 "Iraq",
                 "Ireland",
                 "Israel",
                 "Italy",
                 "Jamaica",
                 "Japan",
                 "Jordan",
                 "Kazakhstan",
                 "Kenya",
                 "Kiribati",
                 "Kosovo",
                 "Kuwait",
                 "Kyrgyzstan",
                 "Laos",
                 "Latvia",
                 "Lebanon",
                 "Lesotho",
                 "Liberia",
                 "Libya",
                 "Liechtenstein",
                 "Lithuania",
                 "Luxembourg",
                 "Madagascar",
                 "Malawi",
                 "Malaysia",
                 "Maldives",
                 "Mali",
                 "Malta",
                 "Marshall Islands",
                 "Mauritania",
                 "Mauritius",
                 "Mexico",
                 "Micronesia",
                 "Moldova",
                 "Monaco",
                 "Mongolia",
                 "Montenegro",
                 "Morocco",
                 "Mozambique",
                 "Myanmar (formerly Burma)",
                 "Namibia",
                 "Nauru",
                 "Nepal",
                 "Netherlands",
                 "New Zealand",
                 "Nicaragua",
                 "Niger",
                 "Nigeria",
                 "North Korea",
                 "North Macedonia (formerly Macedonia)",
                 "Norway",
                 "Oman",
                 "Pakistan",
                 "Palau",
                 "Palestine",
                 "Panama",
                 "Papua New Guinea",
                 "Paraguay",
                 "Peru",
                 "Philippines",
                 "Poland",
                 "Portugal",
                 "Qatar",
                 "Romania",
                 "Russia",
                 "Rwanda",
                 "Saint Kitts and Nevis",
                 "Saint Lucia",
                 "Saint Vincent and the Grenadines",
                 "Samoa",
                 "San Marino",
                 "Sao Tome and Principe",
                 "Saudi Arabia",
                 "Senegal",
                 "Serbia",
                 "Seychelles",
                 "Sierra Leone",
                 "Singapore",
                 "Slovakia",
                 "Slovenia",
                 "Solomon Islands",
                 "Somalia",
                 "South Africa",
                 "South Korea",
                 "South Sudan",
                 "Spain",
                 "Sri Lanka",
                 "Sudan",
                 "Suriname",
                 "Sweden",
                 "Switzerland",
                 "Syria",
                 "Taiwan",
                 "Tajikistan",
                 "Tanzania",
                 "Thailand",
                 "Timor-Leste (formerly East Timor)",
                 "Togo",
                 "Tonga",
                 "Trinidad and Tobago",
                 "Tunisia",
                 "Turkey",
                 "Turkmenistan",
                 "Tuvalu",
                 "Uganda",
                 "Ukraine",
                 "United Arab Emirates",
                 "United Kingdom",
                 "United States of America",
                 "Uruguay",
                 "Uzbekistan",
                 "Vanuatu",
                 "Vatican City (Holy See)",
                 "Venezuela",
                 "Vietnam",
                 "Yemen",
                 "Zambia",
                 "Zimbabwe"
                 };
                return countryList;
            }
        }

        private static string _baseUrl = "http://www.themyriad.com:8020/";

        public static string BaseUrl
        {
            get { return _baseUrl; }
            set { _baseUrl = value; }
        }



        public static string GetMaxPersonCode(int id)
        {
            PMSEntities db1 = new PMSEntities();
            var data = db1.Locations.Where(x => x.LocationID == id).FirstOrDefault();
            int code = 0;
            var Code = "";
            if (db1.People.Where(x => x.Code != null && x.LocationId == id).Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(db1.People.Where(x => x.Code != null && x.LocationId == id).AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-')[2]) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;
                string value = String.Format("{0:D4}", code);
                Code = "PER-" + data.Prefix + "-" + value;
                return Code;

            }
            else

            {
                code = 1;
                string value = String.Format("{0:D4}", code);
                Code = "PER-" + data.Prefix + "-" + value;
            }
            return Code;
        }
        public static string GetBookingNumber(int id)
        {
            PMSEntities db1 = new PMSEntities();
            var data = db1.Locations.Where(x => x.LocationID == id).FirstOrDefault();
            var Code = "";
            Code = data.Prefix + "-" + Guid.NewGuid().ToString().Split('-')[0].ToUpper();

            return Code;
        }

        public static string GetFileContent(string Path)
        {
            string path = HttpContext.Current.Server.MapPath(Path);
            string content = System.IO.File.ReadAllText(path);
            return content;
        }



    }


}
