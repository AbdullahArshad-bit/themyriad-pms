namespace PMS.Controllers.Api
{
    public class GenericResponse
    {
        public bool status { get; set; }
        public string message { get; set; }
        public object data { get; set; }

        public GenericResponse CreateResponse(bool Status = false, string Message = "", object Data = null)
        {
            if (Data == null)
            {
                Data = new string[] { };
            }
            //if (status == false)
            //{
            //   // ErrorLogger.WriteToErrorLog(Message, Data.ToString(), "ERROR");

            //}
            return new GenericResponse
            {
                status = Status,
                message = Message,
                data = Data
            };
        }

    }

}