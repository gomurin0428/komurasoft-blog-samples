# プロセスの優先度クラスとアフィニティを変更する関数（記事 2・3 章）。
# 記事のコマンドをまとめたもの:
#   $p = Get-Process -Id $PID
#   $p.PriorityClass = "AboveNormal"
#   $p.ProcessorAffinity = [IntPtr]0xF   # 0xF は 2進数で 1111。論理プロセッサ 0〜3 を許可
#
# 記事 3 章の通り、アフィニティの変更はあくまで検証用の強い制約です。
# SupportsShouldProcess を付けているため、-WhatIf で予行できます。
#
# 設定の変更は Windows と Linux の PowerShell 7 で動作します
# （Linux で優先度を上げるには root などの権限が必要です）。

function Set-ProcessCpuSetting {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        # 対象プロセスの ID
        [Parameter(Mandatory)]
        [int] $Id,

        # 設定する優先度クラス（例: AboveNormal）。
        # 記事 2 章の通り、High / RealTime の常用は避けること
        [System.Diagnostics.ProcessPriorityClass] $PriorityClass,

        # 設定するアフィニティマスク（例: 0xF で論理プロセッサ 0〜3 を許可）
        [long] $AffinityMask
    )

    if (-not $PSBoundParameters.ContainsKey('PriorityClass') -and
        -not $PSBoundParameters.ContainsKey('AffinityMask')) {
        throw 'Specify -PriorityClass and/or -AffinityMask.'
    }

    if ($PSBoundParameters.ContainsKey('AffinityMask') -and $AffinityMask -le 0) {
        throw "AffinityMask must be a positive bit mask. Specified: $AffinityMask"
    }

    $process = Get-Process -Id $Id -ErrorAction Stop
    $target = "PID $Id ($($process.ProcessName))"

    if ($PSBoundParameters.ContainsKey('PriorityClass')) {
        if ($PSCmdlet.ShouldProcess($target, "Set PriorityClass to $PriorityClass")) {
            $process.PriorityClass = $PriorityClass
        }
    }

    if ($PSBoundParameters.ContainsKey('AffinityMask')) {
        $maskText = '0x{0:X}' -f $AffinityMask

        if ($PSCmdlet.ShouldProcess($target, "Set ProcessorAffinity to $maskText")) {
            $process.ProcessorAffinity = [IntPtr]$AffinityMask
        }
    }

    # 変更後（-WhatIf 時は変更されていない現状）の状態を返す
    Get-Process -Id $Id |
        Select-Object Id, ProcessName, PriorityClass, ProcessorAffinity
}
