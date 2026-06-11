function Get-OldLogFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $Path,

        [int] $Days = 30,

        [string] $Filter = '*.log',

        [datetime] $Now = (Get-Date)
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Folder not found: $Path"
    }

    $limit = $Now.AddDays(-1 * $Days)

    Get-ChildItem -LiteralPath $Path -Filter $Filter -File |
        Where-Object { $_.LastWriteTime -lt $limit } |
        Sort-Object -Property LastWriteTime |
        Select-Object FullName, Name, Length, LastWriteTime
}
