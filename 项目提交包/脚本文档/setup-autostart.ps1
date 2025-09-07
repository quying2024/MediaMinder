param(
    [string]$Action = "enable",  # enable, disable, status
    [string]$ExecutablePath = ""
)

$TaskName = "MediaMinder_UI_AutoStart"

switch ($Action.ToLower()) {
    "enable" {
        if ([string]::IsNullOrEmpty($ExecutablePath)) {
            Write-Host "错误: 请提供可执行文件路径" -ForegroundColor Red
            exit 1
        }
        
        try {
            # 创建任务计划
            $action = New-ScheduledTaskAction -Execute $ExecutablePath
            $trigger = New-ScheduledTaskTrigger -AtLogOn
            $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType InteractiveToken
            $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
            
            Register-ScheduledTask -TaskName $TaskName -Action $action -Trigger $trigger -Principal $principal -Settings $settings -Description "MediaMinder UI Auto Start"
            
            Write-Host "自启动已启用" -ForegroundColor Green
        } catch {
            Write-Host "启用自启动失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    "disable" {
        try {
            Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
            Write-Host "自启动已禁用" -ForegroundColor Green
        } catch {
            Write-Host "禁用自启动失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    "status" {
        try {
            $task = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
            if ($task) {
                Write-Host "自启动状态: $($task.State)" -ForegroundColor Green
            } else {
                Write-Host "自启动未配置" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "检查自启动状态失败: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    default {
        Write-Host "用法: .\setup-autostart.ps1 -Action [enable|disable|status] -ExecutablePath [路径]" -ForegroundColor Yellow
    }
}
