using Microsoft.Win32.TaskScheduler;

namespace MediaMinder.UI
{
    /// <summary>
    /// 自启动管理器
    /// </summary>
    public static class AutoStartManager
    {
        private const string TaskName = "MediaMinder_UI_AutoStart";

        /// <summary>
        /// 检查自启动是否已启用
        /// </summary>
        /// <returns>如果已启用返回true，否则返回false</returns>
        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var taskService = new TaskService();
                var task = taskService.GetTask(TaskName);
                return task != null && task.Enabled;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 启用自启动
        /// </summary>
        /// <param name="executablePath">可执行文件路径</param>
        /// <returns>操作是否成功</returns>
        public static bool EnableAutoStart(string executablePath)
        {
            try
            {
                using var taskService = new TaskService();
                
                // 删除现有任务（如果存在）
                try
                {
                    taskService.RootFolder.DeleteTask(TaskName);
                }
                catch
                {
                    // 忽略删除异常
                }

                // 创建新任务
                var taskDefinition = taskService.NewTask();
                taskDefinition.RegistrationInfo.Description = "MediaMinder UI Auto Start";
                taskDefinition.Principal.LogonType = TaskLogonType.InteractiveToken;
                taskDefinition.Settings.Enabled = true;
                taskDefinition.Settings.AllowDemandStart = true;
                taskDefinition.Settings.StartWhenAvailable = true;

                // 添加登录触发器
                var trigger = new LogonTrigger();
                taskDefinition.Triggers.Add(trigger);

                // 添加执行操作
                var action = new ExecAction(executablePath);
                taskDefinition.Actions.Add(action);

                // 注册任务
                taskService.RootFolder.RegisterTaskDefinition(TaskName, taskDefinition);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"无法创建自启动任务: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 禁用自启动
        /// </summary>
        /// <returns>操作是否成功</returns>
        public static bool DisableAutoStart()
        {
            try
            {
                using var taskService = new TaskService();
                taskService.RootFolder.DeleteTask(TaskName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
