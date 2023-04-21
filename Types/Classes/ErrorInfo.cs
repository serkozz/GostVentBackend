using System.Net;
// using Newtonsoft.Json;

namespace Types.Classes
{
    public class ErrorInfo<T> where T : class
    {
        public HttpStatusCode StatusCode { get; }
        /// <summary>
        /// Object that represents error additional data
        /// </summary>
        /// <value></value>
        public T Details { get; }
        public string Message { get; }
        public ErrorInfo(HttpStatusCode statusCode, T details, string message)
        {
            StatusCode = statusCode;
            Details = details;
            Message = message;
        }
    }
    public class ErrorInfo
    {
        public HttpStatusCode StatusCode { get; }
        public string StatusName { get; }
        public string Message { get; }
        public ErrorInfo(HttpStatusCode statusCode, string message)
        {
            StatusCode = statusCode;
            Message = message;
            StatusName = StatusCode.ToString();
        }
    }
}