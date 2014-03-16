using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PADI_LIBRARY
{
    public class Common
    {
        private static ILogger logger;

        /// <summary>
        /// Log instance initiation
        /// </summary>
        /// <returns></returns>
        public static ILogger Logger()
        {
            if (logger == null)
                logger = Log4NetLogger.GetInstance(); 
            return logger;
        }
    }
}
