# プロセスの優先度クラスとアフィニティを観察する関数（記事 2・3・10 章）。
# 記事のコマンドをまとめたもの:
#   Get-Process -Id $PID  | Select-Object Id, ProcessName, PriorityClass
#   Get-Process -Id $PID  | Select-Object Id, ProcessName, ProcessorAffinity
#   Get-Process -Name MyApp | Select-Object Id, ProcessName, PriorityClass, ProcessorAffinity
#
# PriorityClass / ProcessorAffinity の取得は Windows と Linux の PowerShell 7 で動作します。

function Get-ProcessCpuSetting {
    [CmdletBinding(DefaultParameterSetName = 'Id')]
    param(
        # 対象プロセスの ID。省略時は現在の PowerShell プロセス（$PID）
        [Parameter(ParameterSetName = 'Id')]
        [int] $Id = $PID,

        # プロセス名で指定する場合（例: MyApp）
        [Parameter(Mandatory, ParameterSetName = 'Name')]
        [string] $Name
    )

    $processes = if ($PSCmdlet.ParameterSetName -eq 'Name') {
        Get-Process -Name $Name -ErrorAction Stop
    }
    else {
        Get-Process -Id $Id -ErrorAction Stop
    }

    $processes |
        Select-Object Id, ProcessName, PriorityClass, ProcessorAffinity
}
