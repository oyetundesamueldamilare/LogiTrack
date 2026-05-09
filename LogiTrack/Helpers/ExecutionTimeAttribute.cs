using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;


namespace LogiTrack.Helpers
{
    public class ExecutionTimeAttribute : ActionFilterAttribute
    {
        private long _start;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _start = Stopwatch.GetTimestamp();
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            var elapsed = Stopwatch.GetElapsedTime(_start).TotalMilliseconds;
            context.HttpContext.Response.Headers.Append("X-Execution-Time-ms", elapsed.ToString("F0"));
        }
    }

}
