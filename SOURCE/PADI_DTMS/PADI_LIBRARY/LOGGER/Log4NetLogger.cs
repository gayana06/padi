#region Directive Section

using System.Globalization;
using System.Collections.Generic;
using log4net;
using log4net.Config;

#endregion

namespace PADI_LIBRARY
{
    public class Log4NetLogger : ILogger
    {
        #region Private Variables

        private ILog log;
        private static Log4NetLogger log4NetLogger;
        private static Dictionary<string, Log4NetLogger> loggerDic = new Dictionary<string, Log4NetLogger>();
        private const string LOG_APPLICATION_LOGS = "LogApplicationLogs";

        #endregion

        #region Class Initializer

        /// <summary>
        /// Default constructor
        /// </summary>
        private Log4NetLogger()
        {
            log = LogManager.GetLogger(LOG_APPLICATION_LOGS);
            XmlConfigurator.Configure();
        }

        /// <summary>
        /// Overloaded constructor with logger type parameter
        /// </summary>
        /// <param name="loggerType">logger type</param>
        private Log4NetLogger(string loggerType)
        {
            log = LogManager.GetLogger(loggerType);
            XmlConfigurator.Configure();
        }

        /// <summary>
        /// Get logger instance
        /// </summary>
        /// <returns>logger instance</returns>
        public static Log4NetLogger GetInstance()
        {
            if (!(loggerDic.ContainsKey(LOG_APPLICATION_LOGS))) 
            {
                loggerDic.Add(LOG_APPLICATION_LOGS,new Log4NetLogger());
            }
            return loggerDic[LOG_APPLICATION_LOGS];
        }

        /// <summary>
        /// Get logger instance overloaded with logger type
        /// </summary>
        /// <param name="loggerType">logger type</param>
        /// <returns>logger instance</returns>
        public static Log4NetLogger GetInstance(string loggerType)
        {
            if (!(loggerDic.ContainsKey(loggerType)))
            {
                loggerDic.Add(loggerType, new Log4NetLogger(loggerType));
            }
            return loggerDic[loggerType];
        }

        #endregion

        #region ILogger Members

        /// <summary>
        /// Logs messages of type 'DEBUG' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        public void LogDebug(string message, string detailMessage, string source)
        {
            if (log.IsDebugEnabled)
                log.Debug(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", message, detailMessage, source));
        }

        /// <summary>
        /// Logs messages of type 'ERROR' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        public void LogError(string message, string detailMessage, string source)
        {
            if (log.IsErrorEnabled)
                log.Error(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", message, detailMessage, source));
        }

        /// <summary>
        /// Logs messages of type 'INFORMATION' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        public void LogInfo(string message, string detailMessage, string source)
        {
            if (log.IsInfoEnabled)
                log.Info(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", message, detailMessage, source));
        }

        /// <summary>
        /// Logs messages of type 'WARNING' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        public void LogWarn(string message, string detailMessage, string source)
        {
            if (log.IsWarnEnabled)
                log.Warn(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", message, detailMessage, source));
        }

        /// <summary>
        /// Logs messages of type 'FATAL' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        public void LogFatal(string message, string detailMessage, string source)
        {
            if (log.IsFatalEnabled)
                log.Fatal(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", message, detailMessage, source));
        }

        /// <summary>
        /// Logs messages of type 'FATAL' to the appenders
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        public void LogFatal(string message, string detailMessage)
        {
            if (log.IsFatalEnabled)
                log.Fatal(string.Format(CultureInfo.InvariantCulture, "{0},{1}", message, detailMessage));
        }

        #endregion
    }
}
