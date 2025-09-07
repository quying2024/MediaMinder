namespace MediaMinder.UI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 检查是否为单实例
        if (!ProcessManager.IsSingleInstance())
        {
            MessageBox.Show("MediaMinder 已经在运行中！", "MediaMinder", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            
            // 创建并显示主窗体
            var mainForm = new PhotoDisplayForm();
            Application.Run(mainForm);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序启动失败: {ex.Message}", "MediaMinder", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            // 释放互斥锁
            ProcessManager.ReleaseMutex();
        }
    }    
}