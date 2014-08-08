using Elmah;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace elmahappender_log4net
{
	public class ELMAHAppender : log4net.Appender.AppenderSkeleton
	{
		private readonly static Type _DeclaringType = typeof(ELMAHAppender);
		private string _HostName;
		private ErrorLog _ErrorLog;

		public bool UseNullContext { get; set; }

		public override void ActivateOptions()
		{
			base.ActivateOptions();
			_HostName = Environment.MachineName;
			try
			{
				if (UseNullContext)
				{
					this._ErrorLog = ErrorLog.GetDefault(null);
				}
				else
				{
					this._ErrorLog = ErrorLog.GetDefault(HttpContext.Current);
				}
			}
			catch (Exception ex)
			{
				this.ErrorHandler.Error("Could not create default ELMAH error log", ex);
			}
		}


		protected override void Append(log4net.Core.LoggingEvent loggingEvent)
		{
			if (this._ErrorLog != null)
			{
				var message = (loggingEvent.MessageObject != null) ? loggingEvent.MessageObject.ToString() : null;

				Error error;
				if (loggingEvent.ExceptionObject != null)
				{
					error = new Error(loggingEvent.ExceptionObject, HttpContext.Current) {
						Time = DateTime.Now
					};

					if (!string.IsNullOrEmpty(message))
						error.Message = message;
				}
				else
				{
					error = new Error(new Exception(message ?? "<unknown>"), HttpContext.Current) {
						Detail = base.RenderLoggingEvent(loggingEvent),
						HostName = this._HostName,
						Time = DateTime.Now,
						Type = "log4net - " + loggingEvent.Level
					};
				}

				// Enforce max length of Elmah error message
				const int messageMaxLength = 500;
				if (error.Message != null && error.Message.Length >= messageMaxLength)
					error.Message = error.Message.Substring(0, messageMaxLength - 3) + "...";

				this._ErrorLog.Log(error);
			}
		}
	}
}
