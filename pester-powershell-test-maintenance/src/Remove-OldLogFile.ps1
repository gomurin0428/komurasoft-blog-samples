function Remove-OldLogFile {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [int] $Days = 30,

        [datetime] $Now = (Get-Date)
    )

    $targets = Get-OldLogFile -Path $Path -Days $Days -Now $Now

    foreach ($target in $targets) {
        if ($PSCmdlet.ShouldProcess($target.FullName, 'Remove old log file')) {
            Remove-Item -LiteralPath $target.FullName -Force
        }
    }
}
