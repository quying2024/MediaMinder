using System.Threading;

namespace MediaMinder.UI
{
    /// <summary>
    /// 进程唯一性管理器
    /// </summary>
    public static class ProcessManager
    {
        private static Mutex? _mutex;
        private const string MutexName = "MediaMinder_UI_SingleInstance";

        /// <summary>
        /// 检查是否为单实例
        /// </summary>
        /// <returns>如果是单实例返回true，否则返回false</returns>
        public static bool IsSingleInstance()
        {
            try
            {
                _mutex = new Mutex(true, MutexName, out bool createdNew);
                return createdNew;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 释放互斥锁
        /// </summary>
        public static void ReleaseMutex()
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
                _mutex = null;
            }
            catch
            {
                // 忽略释放异常
            }
        }
    }
}
