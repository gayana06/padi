using System;

namespace PADI_LIBRARY
{
    public interface ILogger
    {   
        /// <summary>
        /// Log messages of type DEBUG.
        /// </summary>
        /// <param name="message">summery message</param>
        /// <param name="detailMessage">detailed message to be passed</param>
        /// <param name="source">source method</param>
        void LogDebug(string message, string detailMessage, string source);

        /// <summary>
        /// Log messages of type ERROR.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        void LogError(string message, string detailMessage, string source);

        /// <summary>
        /// Log messages of type INFORMATION.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        void LogInfo(string message, string detailMessage, string source);

        /// <summary>
        /// Log messages of type WARNING.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        void LogWarn(string message, string detailMessage, string source);

        /// <summary>
        /// Log messages of type FATAL.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="detailMessage"></param>
        /// <param name="source"></param>
        void LogFatal(string message, string detailMessage, string source);
    }
}
